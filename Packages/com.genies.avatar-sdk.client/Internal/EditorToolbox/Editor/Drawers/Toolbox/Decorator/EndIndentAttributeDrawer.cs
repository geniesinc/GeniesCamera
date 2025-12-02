using UnityEditor;
using Toolbox.Core;

namespace Toolbox.Editor.Drawers
{
    public class EndIndentAttributeDrawer : ToolboxDecoratorDrawer<EndIndentAttribute>
    {
        protected override void OnGuiCloseSafe(EndIndentAttribute attribute)
        {
            EditorGUI.indentLevel -= attribute.IndentToSubtract;
        }
    }
}