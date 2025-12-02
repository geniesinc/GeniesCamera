using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Genies.Avatars
{
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "Unified-UniquePointsShape-Loader", menuName = "Genies/Reference Shape Loaders/Unified Unique Points Shape")]
#endif
    public sealed class UnifiedUniquePointsShapeLoader : UnifiedReferenceShapesLoader
    {
        [Header("Unique Points Shape Settings"), Space(4)]
        public bool enableReferenceCorrelationsCache = false;

        protected override async UniTask<IReferenceShape> InitializeShapeAsync(Mesh mesh, string id)
        {
            var shape = new UniquePointsShape(id);

            using Mesh.MeshDataArray meshDataArray = Mesh.AcquireReadOnlyMeshData(mesh);
            await UniTask.RunOnThreadPool(() =>
            {
                shape.Initialize(meshDataArray[0]);
                shape.EnableReferenceCorrelationsCache = enableReferenceCorrelationsCache;
            });

            return shape;
        }


    }
}
