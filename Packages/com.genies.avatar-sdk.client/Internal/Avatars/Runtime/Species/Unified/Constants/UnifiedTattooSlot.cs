using System.Collections.Generic;

namespace Genies.Avatars
{
    /// <summary>
    /// Contains all the tattoo slots available for the unified species and their transform presets.
    /// </summary>
    public static class UnifiedTattooSlot
    {
        public const string LeftTopForearm = "LeftTopForearmTattoos";
        public const string LeftTopOuterArm = "LeftTopOuterArmTattoos";
        public const string RightSideThigh = "RightSideThighTattoos";
        public const string RightSideAboveTheKnee = "RightSideAboveTheKneeTattoos";
        public const string LeftSideCalf = "LeftSideCalfTattoos";
        public const string LeftSideBelowKnee = "LeftSideBelowKneeTattoos";
        public const string LowerBack = "LowerBackTattoos";
        public const string LowerStomach = "LowerStomachTattoos";
        
        public static readonly IReadOnlyList<string> All = new List<string>
        {
            LeftTopForearm,
            LeftTopOuterArm,
            RightSideThigh,
            RightSideAboveTheKnee,
            LeftSideCalf,
            LeftSideBelowKnee,
            LowerBack,
            LowerStomach
        }.AsReadOnly();
        
        public static readonly IReadOnlyList<TattooTransformPreset> TattooTransformPresets = new List<TattooTransformPreset>
        {
            new TattooTransformPreset
            {
                Id = LeftTopForearm,
                PositionX = 0.22f,
                PositionY = 0.08f,
                Rotation = 0.0f,
                Scale = 9.8f,
            },
            new TattooTransformPreset
            {
                Id = LeftTopOuterArm,
                PositionX = 0.24f,
                PositionY = 0.379f,
                Rotation = 0.0f,
                Scale = 17.1f,
            },
            new TattooTransformPreset
            {
                Id = RightSideThigh,
                PositionX = 0.56f,
                PositionY = -0.25f,
                Rotation = -2.0f,
                Scale = 22.3f,
            },
            new TattooTransformPreset
            {
                Id = RightSideAboveTheKnee,
                PositionX = 0.56f,
                PositionY = -0.43f,
                Rotation = -2.0f,
                Scale = 13.9f,
            },
            new TattooTransformPreset
            {
                Id = LeftSideCalf,
                PositionX = -0.11f,
                PositionY = 0.13f,
                Rotation = -8.0f,
                Scale = 16.8f,
            },
            new TattooTransformPreset
            {
                Id = LeftSideBelowKnee,
                PositionX = -0.265f,
                PositionY = 0.15f,
                Rotation = 0.0f,
                Scale = 17.6f,
            },
            new TattooTransformPreset
            {
                Id = LowerBack,
                PositionX = 0.0f,
                PositionY = -0.74f,
                Rotation = 0.0f,
                Scale = 20.4f,
            },
            new TattooTransformPreset
            {
                Id = LowerStomach,
                PositionX = -0.76f,
                PositionY = -0.12f,
                Rotation = 0.0f,
                Scale = 18.3f,
            }
        }.AsReadOnly();
    }
}
