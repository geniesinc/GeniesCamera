using Genies.Utilities;
using UnityEngine;

namespace Genies.Shaders
{
    /// <summary>
    /// Static access to the <see cref="GeniesShaderInfo"/> instances for our main shaders and material bakers.
    /// </summary>
    public static class GeniesShaders
    {
        private const string _lqMegaSimpleBakerPath = "MegaBakers/LQ-MegaSimpleBaker";
        private const string _mqMegaSimpleBakerPath = "MegaBakers/MQ-MegaSimpleBaker";
        private const string _hqMegaSimpleBakerPath = "MegaBakers/HQ-MegaSimpleBaker";

        private const string _lqUrpBakerPath = "MegaBakers/LQ-URPBaker";
        private const string _mqUrpBakerPath = "MegaBakers/MQ-URPBaker";
        private const string _hqUrpBakerPath = "MegaBakers/HQ-URPBaker";

        private const string _megaShaderInfoPath = "MegaShaderInfo";
        private const string _megaSkinInfoPath = "MegaSkinInfo";
        private const string _megaHairInfoPath = "MegaHairInfo";
        private const string _megaEyesInfoPath = "MegaEyesInfo";
        private const string _megaFlairInfoPath = "MegaFlairInfo";

        /// <summary>
        /// Supports conversion from our mega shaders to MegaSimple (Low quality preset).
        /// </summary>
        public static MaterialBaker LqMegaSimpleBaker => _lqMegaSimpleBaker ??= Resources.Load<MaterialBaker>(_lqMegaSimpleBakerPath);

        /// <summary>
        /// Supports conversion from our mega shaders to MegaSimple (Mid quality preset).
        /// </summary>
        public static MaterialBaker MqMegaSimpleBaker => _mqMegaSimpleBaker ??= Resources.Load<MaterialBaker>(_mqMegaSimpleBakerPath);

        /// <summary>
        /// Supports conversion from our mega shaders to MegaSimple (High quality preset).
        /// </summary>
        public static MaterialBaker HqMegaSimpleBaker => _hqMegaSimpleBaker ??= Resources.Load<MaterialBaker>(_hqMegaSimpleBakerPath);

        /// <summary>
        /// Supports conversion from our mega shaders to URP/Lit (Low quality preset).
        /// </summary>
        public static MaterialBaker LqUrpBaker => _lqUrpBaker ??= Resources.Load<MaterialBaker>(_lqUrpBakerPath);

        /// <summary>
        /// Supports conversion from our mega shaders to URP/Lit (Mid quality preset).
        /// </summary>
        public static MaterialBaker MqUrpBaker => _mqUrpBaker ??= Resources.Load<MaterialBaker>(_mqUrpBakerPath);

        /// <summary>
        /// Supports conversion from our mega shaders to URP/Lit (High quality preset).
        /// </summary>
        public static MaterialBaker HqUrpBaker => _hqUrpBaker ??= Resources.Load<MaterialBaker>(_hqUrpBakerPath);

        public static GeniesShaderInfo MegaShader => _megaShader ??= Resources.Load<GeniesShaderInfo>(_megaShaderInfoPath);
        public static GeniesShaderInfo MegaSkin => _megaSkin ??= Resources.Load<GeniesShaderInfo>(_megaSkinInfoPath);
        public static GeniesShaderInfo MegaHair => _megaHair ??= Resources.Load<GeniesShaderInfo>(_megaHairInfoPath);
        public static GeniesShaderInfo MegaEyes => _megaEyes ??= Resources.Load<GeniesShaderInfo>(_megaEyesInfoPath);
        public static GeniesShaderInfo MegaFlair => _megaFlair ??= Resources.Load<GeniesShaderInfo>(_megaFlairInfoPath);

        private static MaterialBaker _lqMegaSimpleBaker;
        private static MaterialBaker _mqMegaSimpleBaker;
        private static MaterialBaker _hqMegaSimpleBaker;

        private static MaterialBaker _lqUrpBaker;
        private static MaterialBaker _mqUrpBaker;
        private static MaterialBaker _hqUrpBaker;

        private static GeniesShaderInfo _megaShader;
        private static GeniesShaderInfo _megaSkin;
        private static GeniesShaderInfo _megaHair;
        private static GeniesShaderInfo _megaEyes;
        private static GeniesShaderInfo _megaFlair;
    }
}
