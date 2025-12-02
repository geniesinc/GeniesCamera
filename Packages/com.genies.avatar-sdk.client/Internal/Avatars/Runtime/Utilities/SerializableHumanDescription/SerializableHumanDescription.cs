using System;
using System.Collections.Generic;
using UnityEngine;

namespace Genies.Avatars
{
    [Serializable]
    public sealed class SerializableHumanDescription
    {
        public List<SerializableHumanBone>    human;
        public List<SerializableSkeletonBone> skeleton;
        public float                          upperArmTwist;
        public float                          lowerArmTwist;
        public float                          upperLegTwist;
        public float                          lowerLegTwist;
        public float                          armStretch;
        public float                          legStretch;
        public float                          feetSpacing;
        public bool                           hasTranslationDoF;
        
        public static SerializableHumanDescription Convert(HumanDescription humanDescription)
        {
            return new SerializableHumanDescription
            {
                human             = SerializableHumanBone.Convert(humanDescription.human),
                skeleton          = SerializableSkeletonBone.Convert(humanDescription.skeleton),
                upperArmTwist     = humanDescription.upperArmTwist,
                lowerArmTwist     = humanDescription.lowerArmTwist,
                upperLegTwist     = humanDescription.upperLegTwist,
                lowerLegTwist     = humanDescription.lowerLegTwist,
                armStretch        = humanDescription.armStretch,
                legStretch        = humanDescription.legStretch,
                feetSpacing       = humanDescription.feetSpacing,
                hasTranslationDoF = humanDescription.hasTranslationDoF,
            };
        }
        
        public static HumanDescription Convert(SerializableHumanDescription humanDescription)
        {
            return new HumanDescription
            {
                human             = SerializableHumanBone.Convert(humanDescription.human),
                skeleton          = SerializableSkeletonBone.Convert(humanDescription.skeleton),
                upperArmTwist     = humanDescription.upperArmTwist,
                lowerArmTwist     = humanDescription.lowerArmTwist,
                upperLegTwist     = humanDescription.upperLegTwist,
                lowerLegTwist     = humanDescription.lowerLegTwist,
                armStretch        = humanDescription.armStretch,
                legStretch        = humanDescription.legStretch,
                feetSpacing       = humanDescription.feetSpacing,
                hasTranslationDoF = humanDescription.hasTranslationDoF,
            };
        }
    }
}