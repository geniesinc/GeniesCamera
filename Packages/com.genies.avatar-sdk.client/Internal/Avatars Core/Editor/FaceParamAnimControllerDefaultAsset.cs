using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

namespace Genies.Avatars.Sdk.Editor
{
    /// <summary>
    /// Asset that defines default face animation parameters to be applied to avatar animator controllers.
    /// This asset copies face animation parameters from a reference controller to target controllers.
    /// </summary>
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "FaceParamAnimControllerDefaultAsset.asset", menuName = "Genies/Anim Controller Defaults/Face param data asset")]
#endif
#if GENIES_SDK && !GENIES_INTERNAL
    internal class FaceParamAnimControllerDefaultAsset : AnimControllerDefaultAsset
#else
    public class FaceParamAnimControllerDefaultAsset : AnimControllerDefaultAsset
#endif
    {
        /// <summary>
        /// Applies face animation parameters from the reference controller to the target controller.
        /// Only adds parameters that don't already exist in the target controller.
        /// </summary>
        /// <param name="target">The AnimatorController to apply face parameters to.</param>
        public override void ApplyToTargetController(AnimatorController target)
        {
            if (!target)
            {
                return;
            }

            List<AnimatorControllerParameter> targetParams = new List<AnimatorControllerParameter>(target.parameters);

            HashSet<string> targetParamNames = new HashSet<string>();
            foreach (var param in targetParams)
            {
                targetParamNames.Add(param.name);
            }

            // Get ref parameters. Note that these are a copy and not a direct reference (https://docs.unity3d.com/ScriptReference/Animations.AnimatorController-parameters.html)
            AnimatorControllerParameter[] refParams = RefController.parameters;

            foreach (var param in refParams)
            {
                // Skip if the param exists in target controller already
                if (targetParamNames.Contains(param.name))
                {
                    continue;
                }

                targetParams.Add(param);
            }

            target.parameters = targetParams.ToArray();

            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssetIfDirty(target);
        }
    }
}
