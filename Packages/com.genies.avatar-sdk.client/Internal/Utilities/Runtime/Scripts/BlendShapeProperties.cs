using System;

namespace Genies.Utilities
{
    [Flags]
    public enum BlendShapeProperties
    {
        None = 0,
        Vertices = 1 << 0,
        Normals = 1 << 1,
        Tangents = 1 << 2,
        All = ~0
    }
}
