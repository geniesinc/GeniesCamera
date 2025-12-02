using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace Genies.Customization.Framework.Navigation
{
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "NavigationGraph", menuName = "Genies/Customizer/Navigation/Navigation Graph")]
#endif
    [RequireNode(typeof(NavigationRootNode))]
    public class NavigationGraph : NodeGraph
    {
        private INavigationNode _root;

        [ContextMenu("Link")]
        public void Link()
        {
            foreach (var node in nodes)
            {
                if (node is NavigationRootNode rootNode)
                {
                    rootNode.Link();
                }
                else if (node is NavigationNode navNode)
                {
                    navNode.Link();
                }
                else if (node is NavigationEvaluationNode evalNode)
                {
                    evalNode.Link();
                }
            }
        }

        public INavigationNode GetRootNode()
        {
            _root ??= (INavigationNode)nodes.FirstOrDefault(n => n is NavigationRootNode);
            return _root;
        }


    }
}
