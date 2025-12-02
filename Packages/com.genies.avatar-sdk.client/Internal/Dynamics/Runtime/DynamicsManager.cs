using System.Collections.Generic;
using UnityEngine;

namespace Genies.Components.Dynamics
{
    /// <summary>
    /// Manager for all instances <see cref="DynamicsStructure"/>. This class contains all global settings for dynamics.
    /// It is optional to instantiate this manager in your scene. If not instantiated then default settings will be used.
    /// </summary>
    public class DynamicsManager : MonoBehaviour
    {
        public static DynamicsManager Instance { get; private set; }

        /// <summary>
        /// Will return true if the dynamics system is being used in a simple stand-alone mode. If dynamics is running
        /// within an application with feature management then the value returned should be provided by that system.
        /// </summary>
        public static bool DynamicsEnabled => !Instance || Instance.OnCheckDynamicsEnabled == null || Instance.OnCheckDynamicsEnabled.Invoke();

        public delegate bool IsDynamicsEnabledDelegate();
        public event IsDynamicsEnabledDelegate OnCheckDynamicsEnabled;

        public static bool SkipReset { get; set; }

        [Tooltip(DynamicsTooltips.MaxParticleVelocity)]
        [SerializeField] private float _maxParticleVelocity = _defaultMaxParticleVelocity;
        private const float _defaultMaxParticleVelocity = 67f;
        private const float _defaultMaxParticleVelocitySqr = _defaultMaxParticleVelocity * _defaultMaxParticleVelocity;

        [Tooltip(DynamicsTooltips.MaxParticleVelocity)]
        public static float MaxParticleVelocity => Instance ? Instance._maxParticleVelocity : _defaultMaxParticleVelocity;

        private static DynamicsStructure.UpdateMethod _updateMethod = DynamicsStructure.UpdateMethod.Fixed_Update;
        [Tooltip(DynamicsTooltips.UpdateMethod)]
        public static DynamicsStructure.UpdateMethod DefaultUpdateMethod
        {
            get => _updateMethod;
            set
            {
                _updateMethod = value;
                Instance?.SetExistingStructuresUpdateMethod(value);
            }
        }
        
        private static DynamicsStructure.ComputeMethod _computeMethod = DynamicsStructure.ComputeMethod.CPU_JOBS;
        [Tooltip(DynamicsTooltips.ComputeMethod)]
        public static DynamicsStructure.ComputeMethod DefaultComputeMethod
        {
            get => _computeMethod;
            set
            {
                _computeMethod = value;
                Instance?.SetExistingStructuresComputeMethod(value);
            }
        }

        [SerializeField] private bool _verboseLogging;
        [SerializeField] private bool _logPreWarmTime;

        private readonly HashSet<DynamicsStructure> _dynamicsStructures = new();

        public static bool VerboseLogging => Instance ? Instance._verboseLogging : false;
        public static bool LogPreWarmTime => Instance ? Instance._logPreWarmTime : false;

        public static float MaxParticleVelocitySqr => Instance ? Instance._maxParticleVelocity * Instance._maxParticleVelocity : _defaultMaxParticleVelocitySqr;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Debug.LogError("Duplicate Dynamics Manager Instantiated.");
                Destroy(this);
            }
        }

        public void EnableDynamics()
        {
            // Enable any current dynamics structures
            foreach (DynamicsStructure dynamicsStructure in _dynamicsStructures)
            {
                if (dynamicsStructure)
                {
                    dynamicsStructure.enabled = true;
                }
            }
        }

        public void DisableDynamics()
        {
            // Disable any current dynamics structures
            foreach (DynamicsStructure dynamicsStructure in _dynamicsStructures)
            {
                if (dynamicsStructure is not null)
                {
                    dynamicsStructure.enabled = false;
                }
            }
        }

        /// <summary>
        /// Called to add a dynamics structure to the manager's tracking.
        /// </summary>
        /// <param name="dynamicsStructure">The dynamics structure</param>
        public void DynamicsSctructureAdded(DynamicsStructure dynamicsStructure)
        {
            var added = _dynamicsStructures.Add(dynamicsStructure);

            if (!added && VerboseLogging)
            {
                Debug.LogError($"Failed to add dynamics structure to manager tracking: ({dynamicsStructure.gameObject.name})");
            }
        }

        /// <summary>
        /// Called to remove a dynamics structure from the manager's tracking.
        /// </summary>
        /// <param name="dynamicsStructure">The dynamics structure</param>
        public void DynamicsStructureRemoved(DynamicsStructure dynamicsStructure)
        {
            var removed = _dynamicsStructures.Remove(dynamicsStructure);

            if (!removed && VerboseLogging)
            {
                Debug.LogError($"Failed to remove dynamics structure from manager tracking: ({dynamicsStructure.gameObject.name})");
            }
        }

        /// <summary>
        /// Runs the background <see cref="DynamicsStructure.PreWarm"/> operation on all dynamics structures.
        /// </summary>
        public void PrewarmDynamicsStructures()
        {
            foreach (var dynamicsStructure in _dynamicsStructures)
            {
                if (dynamicsStructure is not null)
                {
                    dynamicsStructure.RequestPrewarmOnNextFrame();
                }
            }
        }

        /// <summary>
        /// Make all structures hold their current position until further notice.
        /// </summary>
        public void PauseDynamics()
        {
            foreach (var dynamicsStructure in _dynamicsStructures)
            {
                if (dynamicsStructure is not null)
                {
                    dynamicsStructure.Pause();
                }
            }
        }

        /// <summary>
        /// Play dynamics simulation. If dynamics was paused this will resume the simulation.
        /// </summary>
        public void PlayDynamics()
        {
            foreach (var dynamicsStructure in _dynamicsStructures)
            {
                if (dynamicsStructure is not null)
                {
                    dynamicsStructure.Resume();
                }
            }
        }

        /// <summary>
        /// Initializes dynamics and then holds all structure particles in place.
        /// This is useful when the structures should be simulated but held in a static position, such as static avatar pose sequences.
        /// </summary>
        public void InitAndPauseDynamics()
        {
            PrewarmDynamicsStructures();
            PauseDynamics();
        }

        /// <summary>
        /// Initializes dynamics and ensures that the simulation is enabled. This should be called at the beginning of any animation
        /// which requires moving dynamics structures.
        /// </summary>
        public void InitAndPlayDynamics()
        {
            PrewarmDynamicsStructures();
            PlayDynamics();
        }

        //sets the update method of all the known dynamics structures
        private void SetExistingStructuresUpdateMethod(DynamicsStructure.UpdateMethod method)
        {
            foreach (var dstruct in _dynamicsStructures)
            {
                dstruct.DynamicsUpdateMethod = method;
            }
        }
        
        //sets the compute method of all the known dynamics structures
        private void SetExistingStructuresComputeMethod(DynamicsStructure.ComputeMethod method)
        {
            foreach (var dstruct in _dynamicsStructures)
            {
                dstruct.CollisionComputeMethod = method;
            }
        }
    }
}
