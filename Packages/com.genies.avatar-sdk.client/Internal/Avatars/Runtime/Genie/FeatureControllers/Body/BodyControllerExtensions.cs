using System.Collections.Generic;
using UnityEngine;

namespace Genies.Avatars
{
    public static class BodyControllerExtensions
    {
        public static bool IsPresetApplied(this IBodyController controller, IReadOnlyDictionary<string, float> preset)
        {
            foreach ((string attribute, float weight) in preset)
            {
                if (!Mathf.Approximately(controller.GetAttributeWeight(attribute), weight))
                {
                    return false;
                }
            }
            
            return true;
        }
        
        public static bool IsPresetApplied(this IBodyController controller, IEnumerable<BodyAttributeState> preset)
        {
            foreach (BodyAttributeState attributeState in preset)
            {
                if (!Mathf.Approximately(controller.GetAttributeWeight(attributeState.name), attributeState.weight))
                {
                    return false;
                }
            }
            
            return true;
        }
    }
}