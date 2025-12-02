using UnityEngine;

namespace Genies.Components.Dynamics
{
    /// <summary>
    /// The fundamental unit of the Genies Dynamics system. A particle represents a tracked position and rotation
    /// in 3D space that will be simulated using Verlet Integration.
    /// </summary>
    [ExecuteAlways]
    public class DynamicsParticle : MonoBehaviour
    {
        [Range(0, 1)]
        [Tooltip(DynamicsTooltips.PositionAnchor)]
        public float PositionAnchor;

        [Range(0, 1)]
        [Tooltip(DynamicsTooltips.RotationAnchor)]
        public float RotationAnchor;
        
        [Tooltip(DynamicsTooltips.CollisionEnabled)]
        public bool CollisionEnabled = true;

        [Range(0, 1)]
        [Tooltip(DynamicsTooltips.ParticleCollisionRadius)]
        public float CollisionRadius = 0.05f;

        public float EffectiveScale => transform.lossyScale.x;

        public float ScaledCollisionRadius => CollisionRadius * EffectiveScale;

        [Tooltip(DynamicsTooltips.ParticleCollisionOffset)]
        public Vector3 CollisionOffset;

        [Tooltip(DynamicsTooltips.AffectsTransform)]
        public bool AffectsTransform = true;

        // Dynamics transforms of the particle is tracked outside of the Unity transform for accuracy and time step independence.
        [HideInInspector] public Vector3 CurrentPosition;
        [HideInInspector] public Vector3 CurrentCollisionCenter => CurrentPosition + Rotation * CollisionOffset;
        
        /// <summary>
        /// Gets the world space collision center for collision detection.
        /// </summary>
        public Vector3 WorldSpaceCollisionCenter
        {
            get
            {
                if (ModelSpaceReference != null)
                {
                    // Apply collision offset in model space first, then transform to world space
                    Vector3 modelSpaceCollisionCenter = CurrentPosition + Rotation * CollisionOffset;
                    return ModelSpaceReference.transform.TransformPoint(modelSpaceCollisionCenter);
                }
                else
                {
                    return CurrentCollisionCenter;
                }
            }
        }

        /// <summary>
        /// Gets the world space position of this particle.
        /// </summary>
        public Vector3 GetWorldSpacePosition()
        {
            if (ModelSpaceReference != null)
            {
                // Use world space transform of the model space reference
                return ModelSpaceReference.transform.TransformPoint(CurrentPosition);
            }
            else
            {
                return CurrentPosition;
            }
        }

        /// <summary>
        /// Gets the world space rotation of this particle.
        /// </summary>
        public Quaternion GetWorldSpaceRotation()
        {
            if (ModelSpaceReference != null)
            {
                // Use world space transform of the model space reference
                return ModelSpaceReference.transform.rotation * Rotation;
            }
            else
            {
                return Rotation;
            }
        }
        [HideInInspector] public Vector3 LastPosition;
        [HideInInspector] public Vector3 TargetPosition;
        [HideInInspector] public Quaternion Rotation;

        // Retain the original transforms of the particle for use in constraints that need the original shape of the structure.
        [HideInInspector] public Vector3 LocalHomePosition;
        [HideInInspector] public Quaternion LocalHomeRotation;

        [HideInInspector] public Vector3 LocalHomeDirection => LocalHomePosition.normalized;

        // If the particle's parent game object also has a particle component we will use it for more accurate inter-frame calculations.
        private DynamicsParticle _parentParticle;

        // The home position and rotation represent the world space transforms that the particle would have without dynamics applied
        [HideInInspector] public Vector3 HomePosition => GetHomePosition();
        [HideInInspector] public Vector3 LastHomePosition;
        [HideInInspector] public Quaternion HomeRotation => GetHomeRotation();

        // Model space reference GameObject - the root character GameObject that defines the model space
        [HideInInspector] public GameObject ModelSpaceReference;
        [HideInInspector] public Vector3 ModelSpaceHomePosition;
        [HideInInspector] public Quaternion ModelSpaceHomeRotation;
        
        /// <summary>
        /// The name of the bone to look for. The parent of this bone will be used as the model space reference. Defaults to "Root".
        /// </summary>
        private const string MODEL_SPACE_ROOT_BONE_NAME = "Root";

        // Model space home position and rotation for physics simulation
        [HideInInspector] public Vector3 ModelSpaceHomePositionProperty => GetModelSpaceHomePosition();
        [HideInInspector] public Quaternion ModelSpaceHomeRotationProperty => GetModelSpaceHomeRotation();

        private bool _initialized = false;

        /// <summary>
        /// Finds the model space reference GameObject. Uses manual reference if set, otherwise looks for the parent of the "Root" bone.
        /// </summary>
        private GameObject FindModelSpaceReference()
        {
            // Auto-detect: Look for the "Root" bone in the hierarchy
            Transform current = transform;
            Transform characterRoot = FindCharacterRoot(current);
            if (characterRoot != null)
            {
                return characterRoot.gameObject;
            }            
            // If no root found, use the root GameObject
            return transform.root.gameObject;
        }
        
        /// <summary>
        /// Attempts to find the character root by looking for the "Root" bone in the hierarchy.
        /// Returns the parent of the "Root" bone as the model space reference.
        /// </summary>
        private Transform FindCharacterRoot(Transform particleTransform)
        {
            Transform current = particleTransform.parent;
            
            while (current != null)
            {
                // Look for the specific "Root" bone
                if (current.name == MODEL_SPACE_ROOT_BONE_NAME)
                {
                    // Return the parent of the "Root" bone as the model space reference
                    // This is the base transform that doesn't move with character movement
                    return current.parent;
                }
                
                current = current.parent;
            }
            
            return null;
        }

        private void Start()
        {
            Initialize();
        }

        /// <summary>
        /// Stores the original transforms of the particle at the start of the simulation and sets initial values for particle motion.
        /// </summary>
        public void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            // Find the model space reference transform (root character transform)
            // This should be the topmost transform in the hierarchy that contains dynamics particles
            ModelSpaceReference = FindModelSpaceReference();

            CurrentPosition = transform.position;
            Rotation = transform.rotation;
            LastPosition = CurrentPosition;

            LocalHomePosition = transform.localPosition;
            LocalHomeRotation = transform.localRotation;

            // Calculate model space home position and rotation
            if (ModelSpaceReference != null)
            {
                // Use world space transform of the model space reference
                ModelSpaceHomePosition = ModelSpaceReference.transform.InverseTransformPoint(transform.position);
                ModelSpaceHomeRotation = Quaternion.Inverse(ModelSpaceReference.transform.rotation) * transform.rotation;
            }
            else
            {
                // Fallback to world space if no model space reference found
                ModelSpaceHomePosition = transform.position;
                ModelSpaceHomeRotation = transform.rotation;
            }

            if (transform.parent != null)
            {
                _parentParticle = transform.parent.GetComponent<DynamicsParticle>();
            }

            LastHomePosition = ModelSpaceHomePosition;

            _initialized = true;
        }

        /// <summary>
        /// Updates the particle's position and rotation values with the values from its underlying Unity game object.
        /// </summary>
        public void UpdateParticleFromTransform()
        {
            if (ModelSpaceReference != null)
            {
                // Use model space home position and rotation
                CurrentPosition = GetModelSpaceHomePosition();
                LastPosition = CurrentPosition;
                Rotation = GetModelSpaceHomeRotation();
            }
            else
            {
                // Fallback to world space
                CurrentPosition = HomePosition;
                LastPosition = CurrentPosition;
                Rotation = HomeRotation;
            }
        }

        /// <summary>
        /// Moves the particle's underlying game object back to its original position and rotation upon the start of the simulation.
        /// </summary>
        public void ResetToHomeTransform()
        {
            if (!_initialized)
            {
                Initialize();
            }

            if (AffectsTransform)
            {
                transform.localPosition = LocalHomePosition;
                transform.localRotation = LocalHomeRotation;
            }

            UpdateParticleFromTransform();
        }

        public void LerpToHomePositionAndRotation(float amount)
        {
            if (ModelSpaceReference != null)
            {
                // Use model space home position and rotation
                Vector3 modelSpaceHomePosition = GetModelSpaceHomePosition();
                Quaternion modelSpaceHomeRotation = GetModelSpaceHomeRotation();
                
                this.CurrentPosition = Vector3.Lerp(this.CurrentPosition, modelSpaceHomePosition, amount);
                this.LastPosition = Vector3.Lerp(this.LastPosition, modelSpaceHomePosition, amount);
                this.Rotation = Quaternion.Slerp(this.Rotation, modelSpaceHomeRotation, amount);
            }
            else
            {
                // Fallback to world space
                this.CurrentPosition = Vector3.Lerp(this.CurrentPosition, this.HomePosition, amount);
                this.LastPosition = Vector3.Lerp(this.LastPosition, this.HomePosition, amount);
                this.Rotation = Quaternion.Slerp(this.Rotation, this.HomeRotation, amount);
            }
        }

        /// <summary>
        /// Resets the particle completely. Moves the particle and underlying game object back to its original position and zeroes out velocity.
        /// </summary>
        public void Reset()
        {
            ResetToHomeTransform();
            CurrentPosition = transform.position;
            LastPosition = CurrentPosition;
        }

        /// <summary>
        /// Updates the position and rotation of the underlying unity game object with the particle's position and rotation.
        /// </summary>
        public void UpdateUnityTransforms()
        {
            if (AffectsTransform)
            {
                if (ModelSpaceReference != null)
                {
                    // Convert model space position and rotation to world space
                    Vector3 worldPosition = ModelSpaceReference.transform.TransformPoint(CurrentPosition);
                    Quaternion worldRotation = ModelSpaceReference.transform.rotation * Rotation;
                    transform.SetPositionAndRotation(worldPosition, worldRotation);
                }
                else
                {
                    // Fallback to direct assignment if no model space reference
                    transform.SetPositionAndRotation(CurrentPosition, Rotation);
                }
            }
        }

        private Vector3 GetHomePosition()
        {
            if (!AffectsTransform)
            {
                return transform.position;
            }
            else if (transform.parent == null)
            {
                return LocalHomePosition;
            }
            else if (_parentParticle)
            {
                return _parentParticle.CurrentPosition + _parentParticle.Rotation * LocalHomePosition * _parentParticle.EffectiveScale;
            }
            else
            {
                return transform.parent.TransformPoint(LocalHomePosition);
            }
        }

        /// <summary>
        /// Gets the model space home position for this particle.
        /// </summary>
        public Vector3 GetModelSpaceHomePosition()
        {
            if (ModelSpaceReference == null)
            {
                return GetHomePosition();
            }

            // Use world space transform of the model space reference
            if (!AffectsTransform)
            {
                return ModelSpaceReference.transform.InverseTransformPoint(transform.position);
            }
            else if (transform.parent == null)
            {
                return ModelSpaceReference.transform.InverseTransformPoint(LocalHomePosition);
            }
            else if (_parentParticle)
            {
                Vector3 worldHomePosition = _parentParticle.CurrentPosition + _parentParticle.Rotation * LocalHomePosition * _parentParticle.EffectiveScale;
                return ModelSpaceReference.transform.InverseTransformPoint(worldHomePosition);
            }
            else
            {
                Vector3 worldHomePosition = transform.parent.TransformPoint(LocalHomePosition);
                return ModelSpaceReference.transform.InverseTransformPoint(worldHomePosition);
            }
        }

        private Quaternion GetHomeRotation()
        {
            if (!AffectsTransform)
            {
                return transform.rotation;
            }
            else if (transform.parent == null)
            {
                return LocalHomeRotation;
            }
            else if (_parentParticle)
            {
                return _parentParticle.Rotation * LocalHomeRotation;
            }
            else
            {
                return transform.parent.rotation * LocalHomeRotation;
            }
        }

        /// <summary>
        /// Gets the model space home rotation for this particle.
        /// </summary>
        private Quaternion GetModelSpaceHomeRotation()
        {
            if (ModelSpaceReference == null)
            {
                return GetHomeRotation();
            }

            // Use world space transform of the model space reference
            if (!AffectsTransform)
            {
                return Quaternion.Inverse(ModelSpaceReference.transform.rotation) * transform.rotation;
            }
            else if (transform.parent == null)
            {
                return Quaternion.Inverse(ModelSpaceReference.transform.rotation) * LocalHomeRotation;
            }
            else if (_parentParticle)
            {
                Quaternion worldHomeRotation = _parentParticle.Rotation * LocalHomeRotation;
                return Quaternion.Inverse(ModelSpaceReference.transform.rotation) * worldHomeRotation;
            }
            else
            {
                Quaternion worldHomeRotation = transform.parent.rotation * LocalHomeRotation;
                return Quaternion.Inverse(ModelSpaceReference.transform.rotation) * worldHomeRotation;
            }
        }

#if UNITY_EDITOR
        private void Update()
        {
            // Necessary for gizmos to display properly in scene view
            if (!Application.isPlaying)
            {
                if (ModelSpaceReference != null)
                {
                    // Convert world space transform to model space for editor display
                    Transform modelSpaceTransform = ModelSpaceReference.transform;
                    CurrentPosition = modelSpaceTransform.InverseTransformPoint(transform.position);
                    Rotation = Quaternion.Inverse(modelSpaceTransform.rotation) * transform.rotation;
                }
                else
                {
                    // Fallback to direct assignment if no model space reference
                    CurrentPosition = transform.position;
                    Rotation = transform.rotation;
                }
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(PositionAnchor, PositionAnchor, 1f, 0.75f);
            
            // Use the public method to get world space collision center
            Vector3 worldCollisionCenter = GetWorldSpacePosition() + GetWorldSpaceRotation() * CollisionOffset;
            
            Gizmos.DrawSphere(worldCollisionCenter, ScaledCollisionRadius);
        }
#endif
    }
}
