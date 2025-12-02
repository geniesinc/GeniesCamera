using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Genies.Avatars
{
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "Unified-TriangulatedShape-Loader", menuName = "Genies/Reference Shape Loaders/Unified Triangulated Shape")]
#endif
    public sealed class UnifiedTriangulatedShapeLoader : UnifiedReferenceShapesLoader
    {
        public enum TswSolver
        {
            Biquadratic = 0,
            Exponential = 1,
            Gauss       = 2,
        }

        [Header("Triangulated Shape Settings"), Space(4)]
        public bool enableWeightsCache = false;
        public TswSolver weightsSolver = TswSolver.Biquadratic;
        public BiquadraticTswSolver biquadraticSolver = new();
        public ExponentialTswSolver exponentialSolver = new();
        public GaussTswSolver gaussSolver = new();

        protected override UniTask<IReferenceShape> InitializeShapeAsync(Mesh mesh, string id)
        {
            ITswSolver solver = weightsSolver switch
            {
                TswSolver.Biquadratic => biquadraticSolver,
                TswSolver.Exponential => exponentialSolver,
                TswSolver.Gauss       => gaussSolver,
                _ => throw new ArgumentOutOfRangeException()
            };

            var shape = new TriangulatedShape(id, solver);
            shape.Initialize(mesh);
            shape.EnableWeightsCache = enableWeightsCache;

            return UniTask.FromResult<IReferenceShape>(shape);
        }

        private bool ShowBiquadraticSolver => weightsSolver is TswSolver.Biquadratic;
        private bool ShowExponentialSolver => weightsSolver is TswSolver.Exponential;
        private bool ShowGaussSolver       => weightsSolver is TswSolver.Gauss;
    }
}
