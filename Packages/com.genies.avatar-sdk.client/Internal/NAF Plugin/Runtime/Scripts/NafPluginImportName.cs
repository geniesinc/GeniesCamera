namespace Genies.NafPlugin
{
    public static class ImportName
    {
#if UNITY_EDITOR
        public const string Value = "gnUnityPlugin";
#elif UNITY_IOS || UNITY_WEBGL
        // We use __Internal on iOS and WebGL as the static libraries are merged into the final build
        public const string Value = "__Internal";
#elif UNITY_STANDALONE_WIN
        public const string Value = "libgnUnityPlugin";
#else
        public const string Value = "gnUnityPlugin";
#endif
    }
}
