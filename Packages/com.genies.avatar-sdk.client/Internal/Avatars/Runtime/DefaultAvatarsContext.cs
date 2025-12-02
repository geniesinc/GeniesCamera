namespace Genies.Avatars
{
    /// <summary>
    /// Use the <see cref="Instance"/> field to set a default <see cref="AvatarsContext"/> that will be used
    /// by the <see cref="AvatarsFactory"/> when no context is provided.
    /// </summary>
    public static class DefaultAvatarsContext
    {
        public static AvatarsContext Instance;
    }
}
