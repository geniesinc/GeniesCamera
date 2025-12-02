using Genies.Utilities;
using System.Collections.Generic;
using UnityEngine;

namespace Genies.Avatars
{
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "RepeatableParamFeature", menuName = "Genies/Genie Components/Animation Features/Repeatable Param Feature")]
#endif
    public class RepeatableParamAnimationFeatureAsset : RepeatableParamBaseFeatureAsset
    {
        [SerializeField]
        private List<string> animatorParameterSuffixes = new List<string>();
        public override List<string> SupportSuffixes => animatorParameterSuffixes;
        public GenieComponentAsset component;

        public override GenieComponent CreateFeatureComponent(AnimatorParameters parameters)
        {
            return component.CreateComponent();
        }
    }
}
