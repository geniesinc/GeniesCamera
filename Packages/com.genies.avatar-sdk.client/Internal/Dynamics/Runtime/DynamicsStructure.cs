using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Diagnostics = System.Diagnostics;
using System.Text;
using Genies.Utilities;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Genies.Components.Dynamics
{
    /// <summary>
    /// A dynamics structure represents a collection of particles, constrains, and colliders that when combined create a simulated soft body object.
    /// </summary>
    public class DynamicsStructure : MonoBehaviour
    {
        // The threshhold for anchored particle position and rotation at which values are directly copied and not lerped.
        private const float _fullAnchorLimit = 0.99f;

        // Used to keep parameters such as anchoring and stretchiness feeling consistent over variable timestep.
        private const float _referenceDt = 0.02f;

        private const string _dynamicsStepMethodName = "SpecifiedFPSPhysicsStep";
        private const string _transformsUpdateMethodName = "SpecifiedFPSTransformsUpdate";

        // Kernal numbers in the corresponding compute shader
        private const int _sphereCollisionKernal = 0;
        private const int _capsuleCollisionKernal = 1;

        [Range(1, 8)] [Tooltip(DynamicsTooltips.Iterations)]
        public int Iterations = 1;

        private const float _maxPreWarmTime = 5f;
        public const float DefaultPreWarmTime = 0.125f;

        [Range(0, _maxPreWarmTime)] [Tooltip(DynamicsTooltips.PreWarmTime)]
        public float PreWarmTime = DefaultPreWarmTime;

        public Vector3 Gravity = new(0f, -9.8f, 0f);

        [Range(0, 1)] [Tooltip(DynamicsTooltips.Friction)]
        public float Friction = 0.25f;

        [Tooltip(DynamicsTooltips.ParticleToParticleCollision)]
        public bool ParticleToParticleCollision;

        public List<DynamicsParticle> Particles;
        public List<DynamicsCollider> Colliders;
        public List<DynamicsLink> Links;

        //internal data
        private ParticleDataJobs[] _particles;
        private ColliderDataJobs[] _colliders;
        private int _particleCount;
        private int _colliderCount;

        [Tooltip(DynamicsTooltips.PauseStructure)] [SerializeField]
        private bool _paused;

        [Tooltip(DynamicsTooltips.ShowHomeTransforms)] [SerializeField]
        private bool _showHomeTransforms;

        [Tooltip(DynamicsTooltips.ShowStatistics)] [SerializeField]
        private bool _showStatistics;

        public ComputeShader ComputeShader;

        public enum ComputeMethod
        {
            CPU_Single_Thread = 0,
            CPU_JOBS = 1,
            GPU_Compute_Shader = 8
            // GPU_Pseudo_Compute_Shader = 9
        }

        [Tooltip(DynamicsTooltips.ComputeMethod)]
        public ComputeMethod CollisionComputeMethod;

        public enum UpdateMethod
        {
            Fixed_Update = 0,
            Update = 1,
            Specified_FPS = 2
        }

        [Tooltip(DynamicsTooltips.UpdateMethod)]
        public UpdateMethod DynamicsUpdateMethod;

        [Tooltip(DynamicsTooltips.SfpsDynamicsFPS)] [Range(1, 500)]
        public float SfpsDynamicsFPS = 60;

        private float _sfpsRunningDynamicsFPS;
        private float _sfpsDynamicsDt;

        [Tooltip(DynamicsTooltips.SfpsUpdateFPS)] [Range(1, 500)]
        public float SfpsUpdateFPS = 15;

        private float _sfpsRunningUpdateFPS;
        private float _sfpsUpdateDt;

        private bool _specifiedFPSRunning = false;

        // Performance tracking
        private float _ticks;

        // Delta time
        private float _dt;

        // Tracks the previous delta time for use in variable time interval Verlet Integration.
        private float _lastDt = _referenceDt;

        // Delta time scaled relative to reference delta time. Used to keep parameters feeling consistent with variable timestep.
        private float _dtScale;

        // The return to home amount, which ranges from 0 to 1 inclusive, will be increased in order to stabilize the structure.
        // Higher values cause the particles to return to their home positions more quickly.
        // A value of 1 will cause the particles to return to their home positions immediately.
        // The velocity and target variables are used for smoothing this value to avoid sudden jumps.
        private float _returnToHomeAmount, _returnToHomeVelocity, _returnToHomeTarget;

        // This will be set if a prewarm operation is requested.
        private bool _prewarmStructureForNextFrame;

        private void Awake()
        {
            DynamicsManager.Instance?.DynamicsSctructureAdded(this);
        }

        private void Start()
        {
            PreWarm();
        }

        /// <summary>
        /// Hold all particles in this structure in a static position and prevent any dynamics calculations.
        /// </summary>
        public void Pause() => _paused = true;

        /// <summary>
        /// Resume dynamics simulation after being paused.
        /// </summary>
        public void Resume() => _paused = false;

        public void BuildFromRecipe(DynamicsRecipe recipe, Transform rootTransform)
        {
            DynamicsUpdateMethod = DynamicsManager.DefaultUpdateMethod;
            SfpsDynamicsFPS = recipe.SfpsDynamicsFPS;
            SfpsUpdateFPS = recipe.SfpsUpdateFPS;
            Iterations = recipe.Iterations;
            PreWarmTime = recipe.PreWarmTime;
            Gravity = recipe.Gravity;
            Friction = recipe.Friction;
            CollisionComputeMethod = DynamicsManager.DefaultComputeMethod;
            ParticleToParticleCollision = recipe.ParticleToParticleCollision;

            Particles = new List<DynamicsParticle>();
            Links = new List<DynamicsLink>();
            Colliders = new List<DynamicsCollider>();

            //jobs helper allocations
            _particleCount = recipe.ParticleRecipes.Count;
            _particles = new ParticleDataJobs[_particleCount];
            _colliderCount = recipe.SphereColliderRecipes.Count + recipe.CapsuleColliderRecipes.Count;
            _colliders = new ColliderDataJobs[_colliderCount];

            Dictionary<string, Transform> childrenByName = rootTransform.GetChildrenByName(includeSelf: true);

            for (var x = 0; x < recipe.ParticleRecipes.Count; x++)
            {
                ParticleRecipe particleRecipe = recipe.ParticleRecipes[x];
                if (!childrenByName.TryGetValue(particleRecipe.TargetObjectName, out Transform particleTransform))
                {
                    Debug.Log($"Failed to find target object for dynamics particle {particleRecipe.TargetObjectName}");
                    continue;
                }

                DynamicsParticle dynamicsParticle = particleTransform.gameObject.AddComponent<DynamicsParticle>();
                dynamicsParticle.PositionAnchor = particleRecipe.PositionAnchor;
                dynamicsParticle.RotationAnchor = particleRecipe.RotationAnchor;
                dynamicsParticle.CollisionEnabled = particleRecipe.CollisionEnabled;
                dynamicsParticle.CollisionRadius = particleRecipe.CollisionRadius;
                dynamicsParticle.CollisionOffset = particleRecipe.CollisionOffset;
                dynamicsParticle.AffectsTransform = particleRecipe.AffectsTransform;

                dynamicsParticle.Initialize();

                Particles.Add(dynamicsParticle);

                //jobs allocation
                _particles[x] = new ParticleDataJobs()
                {
                    CurrentPosition = dynamicsParticle.CurrentPosition,
                    CollisionEnabled = dynamicsParticle.CollisionEnabled,
                    CurrentCollisionCenter = dynamicsParticle.CurrentCollisionCenter,
                    WorldSpaceCollisionCenter = dynamicsParticle.WorldSpaceCollisionCenter,
                    ModelSpaceRotation = dynamicsParticle.ModelSpaceReference != null ? dynamicsParticle.ModelSpaceReference.transform.rotation : Quaternion.identity,
                };
            }

            foreach (LinkRecipe linkRecipe in recipe.LinkRecipes)
            {
                if (!childrenByName.TryGetValue(linkRecipe.StartParticleObjectName, out Transform startTransform))
                {
                    Debug.Log($"Failed to find target object for dynamics link {linkRecipe.StartParticleObjectName}");
                    continue;
                }

                if (!childrenByName.TryGetValue(linkRecipe.EndParticleObjectName, out Transform endTransform))
                {
                    Debug.Log($"Failed to find target object for dynamics link {linkRecipe.EndParticleObjectName}");
                    continue;
                }

                DynamicsLink dynamicsLink = startTransform.gameObject.AddComponent<DynamicsLink>();

                dynamicsLink.Stretchiness = linkRecipe.Stretchiness;
                dynamicsLink.MaintainStartParticleRotation = linkRecipe.MaintainStartParticleRotation;
                dynamicsLink.MaintainEndParticleRotation = linkRecipe.MaintainEndParticleRotation;

                dynamicsLink.AnchorToStartParticleRotation = linkRecipe.AnchorToStartParticleRotation;
                dynamicsLink.AngleLimiting = linkRecipe.AngleLimiting;

                dynamicsLink.StartParticle = startTransform.GetComponent<DynamicsParticle>();
                dynamicsLink.EndParticle = endTransform.GetComponent<DynamicsParticle>();

                dynamicsLink.Initialize();

                Links.Add(dynamicsLink);
            }

            for (var x = 0; x < recipe.SphereColliderRecipes.Count; x++)
            {
                SphereColliderRecipe sphereColliderRecipe = recipe.SphereColliderRecipes[x];
                if (!childrenByName.TryGetValue(sphereColliderRecipe.TargetObjectName, out Transform target))
                {
                    Debug.Log($"Failed to find target object for dynamics sphere collider: {sphereColliderRecipe.TargetObjectName}");
                    continue;
                }

                DynamicsSphereCollider collider = target.gameObject.AddComponent<DynamicsSphereCollider>();
                collider.CollisionRadius = sphereColliderRecipe.CollisionRadius;
                collider.Offset = sphereColliderRecipe.Offset;
                Colliders.Add(collider);
                
                //jobs allocation
                _colliders[x] = new ColliderDataJobs() { IsSphere = true };
            }

            var offset = recipe.SphereColliderRecipes.Count;
            for (var x = 0; x < recipe.CapsuleColliderRecipes.Count; x++)
            {
                CapsuleColliderRecipe capsuleColliderRecipe = recipe.CapsuleColliderRecipes[x];
                if (!childrenByName.TryGetValue(capsuleColliderRecipe.TargetObjectName, out Transform target))
                {
                    Debug.Log($"Failed to find target object for dynamics capsule collider: {capsuleColliderRecipe.TargetObjectName}");
                    continue;
                }

                DynamicsCapsuleCollider collider = target.gameObject.AddComponent<DynamicsCapsuleCollider>();
                collider.Height = capsuleColliderRecipe.Height;
                collider.Offset = capsuleColliderRecipe.Offset;
                collider.Rotation = capsuleColliderRecipe.Rotation;
                collider.CollisionRadius = capsuleColliderRecipe.CollisionRadius;
                Colliders.Add(collider);
                
                //jobs allocation
                _colliders[offset + x] = new ColliderDataJobs() { IsCapsule = true };
            }
        }
        
        //for when the number of particles or colliders changes
        private void ReallocateJobsHeap()
        {
            //allocate
            _particleCount = Particles.Count;
            _particles = new ParticleDataJobs[_particleCount];
            _colliderCount = Colliders.Count;
            _colliders = new ColliderDataJobs[_colliderCount];
            
            //fill particles data
            for (var x = 0; x < Particles.Count; x++)
            {
                var p = Particles[x];
                _particles[x] = new ParticleDataJobs()
                {
                    CurrentPosition = p.CurrentPosition,
                    CollisionEnabled = p.CollisionEnabled,
                    CurrentCollisionCenter = p.CurrentCollisionCenter,
                    WorldSpaceCollisionCenter = p.WorldSpaceCollisionCenter,
                    ModelSpaceRotation = p.ModelSpaceReference != null ? p.ModelSpaceReference.transform.rotation : Quaternion.identity,
                };
            }
            
            //fill colliders data
            for (var x = 0; x < Colliders.Count; x++)
            {
                var c = Colliders[x];
                if (c is DynamicsSphereCollider sC)
                {
                    _colliders[x] = new ColliderDataJobs() { IsSphere = true };
                }
                else if (c is DynamicsCapsuleCollider cC)
                {
                    _colliders[x] = new ColliderDataJobs() { IsCapsule = true };
                }
            }
        }

        public void ResetParticlesToHomeTransforms()
        {
            if (Particles == null)
            {
                return;
            }

            foreach (DynamicsParticle particle in Particles)
            {
                if (particle != null)
                {
                    particle.ResetToHomeTransform();
                }
            }
        }

        public void LerpParticlesToHomePositionAndRotation(float amount)
        {
            if (Particles == null)
            {
                return;
            }

            foreach (DynamicsParticle particle in Particles)
            {
                if (particle != null)
                {
                    particle.LerpToHomePositionAndRotation(amount);
                }
            }
        }

        private void FixedUpdate()
        {
            // Keep all particles static when paused.
            if (_paused)
            {
                return;
            }

            if (DynamicsUpdateMethod != UpdateMethod.Fixed_Update)
            {
                return;
            }

            for (var iteration = 0; iteration < Iterations; iteration++)
            {
                PerformPhysicsStep(Time.fixedDeltaTime / Iterations);
            }
        }

        private void Update()
        {
            // Keep all particles static when paused.
            if (_paused)
            {
                return;
            }

            // Update fully anchored particles
            foreach (DynamicsParticle particle in Particles)
            {
                if (!particle)
                {
                    continue;
                }

                // Anchor to original non-dynamic position
                if (particle.PositionAnchor > _fullAnchorLimit)
                {
                    // This is a special case where the positional anchoring of the particle is locked 1 to 1 with it's game object transform.
                    // For full anchoring, we need to use the world space home position converted to model space
                    if (particle.ModelSpaceReference != null)
                    {
                        Transform modelSpaceTransform = particle.ModelSpaceReference.transform;
                        particle.CurrentPosition = modelSpaceTransform.InverseTransformPoint(particle.HomePosition);
                    }
                    else
                    {
                        particle.CurrentPosition = particle.HomePosition;
                    }
                }

                // Anchor to original non-dynamic rotation
                if (particle.RotationAnchor > _fullAnchorLimit)
                {
                    // This is a special case where the rotational anchoring of the particle is locked 1 to 1 with it's game object transform.
                    // For full anchoring, we need to use the world space home rotation converted to model space
                    if (particle.ModelSpaceReference != null)
                    {
                        Transform modelSpaceTransform = particle.ModelSpaceReference.transform;
                        particle.Rotation = Quaternion.Inverse(modelSpaceTransform.rotation) * particle.HomeRotation;
                    }
                    else
                    {
                        particle.Rotation = particle.HomeRotation;
                    }
                }
            }

            if (DynamicsUpdateMethod == UpdateMethod.Specified_FPS)
            {
                if (_specifiedFPSRunning)
                {
                    if (SfpsDynamicsFPS != _sfpsRunningDynamicsFPS || SfpsUpdateFPS != _sfpsRunningUpdateFPS)
                    {
                        CancelSpecifiedFPSUpdates();
                        _specifiedFPSRunning = false;
                    }
                }
                else
                {
                    _sfpsDynamicsDt = 1f / SfpsDynamicsFPS;
                    _sfpsUpdateDt = 1f / SfpsUpdateFPS;
                    InvokeRepeating(_dynamicsStepMethodName, 0f, _sfpsDynamicsDt);
                    InvokeRepeating(_transformsUpdateMethodName, 0f, _sfpsUpdateDt);
                    _specifiedFPSRunning = true;
                    _sfpsRunningDynamicsFPS = SfpsDynamicsFPS;
                    _sfpsRunningUpdateFPS = SfpsUpdateFPS;
                }
            }
            else if (DynamicsUpdateMethod != UpdateMethod.Specified_FPS && _specifiedFPSRunning)
            {
                CancelSpecifiedFPSUpdates();
                _specifiedFPSRunning = false;
            }
            else if (DynamicsUpdateMethod == UpdateMethod.Update)
            {
                for (var iteration = 0; iteration < Iterations; iteration++)
                {
                    PerformPhysicsStep(Time.deltaTime / Iterations);
                }
            }

            // Hard limit enforcement outside of the Verlet Simulation. Performed per-frame to avoid rendering incorrect states.
            if (DynamicsUpdateMethod == UpdateMethod.Fixed_Update || DynamicsUpdateMethod == UpdateMethod.Specified_FPS)
            {
                EnforceLinkAngleLimitsCPU();
            }
        }

        private void LateUpdate()
        {
            if (_prewarmStructureForNextFrame)
            {
                PreWarm();
                _prewarmStructureForNextFrame = false;
            }

            // Keep all particles static when paused.
            if (_paused)
            {
                return;
            }

            // Ensure that the particle transforms are pushed back to their Unity game object transforms after they may have been modified by keyframe animation.
            UpdateUnityTransforms();
        }

        private void CancelSpecifiedFPSUpdates()
        {
            CancelInvoke(_dynamicsStepMethodName);
            CancelInvoke(_transformsUpdateMethodName);
        }

        [Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Method is called via InvokeRepeating")]
        private void SpecifiedFPSPhysicsStep()
        {
            for (var iteration = 0; iteration < Iterations; iteration++)
            {
                PerformPhysicsStep(_sfpsDynamicsDt / Iterations);
            }
        }

        [Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Method is called via InvokeRepeating")]
        private void SpecifiedFPSTransformsUpdate()
        {
            UpdateUnityTransforms();
        }

        private void UpdateUnityTransforms()
        {
            // The transforms from simulation space are applied back to their counterparts Unity transforms.
            // This is done in late update to avoid interference with keyframe animation or any other drivers of motion.
            foreach (DynamicsParticle particle in Particles)
            {
                if (particle)
                {
                    particle.UpdateUnityTransforms();
                }
            }
        }

        private void PerformPhysicsStep(float deltaTime)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var stopwatch = Diagnostics.Stopwatch.StartNew();
#endif

            if (_lastDt <= Mathf.Epsilon)
            {
                Debug.Log("Last delta time invalid, skipping physics step.");
                _lastDt = deltaTime;
                return;
            }

            _dt = deltaTime;

            _dtScale = _dt / _referenceDt;

            // Motion
            SimulateParticleMotionCPU();

            // Particle to Particle Collision
            if (ParticleToParticleCollision)
            {
                CollideParticlesWithParticlesCPU();
            }

            // Constraints
            EnforceLinkConstraintsCPU();

            // Collision
            // Placed last to give collision the strongest effect (prevent clipping)
            if (CollisionComputeMethod == ComputeMethod.CPU_JOBS)
            {
                CollideParticlesWithCollidersJobs();
            }
            else if (CollisionComputeMethod == ComputeMethod.CPU_Single_Thread)
            {
                CollideParticlesWithCollidersCPU();
            }
            else if (CollisionComputeMethod == ComputeMethod.GPU_Compute_Shader)
            {
                CollideParticlesWithCollidersGPU();
            }

            // Enforcement of hard limits to prevent visual defects.
            EnforceLinkAngleLimitsCPU();

            if (DynamicsUpdateMethod != UpdateMethod.Specified_FPS)
            {
                UpdateUnityTransforms();
            }

            // Store last delta time so that velocity can be determined from current and last position.
            _lastDt = _dt;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _ticks = _ticks * 0.99f + stopwatch.ElapsedTicks * Iterations * 0.01f; // Smooth the results to make them more readable.
#endif
        }

        private void SimulateParticleMotionCPU()
        {
            foreach (DynamicsParticle particle in Particles)
            {
                if (!particle)
                {
                    continue;
                }

                // Velocity
                Vector3 velocity = (particle.CurrentPosition - particle.LastPosition) / _lastDt;

                // Friction
                velocity *= Mathf.Exp(-Friction * _dtScale);

                // Store last position
                particle.LastPosition = particle.CurrentPosition;

                // Verlet Integration
                particle.CurrentPosition += (velocity + (Gravity * particle.EffectiveScale) * _dt) * _dt;

                // Anchor to original non-dynamic position
                if (particle.PositionAnchor > _fullAnchorLimit)
                {
                    // This is a special case where the positional anchoring of the particle is locked 1 to 1 with it's game object transform.
                    // For full anchoring, we need to use the world space home position converted to model space
                    if (particle.ModelSpaceReference != null)
                    {
                        Transform modelSpaceTransform = particle.ModelSpaceReference.transform;
                        particle.CurrentPosition = modelSpaceTransform.InverseTransformPoint(particle.HomePosition);
                    }
                    else
                    {
                        particle.CurrentPosition = particle.ModelSpaceHomePositionProperty;
                    }
                }
                else if (particle.PositionAnchor > Mathf.Epsilon)
                {
                    // Utilizing continuous compounding formula using (e) to get better framerate independence
                    var homeAmount = 1f - Mathf.Exp(-particle.PositionAnchor * particle.PositionAnchor * _dtScale);

                    // For partial anchoring, use model space home position
                    particle.CurrentPosition = Vector3.Lerp(particle.CurrentPosition, particle.ModelSpaceHomePositionProperty, homeAmount);
                }

                // Anchor to original non-dynamic rotation
                if (particle.RotationAnchor > _fullAnchorLimit)
                {
                    // This is a special case where the rotational anchoring of the particle is locked 1 to 1 with it's game object transform.
                    // For full anchoring, we need to use the world space home rotation converted to model space
                    if (particle.ModelSpaceReference != null)
                    {
                        Transform modelSpaceTransform = particle.ModelSpaceReference.transform;
                        particle.Rotation = Quaternion.Inverse(modelSpaceTransform.rotation) * particle.HomeRotation;
                    }
                    else
                    {
                        particle.Rotation = particle.ModelSpaceHomeRotationProperty;
                    }
                }
                else if (particle.RotationAnchor > Mathf.Epsilon)
                {
                    var homeReturnRate = particle.RotationAnchor * particle.RotationAnchor;
                    var homeAmount = 1f - Mathf.Exp(-homeReturnRate * _dtScale);

                    // For partial anchoring, use model space home rotation
                    particle.Rotation = Quaternion.Lerp(particle.Rotation, particle.ModelSpaceHomeRotationProperty, homeAmount);
                }
            }
        }

        private void CollideParticlesWithParticlesCPU()
        {
            for (var a = 0; a < Particles.Count; a++)
            {
                for (var b = 0; b < Particles.Count; b++)
                {
                    if (a == b)
                    {
                        continue;
                    }

                    DynamicsParticle particleA = Particles[a];
                    DynamicsParticle particleB = Particles[b];

                    if (!particleA || !particleB || !particleA.CollisionEnabled || !particleB.CollisionEnabled)
                    {
                        continue;
                    }

                    var minDistance = particleB.ScaledCollisionRadius + particleA.ScaledCollisionRadius;

                    Vector3 fromAtoB = particleB.WorldSpaceCollisionCenter - particleA.WorldSpaceCollisionCenter;
                    var sqrDistFromAtoB = fromAtoB.sqrMagnitude;

                    if (sqrDistFromAtoB > Math.MinVectorLengthSqr && sqrDistFromAtoB < minDistance * minDistance)
                    {
                        var distFromAtoB = Mathf.Sqrt(sqrDistFromAtoB);
                        var overlap = minDistance - distFromAtoB;

                        // Higher position anchoring values will give particles the ability to win out in collisions.
                        var correctionBalance = 0.5f - (particleA.PositionAnchor - particleB.PositionAnchor) * 0.5f;

                        Vector3 dirFromAtoB = fromAtoB / distFromAtoB;
                        
                        // Convert world space collision response to model space
                        if (particleA.ModelSpaceReference != null)
                        {
                            Transform modelSpaceTransform = particleA.ModelSpaceReference.transform;
                            Vector3 modelSpaceDir = modelSpaceTransform.InverseTransformDirection(dirFromAtoB);
                            particleB.CurrentPosition += (1f - correctionBalance) * overlap * modelSpaceDir;
                            particleA.CurrentPosition -= correctionBalance * overlap * modelSpaceDir;
                        }
                        else
                        {
                            // Fallback to direct world space response
                            particleB.CurrentPosition += (1f - correctionBalance) * overlap * dirFromAtoB;
                            particleA.CurrentPosition -= correctionBalance * overlap * dirFromAtoB;
                        }
                    }
                }
            }
        }

        private void EnforceLinkConstraintsCPU()
        {
            foreach (DynamicsLink link in Links)
            {
                if (link == null || !link.enabled)
                {
                    continue;
                }

                DynamicsParticle startParticle = link.StartParticle;
                DynamicsParticle endParticle = link.EndParticle;

                // Respect if the start or end particle has a greater position anchoring value and correct on the other end.
                var startToEndCorrectionBalance = 0.5f - (startParticle.PositionAnchor - endParticle.PositionAnchor) * 0.5f;


                Vector3 linkDelta = endParticle.CurrentPosition - startParticle.CurrentPosition;
                var linkDistanceSqr = linkDelta.sqrMagnitude;

                // If the link has become too small we cannot determine its direction.
                // The assumption here will be that the velocities of the particles will lengthen the link in most cases.
                if (linkDistanceSqr < Math.MinVectorLengthSqr)
                {
                    continue;
                }

                var linkDistance = Mathf.Sqrt(linkDistanceSqr);

                Vector3 linkDirectionWorld = linkDelta / linkDistance;

                var lengthError = linkDistance - link.ScaledLength;

                lengthError *= 1f - Mathf.Exp((link.Stretchiness - 1f) * _dtScale);

                // Correct the length of the link
                startParticle.CurrentPosition += lengthError * startToEndCorrectionBalance * linkDirectionWorld;
                endParticle.CurrentPosition += (startToEndCorrectionBalance - 1f) * lengthError * linkDirectionWorld;


                // Correct the positions for particles with anchored rotations
                if (link.AnchorToStartParticleRotation > Mathf.Epsilon)
                {
                    // Refresh values above
                    linkDelta = endParticle.CurrentPosition - startParticle.CurrentPosition;
                    linkDistance = linkDelta.magnitude;

                    if (linkDistance < Math.MinVectorLength)
                    {
                        continue;
                    }

                    // Find the position that the end of the link would be in if it's position was maintained respective to the start particle rotation.
                    Vector3 anchoredEndPosition = link.StartParticle.Rotation * link.LocalStartToEndDirection * linkDistance;

                    // Correct the end position
                    // Utilizing continuous compounding formula using (e) to get better framerate independence
                    var returnAmount = 1f - Mathf.Exp(-link.AnchorToStartParticleRotation * link.AnchorToStartParticleRotation * _dtScale);
                    Vector3 newEndPosition = startParticle.CurrentPosition + Vector3.Slerp(linkDelta, anchoredEndPosition, returnAmount);
                    Vector3 changeFromCurrentPosition = newEndPosition - endParticle.CurrentPosition;
                    endParticle.CurrentPosition = newEndPosition;

                    // We need to avoid adding energy to the simulation. While it would be possible to add the opposing velocity to the start particle,
                    // The calculations would be rather expensive. This simple fix is best for now, however, the added effect could provide useful in
                    // advanced structures. TODO: Explore feedback of Verlet velocity from start particle to end particle.
                    endParticle.LastPosition += changeFromCurrentPosition;

                    // Refresh values once again to prep for rotation correction
                    linkDelta = endParticle.CurrentPosition - startParticle.CurrentPosition;
                    linkDistance = linkDelta.magnitude;

                    if (linkDistance < Math.MinVectorLength)
                    {
                        continue;
                    }

                    linkDirectionWorld = linkDelta / linkDistance;
                }

                if (link.MaintainStartParticleRotation || link.MaintainEndParticleRotation)
                {
                    // Particle Rotation Correction
                    // The start and end particles will be oriented differently with respect to one another at this stage.
                    // 1. Find corrective rotation that would re-align the start particle to face the end particle
                    Vector3 linkDirectionLocal = Quaternion.Inverse(startParticle.Rotation) * linkDirectionWorld;
                    var startCorrectiveRotation = Quaternion.FromToRotation(link.LocalStartToEndDirection, linkDirectionLocal);

                    // 2. The end particle rotation is easy to correct since we stored the original rotation relative to the start particle.
                    if (link.MaintainEndParticleRotation)
                    {
                        endParticle.Rotation = startParticle.Rotation * startCorrectiveRotation * link.EndRotationRelativeToStartRotation;
                    }

                    // 3. Apply the rotation correction to the start.
                    if (link.MaintainStartParticleRotation)
                    {
                        startParticle.Rotation *= startCorrectiveRotation;
                    }
                }
            }
        }

        private void EnforceLinkAngleLimitsCPU()
        {
            foreach (DynamicsLink link in Links)
            {
                if (link.AngleLimiting > Mathf.Epsilon)
                {
                    var dotProductLimit = Mathf.Cos(Mathf.PI * (1f - link.AngleLimiting));

                    Vector3 linkVector = link.EndParticle.CurrentPosition - link.StartParticle.CurrentPosition;
                    var linkLength = linkVector.magnitude;
                    if (linkLength < Math.MinVectorLength)
                    {
                        continue;
                    }

                    Vector3 linkDirection = linkVector / linkLength;
                    Vector3 homeDirection = link.StartParticle.Rotation * link.LocalStartToEndDirection;

                    var currentDotProduct = Vector3.Dot(linkDirection, homeDirection);

                    if (currentDotProduct < dotProductLimit)
                    {
                        // Calculate the angle between Vector A and Vector B in radians
                        var angleToHome = Mathf.Acos(currentDotProduct);

                        // Calculate the angle corresponding to the dot product limit in radians
                        var limitAngle = Mathf.Acos(dotProductLimit);

                        // Calculate the interpolation factor
                        var neededInterpolation = (angleToHome - limitAngle) / angleToHome;

                        linkDirection = Vector3.Slerp(linkDirection, homeDirection, neededInterpolation);

                        // Update the position of the end particle
                        var newEndPosition = link.StartParticle.CurrentPosition + linkDirection * linkLength;
                        var changeFromCurrentPosition = newEndPosition - link.EndParticle.CurrentPosition;
                        link.EndParticle.CurrentPosition = newEndPosition;

                        // Here we avoid adding energy to the Verlet simulation by simply removing the extra velocty we added.
                        // A more detailed explanation is described in the link rotation anchoring section.
                        link.EndParticle.LastPosition += changeFromCurrentPosition;

                        link.EndParticle.UpdateUnityTransforms();
                    }
                }
            }
        }

        private void CollideParticlesWithCollidersGPU()
        {
            if (Colliders.Count == 0)
            {
                return;
            }

            // Skip GPU collision for model space particles - use CPU collision instead
            if (Particles.Count > 0 && Particles[0].ModelSpaceReference != null)
            {
                CollideParticlesWithCollidersCPU();
                return;
            }

            var sphereColliderCount = Colliders.Count(c => c is DynamicsSphereCollider);
            var capsuleColliderCount = Colliders.Count(c => c is DynamicsCapsuleCollider);

            var particleData = new ParticleData[Particles.Count];
            var sphereColliderData = new SphereColliderData[sphereColliderCount];
            var capsuleColliderData = new CapsuleColliderData[capsuleColliderCount];

            for (var i = 0; i < Particles.Count; i++)
            {
                particleData[i] = Particles[i]
                    ? new ParticleData()
                    {
                        CollisionEnabled = Particles[i].CollisionEnabled ? 1 : 0,
                        CurrentCollisionCenter = Particles[i].WorldSpaceCollisionCenter,
                        CollisionRadius = Particles[i].ScaledCollisionRadius
                    }
                    : new ParticleData();
            }

            var sphereColliderIndex = 0;
            var capsuleColliderIndex = 0;

            foreach (DynamicsCollider collider in Colliders)
            {
                if (collider is DynamicsSphereCollider sphereCollider)
                {
                    sphereColliderData[sphereColliderIndex++] =
                        new SphereColliderData() { Position = sphereCollider.Center, CollisionRadius = sphereCollider.ScaledCollisionRadius };
                }
                else if (collider is DynamicsCapsuleCollider capsuleCollider)
                {
                    capsuleColliderData[capsuleColliderIndex++] = new CapsuleColliderData()
                    {
                        StartPosition = capsuleCollider.StartPosition, EndPosition = capsuleCollider.EndPosition, CollisionRadius = capsuleCollider.ScaledCollisionRadius
                    };
                }
            }

            var particleDataSize = 20; // 4B int size collision enabled + 4B float size * (3B Position + 1B Radius)

            var particlesBuffer = new ComputeBuffer(Particles.Count, Particles.Count * particleDataSize);


            // Sphere Collision
            if (sphereColliderCount > 0)
            {
                particlesBuffer.SetData(particleData);
                ComputeShader.SetBuffer(_sphereCollisionKernal, "Particles", particlesBuffer);

                var sphereColliderDataSize = 16; // 4B float size * (3B Position + 1B Radius)

                var sphereCollidersBuffer = new ComputeBuffer(sphereColliderCount, sphereColliderCount * sphereColliderDataSize);
                sphereCollidersBuffer.SetData(sphereColliderData);
                ComputeShader.SetBuffer(_sphereCollisionKernal, "SphereColliders", sphereCollidersBuffer);

                ComputeShader.Dispatch(_sphereCollisionKernal, particleData.Length, sphereColliderData.Length, 1);

                sphereCollidersBuffer.Dispose();

                // Retrieve updated particle positions
                particlesBuffer.GetData(particleData);
            }

            // Capsule Collision
            if (capsuleColliderCount > 0)
            {
                particlesBuffer.SetData(particleData);
                ComputeShader.SetBuffer(_capsuleCollisionKernal, "Particles", particlesBuffer);

                var capsuleColliderDataSize = 28; // 4B float size * (3B Start Position + 3B End Position + 1B Radius)

                particlesBuffer.SetData(particleData);

                var capsuleCollidersBuffer = new ComputeBuffer(capsuleColliderCount, capsuleColliderCount * capsuleColliderDataSize);
                capsuleCollidersBuffer.SetData(capsuleColliderData);
                ComputeShader.SetBuffer(_capsuleCollisionKernal, "CapsuleColliders", capsuleCollidersBuffer);

                ComputeShader.Dispatch(_capsuleCollisionKernal, particleData.Length, capsuleColliderData.Length, 1);

                capsuleCollidersBuffer.Dispose();

                // Retrieve updated particle positions
                particlesBuffer.GetData(particleData);
            }

            // Update particle positions with updated positions in the buffer.
            for (var i = 0; i < Particles.Count; i++)
            {
                if (Particles[i])
                {
                    // Convert world space collision center back to model space position
                    if (Particles[i].ModelSpaceReference != null)
                    {
                        Transform modelSpaceTransform = Particles[i].ModelSpaceReference.transform;
                        Vector3 worldCollisionCenter = particleData[i].CurrentCollisionCenter;
                        Vector3 worldPosition = worldCollisionCenter - Particles[i].Rotation * Particles[i].CollisionOffset;
                        Particles[i].CurrentPosition = modelSpaceTransform.InverseTransformPoint(worldPosition);
                    }
                    else
                    {
                        // Fallback to direct assignment
                        Particles[i].CurrentPosition = particleData[i].CurrentCollisionCenter - Particles[i].Rotation * Particles[i].CollisionOffset;
                    }
                }
            }

            particlesBuffer.Dispose();
        }

        private void CollideParticlesWithCollidersCPU()
        {
            foreach (DynamicsCollider collider in Colliders)
            {
                foreach (DynamicsParticle particle in Particles)
                {
                    if (!collider || !particle || !particle.CollisionEnabled)
                    {
                        continue;
                    }

                    if (collider is DynamicsSphereCollider sphereCollider)
                    {
                        Vector3 colliderCenterToParticleCollisionCenter = particle.WorldSpaceCollisionCenter - sphereCollider.Center;
                        var sqrDistance = Vector3.SqrMagnitude(colliderCenterToParticleCollisionCenter);
                        var minDistance = particle.ScaledCollisionRadius + sphereCollider.ScaledCollisionRadius;

                        // Avoid small values that could compromise stability. Assume other factors will correct this on subsequent iterations.
                        if (sqrDistance < Math.MinVectorLengthSqr)
                        {
                            continue;
                        }

                        if (sqrDistance > minDistance * minDistance)
                        {
                            continue; // Avoid Square Root
                        }

                        var distance = Mathf.Sqrt(sqrDistance);
                        if (distance < minDistance)
                        {
                            Vector3 ejectionDirection = colliderCenterToParticleCollisionCenter / distance; // Unit vector in direction particle needs to be pushed.
                            
                            // Convert world space collision response to model space
                            if (particle.ModelSpaceReference != null)
                            {
                                Transform modelSpaceTransform = particle.ModelSpaceReference.transform;
                                Vector3 modelSpaceDir = modelSpaceTransform.InverseTransformDirection(ejectionDirection);
                                particle.CurrentPosition += modelSpaceDir * (minDistance - distance);
                            }
                            else
                            {
                                // Fallback to direct world space response
                                particle.CurrentPosition += ejectionDirection * (minDistance - distance);
                            }
                        }
                    }
                    else if (collider is DynamicsCapsuleCollider capsuleCollider)
                    {
                        Vector3 closestPoint = Math.ClosestPointOnSegment(particle.WorldSpaceCollisionCenter, capsuleCollider.StartPosition, capsuleCollider.EndPosition);
                        Vector3 ejectionVector = particle.WorldSpaceCollisionCenter - closestPoint;
                        var sqrDistance = ejectionVector.sqrMagnitude;

                        // Avoid small values that could compromise stability. Assume other factors will correct this on subsequent iterations.
                        if (sqrDistance < Math.MinVectorLengthSqr)
                        {
                            continue;
                        }

                        var distance = Mathf.Sqrt(sqrDistance);
                        var distanceError = (capsuleCollider.ScaledCollisionRadius + particle.ScaledCollisionRadius) - distance;

                        if (distanceError > 0f)
                        {
                            Vector3 ejectionDirection = ejectionVector / distance;

                            // Convert world space collision response to model space
                            if (particle.ModelSpaceReference != null)
                            {
                                Transform modelSpaceTransform = particle.ModelSpaceReference.transform;
                                Vector3 modelSpaceDir = modelSpaceTransform.InverseTransformDirection(ejectionDirection);
                                particle.CurrentPosition += modelSpaceDir * distanceError;
                            }
                            else
                            {
                                // Fallback to direct world space response
                                particle.CurrentPosition += ejectionDirection * distanceError;
                            }
                        }
                    }
                }
            }
        }
        
        //update the particle position based on the colliders, utilizing the jobs and burst system
        private void CollideParticlesWithCollidersJobs()
        {
            if (Particles == null || Colliders == null)
            {
                return;
            }

            //in case particles or colliders added
            if (_particleCount != Particles.Count || _colliderCount != Colliders.Count)
            {
                ReallocateJobsHeap();
            }

            //set particles data
            for (var x = 0; x < _particles.Length; x++)
            {
                var data = Particles[x];
                _particles[x].CollisionEnabled = data.CollisionEnabled;
                _particles[x].CurrentCollisionCenter = data.CurrentCollisionCenter;
                _particles[x].WorldSpaceCollisionCenter = data.WorldSpaceCollisionCenter;
                _particles[x].ScaledCollisionRadius = data.ScaledCollisionRadius;
                _particles[x].Rotation = data.Rotation;
                _particles[x].CollisionOffset = data.CollisionOffset;
                _particles[x].CurrentPosition = data.CurrentPosition;
                _particles[x].ModelSpaceRotation = data.ModelSpaceReference != null ? data.ModelSpaceReference.transform.rotation : _particles[x].ModelSpaceRotation = Quaternion.identity;
            }

            //set colliders data
            for (var x = 0; x < _colliders.Length; x++)
            {
                var data = Colliders[x];
                if (data is DynamicsSphereCollider sCollider)
                {
                    _colliders[x].IsSphere = true;
                    _colliders[x].Center = sCollider.Center;
                    _colliders[x].ScaledCollisionRadius = sCollider.ScaledCollisionRadius;
                }
                else if (data is DynamicsCapsuleCollider cCollider)
                {
                    _colliders[x].IsCapsule = true;
                    _colliders[x].StartPosition = cCollider.StartPosition;
                    _colliders[x].EndPosition = cCollider.EndPosition;
                    _colliders[x].ScaledCollisionRadius = cCollider.ScaledCollisionRadius;
                }
            }

            //run job
            unsafe
            {
                fixed (ColliderDataJobs* colliderPtr = _colliders)
                {
                    fixed (ParticleDataJobs* particlePtr = _particles)
                    {
                        for (var x = 0; x < Colliders.Count; x++)
                        {
                            CollideParticlesJob job = new CollideParticlesJob { Particles = particlePtr, Colliders = colliderPtr, ColliderIndex = x };

                            JobHandle handle = job.Schedule(_particles.Length, 64);
                            handle.Complete();
                        }
                    }
                }
            }

            //set particle positions
            for (var x = 0; x < _particles.Length; x++)
            {
                var data = _particles[x];
                Particles[x].CurrentPosition = data.CurrentPosition;
            }
        }

        [BurstCompile]
        public unsafe struct CollideParticlesJob : IJobParallelFor
        {
            [NativeDisableUnsafePtrRestriction] public unsafe ParticleDataJobs* Particles;
            [NativeDisableUnsafePtrRestriction] public unsafe ColliderDataJobs* Colliders;

            public int ColliderIndex;

            public void Execute(int index)
            {
                var particle = Particles[index];
                var collider = Colliders[ColliderIndex];

                if (!particle.CollisionEnabled)
                {
                    return;
                }

                if (collider.IsSphere)
                {
                    Vector3 colliderCenterToParticleCollisionCenter = particle.WorldSpaceCollisionCenter - collider.Center;
                    var sqrDistance = Vector3.SqrMagnitude(colliderCenterToParticleCollisionCenter);
                    var minDistance = particle.ScaledCollisionRadius + collider.ScaledCollisionRadius;

                    if (sqrDistance < Math.MinVectorLengthSqr)
                    {
                        return;
                    }

                    if (sqrDistance > minDistance * minDistance)
                    {
                        return;
                    }

                    var distance = Mathf.Sqrt(sqrDistance);
                    if (distance < minDistance)
                    {
                        Vector3 ejectionDirection = colliderCenterToParticleCollisionCenter / distance;
                        // Apply collision response in world space (CurrentPosition is world space in job system)
                        Vector3 modelSpaceDir = Quaternion.Inverse(particle.ModelSpaceRotation) * ejectionDirection;
                        particle.CurrentPosition += modelSpaceDir * (minDistance - distance);
                        particle.CurrentCollisionCenter = particle.CurrentPosition + particle.Rotation * particle.CollisionOffset;
                        Particles[index] = particle;
                    }
                }
                else if (collider.IsCapsule)
                {
                    Vector3 closestPoint = Math.ClosestPointOnSegment(particle.WorldSpaceCollisionCenter, collider.StartPosition, collider.EndPosition);
                    Vector3 ejectionVector = particle.WorldSpaceCollisionCenter - closestPoint;
                    var sqrDistance = ejectionVector.sqrMagnitude;

                    if (sqrDistance < Math.MinVectorLengthSqr)
                    {
                        return;
                    }

                    var distance = Mathf.Sqrt(sqrDistance);
                    var distanceError = (collider.ScaledCollisionRadius + particle.ScaledCollisionRadius) - distance;

                    if (distanceError > 0f)
                    {
                        Vector3 ejectionDirection = ejectionVector / distance;
                        // Apply collision response in world space (CurrentPosition is world space in job system)
                        Vector3 modelSpaceDir = Quaternion.Inverse(particle.ModelSpaceRotation) * ejectionDirection;
                        particle.CurrentPosition += modelSpaceDir * distanceError;
                        particle.CurrentCollisionCenter = particle.CurrentPosition + particle.Rotation * particle.CollisionOffset;
                        Particles[index] = particle;
                    }
                }
            }
        }

        /// <summary>
        /// Runs the dynamics simulation in the background to prepare for a brand new pose.
        /// </summary>
        public void PreWarm()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var stopwatch = Diagnostics.Stopwatch.StartNew();
#endif

            foreach (var particle in Particles)
            {
                particle.Reset();
            }

            foreach (var link in Links)
            {
                link.Initialize();
            }

            // The time intervals between physics steps can be longer in the beginning
            // of the pre-warm process. This will reduce the computational cost and allow
            // the structure to more quickly move to its stable position. Towards the end
            // of the pre-warm process the time intervals must match the actual intervals
            // in the live simulation as closely as possible. This will avoid bouncing
            // upon the start of the simulation.
            var preWarmStepTimes = new Stack<float>();

            var nativeStepTime = _referenceDt / Iterations;

            const float maxPreWarmStepIntervalTime = 0.125f;

            // Calculate and store a set of time intervals that smoothly flows from
            // coarse efficient intervals to the native time interval.
            var sumOfPreWarmIntervals = 0f;
            while (sumOfPreWarmIntervals < PreWarmTime)
            {
                var timeStep = Mathf.SmoothStep(maxPreWarmStepIntervalTime, nativeStepTime, 1f - (sumOfPreWarmIntervals / _maxPreWarmTime));
                preWarmStepTimes.Push(timeStep);
                sumOfPreWarmIntervals += timeStep;
            }

            var preWarmSteps = preWarmStepTimes.Count;

            while (preWarmStepTimes.Any())
            {
                PerformPhysicsStep(preWarmStepTimes.Pop());
            }

            // Initialize particles
            foreach (DynamicsParticle particle in Particles)
            {
                if (particle)
                {
                    particle.LastHomePosition = particle.HomePosition; // Update global home position for each particle to prevent max velocity correction.
                    particle.LastPosition = particle.CurrentPosition; // Zero velocity
                }
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (DynamicsManager.LogPreWarmTime)
                Debug.Log($"Dynamics Structure ({gameObject.name}) - Pre-Warmed by {preWarmSteps} physics steps in {stopwatch.ElapsedMilliseconds} ms.");
#endif
        }

        /// <summary>
        /// Notify the structure that the next frame contains a new pose of the underlying hierarchy and therefore requires a prewarm operation.
        /// </summary>
        public void RequestPrewarmOnNextFrame()
        {
            _prewarmStructureForNextFrame = true;
        }

        private void OnDisable()
        {
            if (!DynamicsManager.SkipReset)
            {
                ResetParticlesToHomeTransforms();
            }
        }

        private void OnEnable()
        {
            if (!DynamicsManager.SkipReset)
            {
                UpdateParticlesFromTransforms();
            }
        }

        private void UpdateParticlesFromTransforms()
        {
            if (Particles == null)
            {
                return;
            }

            foreach (DynamicsParticle particle in Particles)
            {
                if (particle != null)
                {
                    particle.UpdateParticleFromTransform();
                }
            }
        }

        private void OnDestroy()
        {
            DynamicsManager.Instance?.DynamicsStructureRemoved(this);
        }

#if UNITY_EDITOR
        private readonly StringBuilder _statisticsMessage = new(50, 1000);
        private void OnDrawGizmos()
        {
            if (_showHomeTransforms)
            {
                foreach (DynamicsParticle particle in Particles)
                {
                    if (!particle) continue;
                    DynamicsGizmos.DrawAxes(particle.HomePosition, particle.HomeRotation, particle.ScaledCollisionRadius);
                }
            }

            if (_showStatistics)
            {
                UnityEditor.Handles.BeginGUI();
                _statisticsMessage.Clear();

                _statisticsMessage.Append("FPS: ");
                _statisticsMessage.Append(1f / Time.deltaTime);
                _statisticsMessage.Append("\n");

                _statisticsMessage.Append("Dynamics Step Time:\n");
                var milliseconds = Mathf.RoundToInt(_ticks / 10000f);
                if (milliseconds > 0)
                {
                    _statisticsMessage.Append(milliseconds);
                    _statisticsMessage.Append(" ms");
                }
                else
                {
                    _statisticsMessage.Append(_ticks.ToString("0"));
                    _statisticsMessage.Append(" ticks");
                }

                var padding = 10;
                GUI.Label(new Rect(padding, 0, Screen.width - padding, Screen.height), _statisticsMessage.ToString());
                UnityEditor.Handles.EndGUI();
            }
        }
#endif
    }
}
