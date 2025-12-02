using System;
using UnityEngine;

namespace Genies.Utilities
{
    public partial class AssetPath
    {
        /// <summary>
        /// We limit this attributes to fields and only allow one. Should
        /// only be applied to string types.
        /// </summary>
        [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
        public class Attribute : PropertyAttribute
        {
            private PathType m_PathType;
            private Type m_Type;

            /// <summary>
            /// Gets the type of asset path this attribute is watching.
            /// </summary>
            public PathType pathType
            {
                get { return m_PathType; }
            }

            /// <summary>
            /// Gets the type of asset this attribute is expecting.
            /// </summary>
            public Type type
            {
                get { return m_Type; }
            }

            /// <summary>
            /// Creates the default instance of AssetPathAttribute
            /// </summary>
            public Attribute(Type type, PathType pathType = PathType.Project)
            {
                m_Type = type;
                m_PathType = pathType;
            }



            public string SuperProperty
            {
                get
                {
                    /* whole pile of work done here */
                    return "Complex string example";
                }
            }

            public void Evulate()
            {

                string value = SuperProperty;
            }
        }
    }
}
