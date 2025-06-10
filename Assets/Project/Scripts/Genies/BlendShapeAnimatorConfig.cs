using UnityEngine;

namespace Genies.Utils
{
    public enum ChannelRetargetBehavior
    {
        Unchanged = 0,
        PositiveControl = 1,
        NegativeControl = 2
    }

    /// <summary>
    /// A ScriptableObject that holds information about the driver>driven relationships between animation channels and blendshapes.
    /// </summary>
    [CreateAssetMenu(fileName = "BlendShapeAnimatorConfig", menuName = "Genies/Utils/Blend Shape Animator Config")]
    [System.Serializable]
    public class BlendShapeAnimatorConfig : ScriptableObject
    {
        public AttrManagerChannel[] channels;
    }


    [System.Serializable]
    public class AttrManagerChannel
    {
        public string inputChannelName;
        public DrivenAttribute[] drivenAttributes;

    }

    [System.Serializable]
    public class DrivenAttribute
    {
        public string outputChannelName;
        public string[] targetSubmeshes;
        public ChannelRetargetBehavior retargetBehavior;
    }
}
