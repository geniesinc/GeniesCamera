using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Genies.Utilities
{
    public static class TransformExtensions
    {
        public static void ResetLocalTransform(this Transform transform)
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        public static void SetMatrix(this Transform transform, in Matrix4x4 matrix)
        {
            // not sure how to deal with the scale so I will let Unity do its thing by using SetParent
            Transform parent = transform.parent;
            transform.SetParent(null, worldPositionStays: false);
            transform.SetLocalMatrix(matrix);
            transform.SetParent(parent, worldPositionStays: true);
        }

        public static void SetLocalMatrix(this Transform transform, in Matrix4x4 matrix)
        {
            transform.localPosition = matrix.GetColumn(3);
            transform.localRotation = Quaternion.LookRotation(matrix.GetColumn(2), matrix.GetColumn(1));
            transform.localScale = new Vector3(
                matrix.GetColumn(0).magnitude,
                matrix.GetColumn(1).magnitude,
                matrix.GetColumn(2).magnitude
            );
        }

        /// <summary>
        /// Returns the scene path of this transform.
        /// </summary>
        public static string GetPath(this Transform transform)
        {
            return GetPathRelativeTo(transform, parent: null);
        }

        /// <summary>
        /// Given a valid parent (the transform must be parented to it, whether it is a direct or a deep child), will
        /// output the relative path of this transform, so that calling <see cref="Transform.Find"/> on the given parent
        /// with the returned path will return this transform.
        /// </summary>
        public static string GetPathRelativeTo(this Transform transform, Transform parent)
        {
            if (transform == parent)
            {
                return string.Empty; // Transform.Find() returns the same transform when an empty string is given
            }

            var pathBuilder = new StringBuilder();
            pathBuilder.Append(transform.name);
            Transform nextParent = transform.parent;

            while (nextParent && nextParent != parent)
            {
                pathBuilder.Insert(0, $"{nextParent.name}/");
                nextParent = nextParent.parent;
            }

            // if the given parent was a real parent of the transform then we can return the built path
            return nextParent == parent ? pathBuilder.ToString() : null;
        }

        public static Dictionary<string, Transform> GetChildrenByName(this Transform transform, bool recursive = true,
            bool includeSelf = false)
        {
            var transformsByName = new Dictionary<string, Transform>();
            AddChildrenByName(transform, transformsByName, recursive, includeSelf);
            return transformsByName;
        }

        public static void AddChildrenByName(this Transform transform, IDictionary<string, Transform> transformsByName,
            bool recursive = true, bool includeSelf = false)
        {
            if (includeSelf)
            {
                transformsByName.TryAdd(transform.name, transform);
            }

            for (int i = 0; i < transform.childCount; ++i)
            {
                Transform child = transform.GetChild(i);
                transformsByName.TryAdd(child.name, child);

                if (recursive)
                {
                    AddChildrenByName(child, transformsByName, recursive: true, includeSelf: false);
                }
            }
        }

        public static bool ParentExists(this Transform transform, string targetName)
        {
            var parent = transform.parent;
            while (parent != null)
            {
                if (parent.name == targetName)
                {
                    return true;
                }

                parent = parent.parent;
            }

            return false;
        }
    }
}
