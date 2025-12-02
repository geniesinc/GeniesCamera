namespace Genies.AvatarEditor
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal enum AvatarEditorMode
#else
    public enum AvatarEditorMode
#endif
    {
        Avatar,
        Outfit,
    }
}
