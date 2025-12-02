using System.Collections.Generic;
namespace Genies.Customization.Framework.Navigation
{
    public interface INavigationNode
    {
        /// <summary>
        /// If the node starts it's own navigation stack, each stack has it's own back button and undo/redo logic
        /// </summary>
        public bool IsRootNode { get; }

        /// <summary>
        /// If the node is stackable. Set this to false if the node shouldn't be pushed to a navigation stack
        /// this is useful for leaf nodes mostly but be creative I guess. A non stackable node will be skipped when
        /// a back button request is invoked.
        /// </summary>
        public bool IsStackable { get; }

        /// <summary>
        /// Flag to check if the current node has child nodes and if we need to open
        /// the first one as a default
        /// </summary>
        public bool OpenFirstChildNodeAsDefault { get; }
        public ICustomizationConfig Config { get; }
        public ICustomizationController Controller { get; }

        /// <summary>
        /// The default Child node to open when selecting this node.
        /// </summary>
        public INavigationNode DefaultChildNavigationNodeToOpen { get; set; }
        public INavigationNode EditItemNode { get; }
        public INavigationNode CreateItemNode { get; }
        public List<INavigationNode> Children { get; }

        /// <summary>
        /// Sometimes a node can evaluate to a different node (conditional access for example), in that case, we want to make sure
        /// when going to a node to always cache the evaluated node so that unexpected behavior doesn't occur.
        /// </summary>
        /// <returns></returns>
        public INavigationNode GetEvaluatedNode();
    }
}
