using GnWrappers;
using UnityEngine;

namespace Genies.Naf
{
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "NAF-Settings", menuName = "Genies/NAF/NAF Settings", order = 0)]
#endif
    public sealed class NafSettings : ScriptableObject
    {
        private const string DefaultSettingsResourcesPath = "NafSettings_Default";

        public enum Logging
        {
            Disabled              = 0,
            EditorOnly            = 1,
            EditorOrDevBuildOnly  = 2,
            Always                = 3,
        }

        public Logging                logging                    = Logging.EditorOnly;
        public NafAssetResolverConfig defaultAssetResolverConfig;
        public NafTextureSettings     globalTextureSettings;
        public NafMaterialSettings    defaultMaterialSettings;

        /**
         * Applies the settings to the NAF plugin. This ignores any settings that can only be applied during initialization, like logging.
         */
        public void Apply()
        {
            // apply global texture settings
            using TextureSettings textureSettings = TextureSettings.Global();
            globalTextureSettings.Write(textureSettings);

            // set default asset resolver config
            NafAssetResolverConfig.Default = defaultAssetResolverConfig;

            // set default material settings
            NafMaterialSettings.Default = defaultMaterialSettings;
        }

        public static bool TryLoadDefault(out NafSettings settings)
        {
            settings = Resources.Load<NafSettings>(DefaultSettingsResourcesPath);
            return settings != null;
        }
    }
}
