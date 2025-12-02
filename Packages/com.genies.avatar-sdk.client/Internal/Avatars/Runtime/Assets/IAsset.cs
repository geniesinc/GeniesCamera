namespace Genies.Avatars
{
    /// <summary>
    /// Standardized way of thinking about assets used across the Avatars package. All assets must have a unique
    /// string ID that must be the same as the one used to load from <see cref="IAssetLoader{TAsset}"/>. Also all
    /// assets have a property that defines the asset LOD.
    /// <br/><br/>
    /// Two asset instances with the same ID will be considered the same asset even if the instance
    /// is different (that would probably mean that the instance was not created properly).
    /// </summary>
    public interface IAsset
    {
        string Id { get; }
        string Lod { get; }
    }
}