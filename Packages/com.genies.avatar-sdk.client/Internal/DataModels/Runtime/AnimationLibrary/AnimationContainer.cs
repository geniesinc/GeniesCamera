using UnityEngine;

namespace Genies.Models
{
    /// <summary>
    /// Animation Container
    /// </summary>
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "AnimationContainer", menuName = "Genies/AnimationLibrary/GenericAnimationContainer", order = 0)]
#endif
    public class AnimationContainer : OrderedScriptableObject, IDynamicAsset
    {
        public const int CurrentPipelineVersion = 0;
        public int PipelineVersion { get; set; } = CurrentPipelineVersion;

        [SerializeField] private string assetName;
        [SerializeField] private string guid;
        [SerializeField] private string assetAddress;
        [SerializeField] private AnimationClip[] animations;
        [SerializeField] private RuntimeAnimatorController controller;

        public string AssetId
        {
            get => assetName;
            set => assetName = value;
        }

        public string Guid
        {
            get => guid;
            set => guid = value;
        }

        public string AssetAddress
        {
            get => assetAddress;
            set => assetAddress = value;
        }

        public AnimationClip[] Animations
        {
            get => animations;
            set => animations = value;
        }

        public RuntimeAnimatorController Controller
        {
            get => controller;
            set => controller = value;
        }
    }
}
