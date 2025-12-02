using System;
using System.Collections.Generic;
using System.Linq;
using Genies.ServiceManagement;
using Genies.FeatureFlags;
using Genies.Utilities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Genies.Avatars
{
    /// <summary>
    /// Genie component that provides animation IK Retargeting support.
    /// </summary>
    public sealed class IkRetargeting : GenieComponent
    {
        public override string Name => _config.name;
        public IkRetargetingBehaviour Behaviour { get; private set; }
        private IFeatureFlagsManager _FeatureFlagsManager => ServiceManager.Get<IFeatureFlagsManager>();

        private readonly IkrConfig _config;
        private readonly bool _setIkHints;
        private readonly bool _autoRebuild;
        private readonly GenieGroundIkrTarget.Config[] _groundTargetConfigs;

        public IkRetargeting(IkrConfig config, bool setIkHints = false, bool autoRebuild = true, IEnumerable<GenieGroundIkrTarget.Config> groundTargetConfigs = null)
        {
            _config = config;
            _setIkHints = setIkHints;
            _groundTargetConfigs = groundTargetConfigs?.ToArray() ?? Array.Empty<GenieGroundIkrTarget.Config>();
        }

        public override GenieComponent Copy()
        {
            return new IkRetargeting(_config, _setIkHints, _autoRebuild, _groundTargetConfigs);
        }

        protected override bool TryInitialize()
        {
            // skip adding the component if there is a feature manager and the ik retargeting feature flag is not enabled
            if (_FeatureFlagsManager is not null )
            {
                Debug.LogWarning($"The FeatureFlagsManager is not available. The component will not be added");
                return false;
            }

            // disallow multiple IKRetargeting components on the same genie
            if (Genie.Components.TryGet<IkRetargeting>(out _))
            {
                Debug.LogError($"This genie already has a {nameof(IkRetargeting)} component");
                return false;
            }

            if (!_config)
            {
                Debug.LogError($"No config was given");
                return false;
            }

            Behaviour = Genie.Animator.gameObject.AddComponent<IkRetargetingBehaviour>();
            Behaviour.setIkHints = _setIkHints;
            Behaviour.autoRebuild = _autoRebuild;
            Behaviour.PostprocessGoals += PostprocessGoals;
            Genie.RootRebuilt += OnGenieRootRebuilt;

            // set behaviour config (this will trigger a rebuild)
            Behaviour.Config = _config;

            return true;
        }

        protected override void OnRemoved()
        {
            Genie.RootRebuilt -= OnGenieRootRebuilt;

            if (Behaviour)
            {
                Behaviour.PostprocessGoals -= PostprocessGoals;
                Object.Destroy(Behaviour);
            }
        }

        protected internal override void OnAnimationFeatureManagerRefreshed()
        {
            // force a rebuild since the animator parameters changed
            Behaviour.Rebuild();
        }

        private void OnGenieRootRebuilt()
        {
            // force a rebuild since the genie skeleton could have been rebuilt
            Behaviour.Rebuild();
        }

        private void PostprocessGoals(List<IkrGoal> goals, AnimatorParameters parameters)
        {
            // add our ground targets to the goals (or create new goals if they don't exist)
            foreach (GenieGroundIkrTarget.Config config in _groundTargetConfigs)
            {
                // skip targets with non-existing weight parameters
                if (!parameters.Contains(config.weightProperty))
                {
                    continue;
                }

                IkrGoal goal = GetOrCreateGoal(goals, config.goal);
                var target = GenieGroundIkrTarget.CreateFromConfig(config, Genie);
                goal.Targets.Add(target);
            }
        }

        private IkrGoal GetOrCreateGoal(List<IkrGoal> goals, AvatarIKGoal ikGoal)
        {
            foreach (IkrGoal goal in goals)
            {
                if (goal.Goal == ikGoal)
                {
                    return goal;
                }
            }

            var newGoal = new IkrGoal(ikGoal, Genie.Animator);
            goals.Add(newGoal);

            return newGoal;
        }
    }
}
