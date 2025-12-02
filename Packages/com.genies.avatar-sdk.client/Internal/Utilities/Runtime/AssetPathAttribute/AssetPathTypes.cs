using UnityEngine;
using System.Collections;

namespace Genies.Utilities
{
    public partial class AssetPath
    {
        /// <summary>
        /// A enum containing all the types of paths we can watch
        /// </summary>
        public enum PathType
        {
            /// <summary>
            /// The path will be contained within the 'Asset/*' directory.
            /// </summary>
            Project,
            /// <summary>
            /// The path will be contained within a resources folder.
            /// </summary>
            Resources,
        }
    }
}
