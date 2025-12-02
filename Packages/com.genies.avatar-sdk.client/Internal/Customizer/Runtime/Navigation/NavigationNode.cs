using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using XNode;

namespace Genies.Customization.Framework.Navigation
{
    /// <summary>
    /// Custom attribute to mark fields that should show child node dropdown options
    /// </summary>
    public class ChildNodeDropdownAttribute : PropertyAttribute
    {
        public ChildNodeDropdownAttribute() { }
    }

    [CreateNodeMenu("Customizer UI/Navigation Node")]
    public class NavigationNode : BaseNavigationNode, INavigationNode
    {
        public bool isRootNode;
        [Tooltip("If marked false, the node will not be part of the navigation stack. This can be used for leaf nodes that don't breadcrumb")]
        public bool isStackable = true;
        [Tooltip("If marked true, we will use to check if the current node has child nodes and if we need to open the first one as a default")]
        public bool openFirstChildNodeAsDefault = false;
        [Input(ShowBackingValue.Always)]
        [FormerlySerializedAs("customizationController")]
        [FormerlySerializedAs("node")]
        public CustomizationConfig customizationConfig;

        [Input(ShowBackingValue.Always)]
        [FormerlySerializedAs("customization config")]
        public BaseCustomizationController customizationController;

        [Tooltip("Choose which child node to open by default when this node is navigated to")]
        [ChildNodeDropdown]
        public BaseNavigationNode defaultChildNodeToOpen;

        [Output(connectionType = ConnectionType.Override)]
        public BaseNavigationNode editItemNode;
        [Output(connectionType = ConnectionType.Override)]
        public BaseNavigationNode createItemNode;
        [Output()]
        public List<BaseNavigationNode> childNodes;

        // Gets or sets the default child node to open when this node is navigated to.
        // The selected node must be in the childNodes list.
        public BaseNavigationNode DefaultChildNodeToOpen
        {
            get => defaultChildNodeToOpen;
            set
            {
                // Validate that the selected node is in the childNodes list
                if (value != null && (childNodes == null || !childNodes.Contains(value)))
                {
                    Debug.LogWarning($"Cannot set DefaultChildNodeToOpen to {value.name} as it is not in the childNodes list for {name}");
                    return;
                }
                defaultChildNodeToOpen = value;
            }
        }

        // Gets the default child node as an INavigationNode interface.
        // Returns null if no default child is set or if the child is not valid.
        public INavigationNode GetDefaultChildNode()
        {
            if (defaultChildNodeToOpen != null && childNodes != null && childNodes.Contains(defaultChildNodeToOpen))
            {
                return defaultChildNodeToOpen;
            }
            return null;
        }

        // Gets the index of default child node.
        // Returns -1 if no default child is set or if the child is not valid.
        public int GetIndexOfDefaultChildNodeToOpen()
        {
            if (defaultChildNodeToOpen != null && childNodes != null && childNodes.Contains(defaultChildNodeToOpen))
            {
                return childNodes.IndexOf(defaultChildNodeToOpen);
            }
            return -1;
        }

        public override bool IsRootNode => isRootNode;
        public override bool IsStackable => isStackable;
        public override bool OpenFirstChildNodeAsDefault => openFirstChildNodeAsDefault;

        public override ICustomizationConfig Config => customizationConfig;
        public override ICustomizationController Controller => customizationController;

        public override INavigationNode DefaultChildNavigationNodeToOpen
        {
            get => (INavigationNode)defaultChildNodeToOpen;
            set => defaultChildNodeToOpen = value as BaseNavigationNode;
        }
        public override INavigationNode EditItemNode => editItemNode;

        public override INavigationNode CreateItemNode => createItemNode;

        public override List<INavigationNode> Children => childNodes.ConvertAll(input => (INavigationNode)input);

        public override INavigationNode GetEvaluatedNode()
        {
            return this;
        }

        public override void Link()
        {
            foreach (var port in Ports)
            {
                if (port.fieldName == nameof(childNodes))
                {
                    var nodes = port.GetConnections().Select(c => c.node as BaseNavigationNode).ToList();

                    if (!nodes.Any())
                    {
                        continue;
                    }

                    if (childNodes == null)
                    {
                        childNodes = new List<BaseNavigationNode>();
                    }

                    for (var index = childNodes.Count - 1; index >= 0; index--)
                    {
                        BaseNavigationNode node = childNodes[index];
                        if (node == null || !nodes.Contains(node))
                        {
                            childNodes.Remove(node);
                        }
                        else
                        {
                            nodes.Remove(node);
                        }
                    }

                    childNodes.AddRange(nodes);
                }

                if (port.fieldName == nameof(editItemNode))
                {
                    editItemNode = (BaseNavigationNode)port.Connection?.node;
                }

                if (port.fieldName == nameof(createItemNode))
                {
                    createItemNode = (BaseNavigationNode)port.Connection?.node;
                }
            }
        }
    }
}
