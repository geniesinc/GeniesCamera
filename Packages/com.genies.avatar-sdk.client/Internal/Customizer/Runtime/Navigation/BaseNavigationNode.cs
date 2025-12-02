using System.Collections.Generic;
using XNode;

namespace Genies.Customization.Framework.Navigation
{
    public abstract class BaseNavigationNode : Node, INavigationNode
    {
        public abstract bool IsRootNode { get; }
        public abstract bool IsStackable { get; }
        public abstract bool OpenFirstChildNodeAsDefault { get; }
        public abstract INavigationNode DefaultChildNavigationNodeToOpen { get; set; }
        public abstract ICustomizationConfig Config { get; }
        public abstract ICustomizationController Controller { get; }
        public abstract INavigationNode EditItemNode { get; }
        public abstract INavigationNode CreateItemNode { get; }
        public abstract List<INavigationNode> Children { get; }
        public abstract INavigationNode GetEvaluatedNode();

        public abstract void Link();

        public override void OnCreateConnection(NodePort from, NodePort to)
        {
            Link();
            base.OnCreateConnection(from, to);
        }

        public override void OnRemoveConnection(NodePort port)
        {
            Link();
            base.OnRemoveConnection(port);
        }

        protected override void Init()
        {
            Link();
            base.Init();
        }

    }
}
