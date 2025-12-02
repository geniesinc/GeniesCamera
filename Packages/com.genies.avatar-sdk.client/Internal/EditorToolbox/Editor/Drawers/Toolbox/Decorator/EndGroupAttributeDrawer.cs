using Toolbox.Core;

namespace Toolbox.Editor.Drawers
{
    public class EndGroupAttributeDrawer : ToolboxDecoratorDrawer<EndGroupAttribute>
    {
        protected override void OnGuiCloseSafe(EndGroupAttribute attribute)
        {
            ToolboxLayoutHandler.CloseVertical();
        }
    }
}