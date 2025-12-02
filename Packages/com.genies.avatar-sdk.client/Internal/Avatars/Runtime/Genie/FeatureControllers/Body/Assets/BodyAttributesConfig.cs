using System;
using System.Collections.Generic;
using UnityEngine;

namespace Genies.Avatars
{
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "BodyAttributesConfig", menuName = "Genies/Body Attributes Config")]
#endif
    public sealed partial class BodyAttributesConfig : ScriptableObject
    {
        [SerializeField]
        private List<Attribute> attributes;

        public List<Attribute> Attributes => attributes;

        public event Action Updated = delegate { };

        /// <summary>
        /// The <see cref="BodyController"/> is subscribe to the <see cref="Updated"/> event so you can manually call
        /// this after performing any changes for them to take effect.
        /// </summary>
        public void NotifyUpdate()
        {
            Updated.Invoke();
        }

        private void OnValidate()
        {
            NotifyUpdate();
        }

        [Serializable]
        public struct Attribute
        {
            public string           name;
            public List<Joint>      joints;
            public List<BlendShape> blendShapes;
        }

        [Serializable]
        public struct Joint
        {
            public string name;

            [Space(8), Header("Position"), Space(4)]
            public bool enablePosition;
            [Space(4)]
            public Vector3 minPosition;
            public Vector3 maxPosition;

            [Space(4), Header("Rotation"), Space(4)]
            public bool enableRotation;
            [Space(4)]
            public Vector3 minRotation;
            public Vector3 maxRotation;

            [Space(4), Header("Scale"), Space(4)]
            public bool enableScale;
            [Space(4)]
            public Vector3 minScale;
            public Vector3 maxScale;

            [Tooltip("Scale will be evaluated in the LateUpdate rather than in the animation threads")]
            public bool scaleLateUpdate;
            [Tooltip("Final min and max scale vectors will be inverted")]
            public bool invertScale;
            [Tooltip("If true, only the X values from min and max scale will be used to get a uniform scale vector")]
            public bool uniformScale;
        }

        [Serializable]
        public struct BlendShape
        {
            [Header("Min Blend Shape"), Space(4)]
            public string minName;
            public float minDefaultWeight;
            public float minTargetWeight;

            [Space(4), Header("Max Blend Shape"), Space(4)]
            public string maxName;
            public float maxDefaultWeight;
            public float maxTargetWeight;
        }
    }
}
