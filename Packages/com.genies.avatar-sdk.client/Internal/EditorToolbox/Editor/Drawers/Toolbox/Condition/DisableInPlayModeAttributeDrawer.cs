using UnityEditor;
using Toolbox.Core;

namespace Toolbox.Editor.Drawers
{
    public class DisableInPlayModeAttributeDrawer : ToolboxConditionDrawer<DisableInPlayModeAttribute>
    {
        protected override PropertyCondition OnGuiValidateSafe(SerializedProperty property, DisableInPlayModeAttribute attribute)
        {
            return EditorApplication.isPlayingOrWillChangePlaymode ? PropertyCondition.Disabled : PropertyCondition.Valid;
        }
    }
}