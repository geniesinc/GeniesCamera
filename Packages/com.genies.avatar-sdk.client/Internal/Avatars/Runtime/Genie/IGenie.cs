using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Genies.Avatars
{
    /// <summary>
    /// Represents a genie avatar instance.
    /// </summary>
    public interface IGenie : IDisposable
    {
        /// <summary>
        /// The genie species of the avatar instance.
        /// </summary>
        string Species { get; }
        
        /// <summary>
        /// The genie sub-species of the avatar instance;
        /// used for GAP Avatar variant id.
        /// If Species is not UnifiedGAP, this is left empty.
        /// </summary>
        string SubSpecies { get; }

        /// <summary>
        /// The LOD of the genie.
        /// </summary>
        string Lod { get; }

        /// <summary>
        /// The root GameObject of the genie.
        /// </summary>
        GameObject Root { get; }

        /// <summary>
        /// The root of all renderers.
        /// </summary>
        GameObject ModelRoot { get; }

        /// <summary>
        /// The root transform of the skeleton.
        /// </summary>
        Transform SkeletonRoot { get; }

        /// <summary>
        /// The animator that controls this genie.
        /// </summary>
        Animator Animator { get; }

        /// <summary>
        /// The renderer components of this genie.
        /// </summary>
        IReadOnlyList<SkinnedMeshRenderer> Renderers { get; }

        /// <summary>
        /// Manages the components attached to this genie.
        /// </summary>
        GenieComponentManager Components { get; }

        /// <summary>
        /// Whether or not this genie instance has been disposed.
        /// </summary>
        bool IsDisposed { get; }

        /// <summary>
        /// Fired when the <see cref="Root"/> gets rebuilt. On the <see cref="UmaGenie"/> implementation this is fired every time the
        /// <see cref="UMA.CharacterSystem.DynamicCharacterAvatar"/> gets rebuilt.
        /// </summary>
        event Action RootRebuilt;

        /// <summary>
        /// Fired when the genie gets rebuilt. This is always fired along with <see cref="RootRebuilt"/>, but <see cref="RootRebuilt"/>
        /// will not always be fired with this (i.e.: on material-only updates).
        /// </summary>
        event Action Rebuilt;

        /// <summary>
        /// Fired when the genie gets disposed.
        /// </summary>
        event Action Disposed;

        /// <summary>
        /// Creates a clone of this genie instance. When an original <see cref="IGenie"/> instance gets disposed, all its clones
        /// will be disposed too. All clones will be always synced with the original genie.
        /// </summary>
        /// <param name="onLayer">Specifies the unity layer of the cloned avatar</param>
        UniTask<IGenie> CloneAsync(int onLayer = -1);

        /// <summary>
        /// Bakes genie for optimized performance. The returned baked genie is independent from the original and can be
        /// disposed separately since it doesn't share any resources.
        /// </summary>
        /// <param name="urpBake">If true, all materials will be converted to URP standard materials. If false, our custom
        /// MegaSimple material will be used, which is more advanced and contains custom features like glitter.</param>
        UniTask<IGenie> BakeAsync(Transform parent = null, bool urpBake = false);

        /// <summary>
        /// Returns a snapshot of the exact pose that the genie is currently at. A snapshot is not a functional avatar
        /// (no animator or skinned mesh renderer), it is just a static mesh.
        /// </summary>
        /// <param name="urpBake">If true, all materials will be converted to URP standard materials. If false, our custom
        /// MegaSimple material will be used, which is more advanced and contains custom features like glitter.</param>
        UniTask<IGenieSnapshot> TakeSnapshotAsync(Transform parent = null, bool urpBake = false);
    }
}
