using System;
using Genies.Utilities;
using Newtonsoft.Json;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Genies.Models
{
    /// <summary>
    /// Models a single scene light that will be tunable for a 'look'
    /// </summary>
    [Serializable]
    public class SceneLight
    {
        // Used for conversions from UnityEngine.Light to SceneLight and back
        public static Func<int, LightCullingMask> CullingMaskToSceneLightCullingMask = x =>
        {
            if (x == ~0)
            {
                return LightCullingMask.Everything;
            }
            return x == (1<<8) ? LightCullingMask.Avatar : LightCullingMask.Background;
        };
        public static Func<LightCullingMask, int> LightCullingMaskToCullingMask = x =>
        {
            if (x == LightCullingMask.Everything)
            {
                return ~0;
            }
            return x == LightCullingMask.Avatar ? (1 << 8) : ~(1 << 8);
        };
        public static Func<LightType, SceneLightType> LightTypeToSceneLightType = x => x == UnityEngine.LightType.Spot ? SceneLightType.Spot : SceneLightType.Directional;
        public static Func<SceneLightType, LightType> SceneLightTypeToLightType = x => x == SceneLightType.Spot ? UnityEngine.LightType.Spot : UnityEngine.LightType.Directional;
        public static Func<bool, LightShadows> BoolToShadows = x => x ? LightShadows.Soft : LightShadows.None;
        public static Func<LightShadows, bool> ShadowsToBool = x => x != LightShadows.None;
        public static Func<Transform, Transform, Transform, LightLocality> GetLightLocality = (lightTransform, worldAttachTransform, avatarAttachTransform) =>
        {
            if (lightTransform.parent == worldAttachTransform)
            {
                return LightLocality.World;
            }
            return lightTransform.parent == avatarAttachTransform ? LightLocality.Avatar : LightLocality.Camera;
        };

        /// <summary>
        /// Options for placing a Light.
        /// </summary>
        public enum LightLocality
        {
            /// <summary>
            /// Attach the Light to the main camera.
            /// </summary>
            Camera,

            /// <summary>
            /// Place the Light in world space.
            /// </summary>
            World,

            /// <summary>
            /// Have the Light to follow the avatar in only X and Z direction
            /// </summary>
            Avatar,
        }

        /// <summary>
        /// Options for which objects are affected by the Light.
        /// </summary>
        public enum LightCullingMask
        {
            /// <summary>
            /// Affects everything in the scene.
            /// </summary>
            Everything = 1,

            /// <summary>
            /// Affects everything except avatar in the scene.
            /// </summary>
            Background = 2,

            /// <summary>
            /// Affects only the avatar in the scene.
            /// </summary>
            Avatar = 7,
        }

        /// <summary>
        /// Used to narrow down the choices from Unity's <see cref="LightType"/>
        /// </summary>
        public enum SceneLightType {Directional, Spot }

        /// <summary>
        /// Name of the Light for Inspector visibility, not used in code logic.
        /// </summary>
        public string LightName;

        /// <summary>
        /// Whether to enable the Light component or not.
        /// </summary>
        public bool IsEnabled;

        /// <summary>
        /// The locality option for the scene light. By default it is attached to the main camera.
        /// </summary>
        public LightLocality Locality = LightLocality.Camera;

        /// <summary>
        /// Culling mask option that specifies which object(s) will be affected.
        /// </summary>
        public LightCullingMask CullingMask;

        /// <summary>
        /// The LightType (Directional or Spot)
        /// </summary>
        public SceneLightType LightType;

        /// <summary>
        /// The Color of the Light.
        /// </summary>
        [JsonProperty("Color")]
        public Color Color;

        /// <summary>
        /// Local position.
        /// </summary>
        public Vector3 LocalPosition;

        /// <summary>
        /// Local rotation in euler angles.
        /// </summary>
        public Vector3 LocalRotationAngle;

        /// <summary>
        /// The intensity of the Light.
        /// </summary>
        public float Intensity;

        /// <summary>
        /// Range of the spotlight (used only for spotlights)
        /// </summary>
        public float Range;

        /// <summary>
        /// Inner spotlight angle (used only for spotlights)
        /// </summary>
        public float InnerSpotAngle;

        /// <summary>
        /// Outer spotlight angle (used only for spotlights)
        /// </summary>
        public float OuterSpotAngle;

        /// <summary>
        /// Whether or not to let the light cast shadows. Currently only exposed for first light and only soft shadow.
        /// </summary>
        public bool CastShadows;

        /// <summary>
        /// Returns a SceneLight model filled with values from the given Unity Light
        /// </summary>
        /// <param name="light">The light you want to use to fill out the model</param>
        /// <param name="worldAttachTransform">The transform of the light if it's locality is 'World'</param>
        /// <param name="avatarAttachTransform">Parent transform of the light if it's locality is 'Avatar'/></param>
        public static SceneLight FromLight(Light light, Transform worldAttachTransform, Transform avatarAttachTransform)
        {
            var sceneLight = new SceneLight();
            Transform lightTransform = light.transform;

            sceneLight.LightName = lightTransform.name;
            sceneLight.IsEnabled = light.enabled;
            sceneLight.Locality = GetLightLocality(lightTransform, worldAttachTransform, avatarAttachTransform);
            sceneLight.CullingMask = CullingMaskToSceneLightCullingMask(light.cullingMask);
            sceneLight.LightType = LightTypeToSceneLightType(light.type);
            sceneLight.Color = light.color;
            sceneLight.LocalPosition = lightTransform.localPosition;
            sceneLight.LocalRotationAngle = lightTransform.localRotation.eulerAngles;
            sceneLight.Intensity = light.intensity;
            sceneLight.Range = light.range;
            sceneLight.InnerSpotAngle = light.innerSpotAngle;
            sceneLight.OuterSpotAngle = light.spotAngle;
            sceneLight.CastShadows = ShadowsToBool(light.shadows);

            return sceneLight;
        }
    }

    /// <summary>
    /// Extensions for the Unity light component often used with SceneLight data model
    /// </summary>
    public static class LightExtensions
    {
        /// <summary>
        /// Set a Unity Light component with the data in a Genies SceneLight model
        /// </summary>
        public static void SetWithSceneLight(this Light light, SceneLight sceneLight, Transform worldAttachTransform, Transform cameraAttachTransform, Transform avatarAttachTransform)
        {
            light.enabled = sceneLight.IsEnabled;
            switch (sceneLight.Locality)
            {
                case SceneLight.LightLocality.World:
                    light.transform.SetParent(worldAttachTransform);
                    break;
                case SceneLight.LightLocality.Camera:
                    light.transform.SetParent(cameraAttachTransform);
                    break;
                case SceneLight.LightLocality.Avatar:
                    light.transform.SetParent(avatarAttachTransform);
                    break;
                default:
                    goto case SceneLight.LightLocality.World;
            }
            light.cullingMask = SceneLight.LightCullingMaskToCullingMask(sceneLight.CullingMask);
            light.type = SceneLight.SceneLightTypeToLightType(sceneLight.LightType);
            light.color = sceneLight.Color;
            light.transform.localPosition = sceneLight.LocalPosition;
            light.transform.localRotation = Quaternion.Euler(sceneLight.LocalRotationAngle);
            light.intensity = sceneLight.Intensity;
            light.range = sceneLight.Range;
            light.innerSpotAngle = sceneLight.InnerSpotAngle;
            light.spotAngle = sceneLight.OuterSpotAngle;
            light.shadows = SceneLight.BoolToShadows(sceneLight.CastShadows);
        }
    }
}
