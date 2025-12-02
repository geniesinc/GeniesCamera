using UnityEngine;
using System.Collections.Generic;

namespace Genies.Avatars
{
    public static class TransformDeepChildExtension
    {
        //Breadth-first search
        public static Transform FindDeepChild(this Transform aParent, string aName)
        {
            Queue<Transform> queue = new Queue<Transform>();
            queue.Enqueue(aParent);
            while (queue.Count > 0)
            {
                var c = queue.Dequeue();
                if (c.name == aName)
                {
                    return c;
                }

                foreach (Transform t in c)
                {
                    queue.Enqueue(t);
                }
            }

            return null;
        }
    }
}