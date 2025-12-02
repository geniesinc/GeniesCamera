using UnityEngine;

namespace Genies.Components.CreatorTools.TexturePlacement
{
    public struct Ray
    {
        public Vector3 Org;
        public Vector3 Dir;

        public Ray(Vector3 org, Vector3 dir)
        {
            Org = org;
            Dir = dir;
        }
    }
}
