using System;
using System.Collections.Generic;
using System.Linq;

namespace Genies.ServiceManagement
{
    /// <summary>
    /// Performs topological sorting of installers based on their dependencies.
    /// Automatically orders installers so that dependencies are satisfied and detects circular dependencies.
    /// </summary>
    internal static class InstallerTopologicalSorter
    {
        /// <summary>
        /// Sorts installers in dependency order using topological sort algorithm.
        /// Dependencies will be placed before installers that require them.
        /// </summary>
        /// <param name="installers">Collection of installers to sort</param>
        /// <returns>Installers sorted in dependency order</returns>
        /// <exception cref="ServiceManagerException">Thrown when circular dependencies are detected</exception>
        public static List<IGeniesInstaller> Sort(IEnumerable<IGeniesInstaller> installers)
        {
            var installerList = installers.ToList();
            // Group by type and take the first instance of each type to handle duplicates
            var installerMap = installerList.GroupBy(i => i.GetType())
                                            .ToDictionary(g => g.Key, g => g.First());

            // Build dependency graph
            var dependencyGraph = BuildDependencyGraph(installerList);

            // Perform topological sort with cycle detection
            var sortedTypes = TopologicalSort(dependencyGraph);

            // Convert back to installer instances, preserving only the installers we have
            var result = new List<IGeniesInstaller>();
            foreach (var type in sortedTypes)
            {
                if (installerMap.TryGetValue(type, out var installer))
                {
                    result.Add(installer);
                }
            }

            return result;
        }

        /// <summary>
        /// Builds a dependency graph from the installer collection.
        /// </summary>
        private static Dictionary<Type, HashSet<Type>> BuildDependencyGraph(List<IGeniesInstaller> installers)
        {
            var graph = new Dictionary<Type, HashSet<Type>>();

            // Initialize graph with all installer types
            foreach (var installer in installers)
            {
                var installerType = installer.GetType();
                if (!graph.ContainsKey(installerType))
                {
                    graph[installerType] = new HashSet<Type>();
                }
            }

            // Add dependencies to graph
            foreach (var installer in installers)
            {
                var installerType = installer.GetType();
                var requiredTypes = InstallerRequirementAnalyzer.GetRequiredInstallerTypes(installer);

                foreach (var requiredType in requiredTypes)
                {
                    // Add required type to graph even if we don't have an instance
                    if (!graph.ContainsKey(requiredType))
                    {
                        graph[requiredType] = new HashSet<Type>();
                    }

                    // Add edge: requiredType -> installerType (requiredType must come before installerType)
                    graph[requiredType].Add(installerType);
                }
            }

            return graph;
        }

        /// <summary>
        /// Performs topological sorting using Kahn's algorithm with cycle detection.
        /// </summary>
        private static List<Type> TopologicalSort(Dictionary<Type, HashSet<Type>> graph)
        {
            var result = new List<Type>();
            var inDegree = new Dictionary<Type, int>();
            var queue = new Queue<Type>();

            // Calculate in-degrees
            foreach (var node in graph.Keys)
            {
                inDegree[node] = 0;
            }

            foreach (var node in graph.Keys)
            {
                foreach (var neighbor in graph[node])
                {
                    inDegree[neighbor]++;
                }
            }

            // Find all nodes with no incoming edges
            foreach (var kvp in inDegree)
            {
                if (kvp.Value == 0)
                {
                    queue.Enqueue(kvp.Key);
                }
            }

            // Process nodes
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                result.Add(current);

                // Remove edges from current node
                foreach (var neighbor in graph[current])
                {
                    inDegree[neighbor]--;
                    if (inDegree[neighbor] == 0)
                    {
                        queue.Enqueue(neighbor);
                    }
                }
            }

            // Check for cycles
            if (result.Count != graph.Count)
            {
                var remainingNodes = graph.Keys.Where(k => !result.Contains(k)).ToList();
                var cycleDescription = string.Join(", ", remainingNodes.Select(t => t.Name));

                throw new ServiceManagerException(
                    $"Circular dependency detected among installers: {cycleDescription}. " +
                    "Remove circular dependencies between IRequiresInstaller declarations.");
            }

            return result;
        }

    }
}
