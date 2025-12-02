using UnityEngine;

namespace Genies.Components.Dynamics
{
    /// <summary>
    /// A constraint that maintains the distance between two particles.
    /// A link can optionally maintain the relative rotations between particles.
    /// </summary>
    public class DynamicsLink : MonoBehaviour
    {
        public DynamicsParticle StartParticle;
        public DynamicsParticle EndParticle;

        [Tooltip(DynamicsTooltips.LinkMaintainStartRotation)]
        public bool MaintainStartParticleRotation;

        [Tooltip(DynamicsTooltips.LinkMaintainEndRotation)]
        public bool MaintainEndParticleRotation;

        [Range(0, 1)]
        [Tooltip(DynamicsTooltips.LinkStretchiness)]
        public float Stretchiness;

        [Range(0, 1)]
        [Tooltip(DynamicsTooltips.AnchorToStartParticleRotation)]
        public float AnchorToStartParticleRotation;

        [Range(0, 1)]
        [Tooltip(DynamicsTooltips.AngleLimiting)]
        public float AngleLimiting;

        /// <summary>
        /// Represents the vector from the start particle to the end particle in the start particle's transform space.
        /// </summary>
        [HideInInspector]
        public Vector3 LocalStartToEndVector { get; private set; }
        [HideInInspector]
        public Vector3 LocalStartToEndDirection { get; private set; }

        /// <summary>
        /// Represents the vector from the end particle to the start particle in the end particle's transform space.
        /// </summary>
        [HideInInspector]
        public Vector3 LocalEndToStartVector { get; private set; }
        [HideInInspector]
        public Vector3 LocalEndToStartDirection { get; private set; }

        /// <summary>
        /// Represents the rotation of the end particle, relative to the start particle, in the initial state of the structure.
        /// </summary>
        [HideInInspector]
        public Quaternion EndRotationRelativeToStartRotation { get; private set; }
        /// <summary>
        /// Represents the rotation of the start particle, relative to the end particle, in the initial state of the structure.
        /// </summary>
        [HideInInspector]
        public Quaternion StartRotationRelativeToEndRotation { get; private set; }

        /// <summary>
        /// The effective uniform scale of the link, based on the scale of the start particle.
        /// </summary>
        private float _effectiveScale => (StartParticle && StartParticle.transform.lossyScale.x != 0) ? StartParticle.transform.lossyScale.x : 1f;

        [HideInInspector]
        /// <summary>
        /// The original local length of the link, before any scaling.
        /// </summary>
        public float OriginalLength { get; private set; }

        [HideInInspector]
        /// <summary>
        /// The current length of the link, after any scaling within the hierarchy of the start particle.
        /// </summary>
        public float ScaledLength => OriginalLength * _effectiveScale;

        private bool _initialized = false;

        private void Start()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            if (StartParticle && EndParticle)
            {
                // Store all information about the initial state of the link to compare against during constraint calculations.

                var startRotationInverse = Quaternion.Inverse(StartParticle.transform.rotation);
                var endRotationInverse = Quaternion.Inverse(EndParticle.transform.rotation);

                LocalStartToEndVector = startRotationInverse * (EndParticle.transform.position - StartParticle.transform.position);
                OriginalLength = LocalStartToEndVector.magnitude;

                if (OriginalLength < Math.MinVectorLength)
                {
                    Debug.LogError("Link length is below the limit for safe normalization.");
                    enabled = false;
                    return;
                }

                LocalStartToEndDirection = LocalStartToEndVector / OriginalLength;

                LocalEndToStartVector = endRotationInverse * (StartParticle.transform.position - EndParticle.transform.position);
                LocalEndToStartDirection = LocalEndToStartVector / OriginalLength;

                EndRotationRelativeToStartRotation = startRotationInverse * EndParticle.transform.rotation;
                StartRotationRelativeToEndRotation = endRotationInverse * StartParticle.transform.rotation;
            }
            else if (Application.isPlaying)
            {
                Debug.LogError("Link constraint is missing a start or end particle.");
                enabled = false;
            }

            _initialized = true;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!StartParticle || !EndParticle) return;

            Gizmos.color = Color.green;
            if (enabled)
            {
                // Use the public methods to get world space positions
                Vector3 startWorldPosition = StartParticle.GetWorldSpacePosition();
                Vector3 endWorldPosition = EndParticle.GetWorldSpacePosition();
                
                Gizmos.DrawLine(startWorldPosition, endWorldPosition);

                if (AngleLimiting > Mathf.Epsilon)
                {
                    // Necessary update for accurate gizmos when editing structures outside of play mode.
                    if (!Application.isPlaying)
                        LocalStartToEndDirection = (Quaternion.Inverse(StartParticle.transform.rotation) * (EndParticle.transform.position - StartParticle.transform.position)).normalized;

                    var coneApexAngle = Mathf.PI * (1f - AngleLimiting);
                    var coneBaseRadius = Mathf.Sin(coneApexAngle) * StartParticle.ScaledCollisionRadius;
                    var coneHeight = Mathf.Cos(coneApexAngle) * StartParticle.ScaledCollisionRadius;

                    var coneRotation = Quaternion.FromToRotation(-Vector3.forward, StartParticle.Rotation * LocalStartToEndDirection);

                    DynamicsGizmos.DrawWireCone(startWorldPosition, coneHeight, coneBaseRadius, coneRotation, Color.yellow, 10);
                }
            }
        }
#endif
    }
}
