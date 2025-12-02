using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.UnityLinker;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Genies.ServiceManagement.Editor
{

    /// <summary>
    /// Generates a link.xml for all services registered using DI to preserve
    /// their constructors from getting stripped. In most regular cases this should suffice
    /// but if you have an issue with a constructor getting removed either modify the logic here
    /// or just add <see cref="PreserveAttribute"/> to that constructor
    /// </summary>
    public class LinkXmlGenerator : IUnityLinkerProcessor
    {
        public int callbackOrder => 0;

        public string GenerateAdditionalLinkXmlFile(BuildReport report, UnityLinkerBuildPipelineData data)
        {
            GenerateLinkXml();
            return GetPath();
        }

        private static string GetPath()
        {
            var directoryPath = ServiceManagerPathsHelper.GetOrCreateServiceManagerAssetsPath();
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            var filePath = Path.Combine(directoryPath, "link.xml");
            return filePath;
        }

        /// <summary>
        /// This method uses code analysis to find all classes that inherit
        /// <see cref="IGeniesInstaller"/> or <see cref="GeniesLifetimeScope"/> and
        /// finds generic registrations, once found all the types in the generic calls
        /// will be preserved.
        /// </summary>
#if GENIES_INTERNAL
        [MenuItem("Genies/ServiceManager/Generate Link.xml")]
#endif
        private static void GenerateLinkXml()
        {
            var assemblyToTypes = new Dictionary<string, HashSet<string>>();
            var scriptGuids     = AssetDatabase.FindAssets("t:Script");

            var allTypesLookup = AppDomain.CurrentDomain.GetAssemblies()
                                          .SelectMany(
                                                      a =>
                                                      {
                                                          try
                                                          {
                                                              return a.GetTypes();
                                                          }
                                                          catch
                                                          {
                                                              return Type.EmptyTypes;
                                                          }
                                                      }
                                                     )
                                          .ToLookup(t => t.FullName);

            var syntaxTrees = scriptGuids.Select(
                                                 guid =>
                                                 {
                                                     var path = AssetDatabase.GUIDToAssetPath(guid);
                                                     var code = File.ReadAllText(path);
                                                     return CSharpSyntaxTree.ParseText(code);
                                                 }
                                                )
                                         .ToList();

            var compilation = CSharpCompilation.Create(
                                                       "TemporaryCompilation",
                                                       syntaxTrees,
                                                       options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                                                      );

            foreach (var tree in syntaxTrees)
            {
                var semanticModel = compilation.GetSemanticModel(tree);
                var root          = tree.GetRoot();

                var classes = root.DescendantNodes()
                                  .OfType<ClassDeclarationSyntax>()
                                  .Where(
                                         cls => cls.BaseList?.Types.Any(
                                                                        bt =>
                                                                            bt.ToString() == nameof(LifetimeScope) ||
                                                                            bt.ToString() == nameof(IInstaller) ||
                                                                            bt.ToString() == nameof(IGeniesInstaller)
                                                                       ) ?? false
                                        );

                foreach (var cls in classes)
                {
                    var relevantMethods = cls.DescendantNodes()
                                             .OfType<MethodDeclarationSyntax>()
                                             .Where(m => m.ParameterList.Parameters.Any(p => p.Type.ToString() == nameof(IContainerBuilder)));

                    foreach (var method in relevantMethods)
                    {
                        var invocations = method.Body.DescendantNodes().OfType<InvocationExpressionSyntax>();
                        foreach (var invocation in invocations)
                        {
                            var genericArguments = invocation.DescendantNodes()
                                                             .OfType<GenericNameSyntax>()
                                                             .SelectMany(g => g.TypeArgumentList.Arguments);

                            foreach (var arg in genericArguments)
                            {
                                ITypeSymbol typeSymbol = semanticModel.GetTypeInfo(arg).Type;

                                if (typeSymbol == null || typeSymbol.TypeKind == TypeKind.Interface)
                                {
                                    continue;
                                }

                                var argFullName = typeSymbol.ToString();

                                if (arg is GenericNameSyntax)
                                {
                                    if (typeSymbol is INamedTypeSymbol named)
                                    {
                                        var nameSpace = named.ConstructedFrom.ContainingNamespace.ToString();

                                        var metadataName = named.MetadataName;
                                        argFullName = !string.IsNullOrEmpty(nameSpace) ? $"{nameSpace}.{metadataName}" : metadataName;
                                    }
                                }


                                var reflectedType = allTypesLookup[argFullName].FirstOrDefault();

                                if (reflectedType == null)
                                {
                                    continue;
                                }

                                var typeAssemblyName = reflectedType.Assembly.GetName().Name;

                                if (!assemblyToTypes.ContainsKey(typeAssemblyName))
                                {
                                    assemblyToTypes[typeAssemblyName] = new HashSet<string>();
                                }

                                assemblyToTypes[typeAssemblyName].Add(argFullName);
                            }
                        }
                    }
                }
            }


            //Write the link data to link.xml
            var doc           = new XDocument();
            var linkerElement = new XElement("linker");
            doc.Add(linkerElement);

            foreach (var assemblyPair in assemblyToTypes)
            {
                var assemblyElement = new XElement("assembly");
                assemblyElement.Add(new XAttribute("fullname", assemblyPair.Key));
                linkerElement.Add(assemblyElement);

                foreach (var type in assemblyPair.Value)
                {
                    var typeElement = new XElement("type");
                    typeElement.Add(new XAttribute("fullname", type));
                    typeElement.Add(new XAttribute("preserve", "all")); // Preserve everything for the type

                    assemblyElement.Add(typeElement);
                }
            }

            // Use XmlWriter to save without the declaration
            var filePath = GetPath();
            using (var writer = XmlWriter.Create(filePath, new XmlWriterSettings { OmitXmlDeclaration = true, Indent = true }))
            {
                doc.Save(writer);
            }

            AssetDatabase.ImportAsset(filePath);

            Debug.Log("link.xml generated successfully!");
        }
    }
}
