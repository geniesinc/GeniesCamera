using System.Collections.Generic;
using System.Linq;
using UnityEngine.Serialization;
using XNode;

namespace Genies.Customization.Framework.Navigation
{
    [CreateNodeMenu("Customizer UI/Root Navigation Node")]
    public class NavigationRootNode : Node, INavigationNode
    {
        [FormerlySerializedAs("customizationController")]
        [FormerlySerializedAs("node")]
        public CustomizationConfig customizationConfig;
        public BaseCustomizationController customizationController;
        [Output(connectionType = ConnectionType.Override)]
        public BaseNavigationNode editItemNode;

        [Output(connectionType = ConnectionType.Override)]
        public BaseNavigationNode createItemNode;

        [Output()]
        public List<BaseNavigationNode> childNodes;

        public bool IsRootNode => true;
        public bool IsStackable => true;
        public bool OpenFirstChildNodeAsDefault => false;

        public ICustomizationConfig Config => customizationConfig;
        public ICustomizationController Controller => customizationController;

        public INavigationNode DefaultChildNavigationNodeToOpen { get; set; } 
       	public INavigationNode EditItemNode => editItemNode;
        public INavigationNode CreateItemNode => createItemNode;

        public List<INavigationNode> Children => childNodes.ConvertAll(node => (INavigationNode)node);
        public INavigationNode GetEvaluatedNode()
        {
            return this;
        }

        public void Link()
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

                    childNodes = new List<BaseNavigationNode>(nodes);
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

        public override void OnRemoveConnection(NodePort port)
        {
            if (port.node != this)
            {
                return;
            }

            if (port.fieldName == nameof(childNodes))
            {
                var connectedNodes = port.GetConnections().Select(p => p.node as NavigationNode);
                for (int i = childNodes.Count - 1; i >= 0; i--)
                {
                    var childNode = childNodes[i];
                    if (!connectedNodes.Contains(childNode))
                    {
                        childNodes.RemoveAt(i);
                    }
                }
            }

            if (port.fieldName == nameof(createItemNode))
            {
                createItemNode = null;
            }

            if (port.fieldName == nameof(editItemNode))
            {
                editItemNode = null;
            }

            base.OnRemoveConnection(port);
        }

        public override void OnCreateConnection(NodePort from, NodePort to)
        {
            if (from.node != this)
            {
                return;
            }

            var targetNode = to.node as NavigationNode;

            if (targetNode == null)
            {
                return;
            }

            if (from.fieldName == nameof(childNodes))
            {
                var index = from.GetConnectionIndex(to);

                if (childNodes.Count - 1 > index)
                {
                    childNodes[index] = targetNode;
                }
                else
                {
                    childNodes.Add(targetNode);
                }
            }

            if (from.fieldName == nameof(createItemNode))
            {
                createItemNode = targetNode;
            }

            if (from.fieldName == nameof(editItemNode))
            {
                editItemNode = targetNode;
            }

            base.OnCreateConnection(from, to);
        }
    }
}
