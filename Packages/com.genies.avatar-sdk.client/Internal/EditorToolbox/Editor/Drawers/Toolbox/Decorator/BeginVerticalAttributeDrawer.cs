using Toolbox.Core;

namespace Toolbox.Editor.Drawers
{
    public class BeginVerticalAttributeDrawer : ToolboxDecoratorDrawer<BeginVerticalAttribute>
    {
        protected override void OnGuiBeginSafe(BeginVerticalAttribute attribute)
        {
            ToolboxLayoutHandler.BeginVertical();
        }
    }
}