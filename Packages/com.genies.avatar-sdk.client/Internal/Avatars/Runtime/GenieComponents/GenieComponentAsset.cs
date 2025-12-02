using UnityEngine;

namespace Genies.Avatars
{
    /// <summary>
    /// Base <see cref="ScriptableObject"/> class to create <see cref="IGenieComponentCreator"/> assets.
    /// </summary>
    public abstract class GenieComponentAsset : ScriptableObject, IGenieComponentCreator
    {
        public abstract GenieComponent CreateComponent();
    }
}