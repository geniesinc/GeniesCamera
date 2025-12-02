using System.Collections.Generic;

namespace Genies.Customization.Framework.Navigation
{
    public abstract class NavigationEvaluationNode : BaseNavigationNode
    {
        [Input(ShowBackingValue.Never)]
        public string input;

        private INavigationNode _EvaluatedNode => GetEvaluatedNode();
        public override bool IsRootNode => _EvaluatedNode?.IsRootNode ?? false;
        public override bool IsStackable => _EvaluatedNode?.IsStackable ?? false;
        public override bool OpenFirstChildNodeAsDefault => _EvaluatedNode?.OpenFirstChildNodeAsDefault ?? false;
        public override INavigationNode DefaultChildNavigationNodeToOpen
        {
            get => _EvaluatedNode?.DefaultChildNavigationNodeToOpen ?? null;
            set
            {
                if (_EvaluatedNode != null)
                {
                    _EvaluatedNode.DefaultChildNavigationNodeToOpen = value;
                }
            }
        }

        public override ICustomizationConfig Config => _EvaluatedNode?.Config;
        public override ICustomizationController Controller => _EvaluatedNode?.Controller;
        public override INavigationNode EditItemNode => _EvaluatedNode?.EditItemNode;
        public override INavigationNode CreateItemNode => _EvaluatedNode?.CreateItemNode;
        public override List<INavigationNode> Children => _EvaluatedNode?.Children;

        protected abstract INavigationNode EvaluateAndGetNext();

        public sealed override INavigationNode GetEvaluatedNode()
        {
            //Find the last non evaluation node.
            var node = EvaluateAndGetNext();

            while (node as NavigationEvaluationNode != null)
            {
                var evalNode = node as NavigationEvaluationNode;
                node = evalNode.EvaluateAndGetNext();
            }

            return node;
        }
    }
}
