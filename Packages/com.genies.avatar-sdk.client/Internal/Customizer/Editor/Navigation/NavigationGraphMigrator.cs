using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using XNode;
using Genies.Customization.Framework;
using Genies.Customization.Framework.Navigation;

namespace Genies.Customizer.Editor.Navigation
{
    /// <summary>
    /// Editor-only migration utilities for NavigationGraph
    /// Handles migrating CustomizationControllers from CustomizationConfigs to standalone objects
    /// </summary>
    public static class NavigationGraphMigrator
    {
        /// <summary>
        /// Iterates through all the NavigationNodes in a NavigationGraph migrating all
        /// CustomizationControllers in CustomizationConfigs to standalone objects
        /// </summary>
        /// <param name="navigationGraph">The NavigationGraph to migrate</param>
        public static void Migrate(NavigationGraph navigationGraph)
        {
            if (navigationGraph == null)
            {
                Debug.LogError("NavigationGraph is null. Cannot perform migration.");
                return;
            }

            // Start asset editing batch
            AssetDatabase.StartAssetEditing();

            try
            {
                var createdAssets = new List<string>(); // Track created assets for single refresh

                foreach (var node in navigationGraph.nodes)
                {
                    if (node is NavigationRootNode root)
                    {
                        MigrateNode(root.Config, node.name, ref root.customizationController, ref createdAssets);
                        MigrateConnections(root);
                    }

                    if (node is NavigationNode n)
                    {
                        MigrateNode(n.Config, n.name, ref n.customizationController, ref createdAssets);
                        MigrateConnections(n);
                    }
                }

                // Only save and refresh once if we created any assets
                if (createdAssets.Count > 0)
                {
                    AssetDatabase.SaveAssets();
                }
                else
                {
                    Debug.Log("No new controller assets created during migration.");
                }
            }
            finally
            {
                // Always stop asset editing, even if there's an exception
                AssetDatabase.StopAssetEditing();
            }
        }

        /// <summary>
        /// Migrates a single node's CustomizationController
        /// </summary>
        /// <param name="config">The CustomizationConfig to migrate</param>
        /// <param name="nodeName">The name of the node being migrated</param>
        /// <param name="controller">Reference to the controller field to update</param>
        /// <param name="createdAssets">List to track created assets</param>
        public static void MigrateNode(ICustomizationConfig config, string nodeName, ref BaseCustomizationController controller, ref List<string> createdAssets)
        {
            if (config == null)
            {
                Debug.LogWarning("No config found to migrate.");
                return;
            }

            if (config is CustomizationConfig asConfig)
            {
                if (asConfig == null)
                {
                    Debug.LogWarning($"No CustomizationConfig found for node: {nodeName}");
                    return;
                }

                if (asConfig.CustomizationController is null || asConfig.CustomizationController is DummyCustomizationController)
                {
                    var originalPath = AssetDatabase.GetAssetPath(asConfig);
                    if (originalPath == null || originalPath == "")
                    {
                        Debug.LogWarning($"No original path found for config asset: {asConfig.name}");
                        return;
                    }
                    var targetPath = GenerateControllerAssetPath(originalPath);
                    // create a dummy controller and set it
                    var dummyController = ScriptableObject.CreateInstance<DummyCustomizationController>();
                    dummyController.BreadcrumbName = nodeName;
                    dummyController.CustomizerViewConfig = asConfig.CustomizerViewConfig.Clone();
                    AssetDatabase.CreateAsset(dummyController, targetPath);
                    createdAssets.Add(targetPath);
                    controller = dummyController;
                }
                else if (asConfig.CustomizationController is BaseCustomizationController asBase)
                {
                    var originalPath = AssetDatabase.GetAssetPath(asConfig);
                    var targetPath = GenerateControllerAssetPath(originalPath);

                    // Try to load existing asset first
                    var controllerAsset = AssetDatabase.LoadAssetAtPath<BaseCustomizationController>(targetPath);
                    if (controllerAsset == null)
                    {
                        // Create a COPY of the controller instead of using the original
                        var controllerCopy = Object.Instantiate(asBase);

                        try
                        {
                            // Set the properties on the copy
                            controllerCopy.BreadcrumbName = asConfig.BreadcrumbName;
                            controllerCopy.CustomizerViewConfig = asConfig.CustomizerViewConfig.Clone();

                            // Create asset from the copy
                            AssetDatabase.CreateAsset(controllerCopy, targetPath);
                            createdAssets.Add(targetPath);
                            controllerAsset = AssetDatabase.LoadAssetAtPath<BaseCustomizationController>(targetPath);
                        }
                        catch (UnityException ex)
                        {
                            Debug.LogError($"Failed to create asset at {targetPath}: {ex.Message}");
                            // Clean up the copy if creation failed
                            Object.DestroyImmediate(controllerCopy);
                            return;
                        }
                    }
                    else
                    {
                        Debug.Log($"Using existing controller asset: {targetPath}");
                    }

                    controller = controllerAsset;
                }
                else
                {
                    Debug.LogWarning($"No BaseCustomizationController found!");
                }
            }
        }

        /// <summary>
        /// Generates the asset path for a controller based on the original config path
        /// </summary>
        /// <param name="originalPath">The original config asset path</param>
        /// <returns>The controller asset path</returns>
        private static string GenerateControllerAssetPath(string originalPath)
        {
            var directory = System.IO.Path.GetDirectoryName(originalPath);
            var originalName = System.IO.Path.GetFileNameWithoutExtension(originalPath);

            return System.IO.Path.Combine(directory, $"{originalName}_Controller.asset");
        }

        /// <summary>
        /// Migrates connections from customizationConfig to customizationController for a given node
        /// </summary>
        /// <param name="node">The node to migrate connections for</param>
        private static void MigrateConnections(Node node)
        {
            if (node is NavigationNode navNode)
            {
                MigrateNodeConnections(navNode);
            }
            else if (node is NavigationRootNode rootNode)
            {
                MigrateNodeConnections(rootNode);
            }
        }

        /// <summary>
        /// Migrates connections from customizationConfig to customizationController for NavigationNode
        /// </summary>
        /// <param name="node">The NavigationNode to migrate connections for</param>
        private static void MigrateNodeConnections(NavigationNode node)
        {
            var configPort = node.GetInputPort(nameof(node.customizationConfig));
            var controllerPort = node.GetInputPort(nameof(node.customizationController));

            if (configPort != null && controllerPort != null && !configPort.IsConnected)
            {
                Debug.LogWarning($"Node {node.name} has no connections to customizationConfig. Skipping migration.");
                return;
            }

            if (configPort != null && controllerPort != null && configPort.IsConnected)
            {
                Debug.Log($"Migrating connections for node: {node.name}");
                var connections = configPort.GetConnections().ToList();

                foreach (var connection in connections)
                {
                    Debug.Log($"Migrating connection from {connection.node.name} to {node.name}");
                    // Disconnect from config port
                    configPort.Disconnect(connection);
                    // Connect to controller port
                    controllerPort.Connect(connection);
                }

                Debug.Log($"Migrated {connections.Count} connections for node: {node.name}");
            }
        }

        /// <summary>
        /// Migrates connections from customizationConfig to customizationController for NavigationRootNode
        /// </summary>
        /// <param name="node">The NavigationRootNode to migrate connections for</param>
        private static void MigrateNodeConnections(NavigationRootNode node)
        {
            var configPort = node.GetInputPort(nameof(node.customizationConfig));
            var controllerPort = node.GetInputPort(nameof(node.customizationController));

            if (configPort != null && controllerPort != null && configPort.IsConnected)
            {
                Debug.Log($"Migrating connections for root node: {node.name}");
                var connections = configPort.GetConnections().ToList();

                foreach (var connection in connections)
                {
                    Debug.Log($"Migrating connection from {connection.node.name} to {node.name}");
                    // Disconnect from config port
                    configPort.Disconnect(connection);
                    // Connect to controller port
                    controllerPort.Connect(connection);
                }

                Debug.Log($"Migrated {connections.Count} connections for root node: {node.name}");
            }
        }

        #region Context Menu Items

        /// <summary>
        /// Adds a context menu item to NavigationGraph assets for migration
        /// </summary>
        /// <param name="command">The menu command</param>
#if GENIES_INTERNAL
        [MenuItem("Assets/Migrate Navigation Graph", true)]
#endif
        private static bool ValidateMigrateNavigationGraph(MenuCommand command)
        {
            // Only show the menu item for NavigationGraph assets
            return Selection.activeObject is NavigationGraph;
        }

        /// <summary>
        /// Context menu item to trigger migration of CustomizationControllers
        /// </summary>
        /// <param name="command">The menu command</param>
#if GENIES_INTERNAL
        [MenuItem("Assets/Migrate Navigation Graph", false)]
#endif
        private static void MigrateNavigationGraph(MenuCommand command)
        {
            var navigationGraph = Selection.activeObject as NavigationGraph;
            if (navigationGraph != null)
            {
                Migrate(navigationGraph);
            }
        }

        /// <summary>
        /// Adds a context menu item to NavigationGraph assets for migration (alternative location)
        /// </summary>
        /// <param name="command">The menu command</param>
#if GENIES_INTERNAL
        [MenuItem("CONTEXT/NavigationGraph/Migrate")]
#endif
        private static void MigrateNavigationGraphContext(MenuCommand command)
        {
            var navigationGraph = command.context as NavigationGraph;
            if (navigationGraph != null)
            {
                Migrate(navigationGraph);
            }
        }

        #endregion
    }
}
