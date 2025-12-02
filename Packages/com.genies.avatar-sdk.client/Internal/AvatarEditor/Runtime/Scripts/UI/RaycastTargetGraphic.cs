using UnityEngine;
using UnityEngine.UI;

namespace Genies.AvatarEditor
{
[RequireComponent(typeof(CanvasRenderer))]
    public class RaycastTargetGraphic : Graphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
        }
    }
}