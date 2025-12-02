using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Genies.Utilities;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Genies.Avatars
{
    [SerializableAs(typeof(GenieComponent), "animation-feature-manager")]
    public sealed class AnimationFeatureManager : GenieComponent
    {
        public override string                        Name               => "Animation Feature Manager";
        public IReadOnlyCollection<IAnimationFeature> Features           => _features;
        public IReadOnlyCollection<IAnimationFeature> ActiveFeatures     => _components.Keys;
        public AnimatorParameters                     AnimatorParameters { get; private set; }
        
        /// <summary>
        /// It is recommended to change the controller through this property instead of accessing the genie animator
        /// directly. This way the features are refreshed instantly (only when needed).
        /// </summary>
        public RuntimeAnimatorController RuntimeAnimatorController
        {
            get => Genie.Animator.runtimeAnimatorController;
            set => SetRuntimeAnimatorController(value);
        }
        
        /// <summary>
        /// Invoked after features have been refreshed (some features may have changed, some may have been activated
        /// and some deactivated).
        /// </summary>
        public event Action FeaturesRefreshed = delegate { };
        
        public bool AutoRefreshFeatures = true;
        
        private readonly TrackedHashSet<IAnimationFeature>             _features;
        private readonly Dictionary<IAnimationFeature, GenieComponent> _components;
        
        private CancellationTokenSource   _autoRefreshCancellation;
        private Animator                  _refreshedAnimator;
        private RuntimeAnimatorController _refreshedController;
        private int                       _refreshedParametersHash;

        public AnimationFeatureManager(IEnumerable<IAnimationFeature> features = null)
        {
            _features = new TrackedHashSet<IAnimationFeature>();
            _components = new Dictionary<IAnimationFeature, GenieComponent>();
            _features.BeginTracking();
            
            if (features is null)
            {
                return;
            }

            // just add the features. Do not refresh yet since we were not added to any genie
            foreach (IAnimationFeature feature in features)
            {
                if (feature is not null)
                {
                    _features.Add(feature);
                }
            }
        }
        
        public override GenieComponent Copy()
        {
            return new AnimationFeatureManager(_features)
            {
                AutoRefreshFeatures = AutoRefreshFeatures
            };
        }

        protected override bool TryInitialize()
        {
            // if another feature manager already exist then just add our features to it instead of adding a new one
            if (Genie.Components.TryGet(out AnimationFeatureManager manager))
            {
                manager.AddFeatures(_features);
                Debug.Log($"Only one {nameof(AnimationFeatureManager)} component is allowed. This manager component won't be added but its features were added to the existing one");
                return false;
            }

            if (AutoRefreshFeatures)
            {
                RefreshFeatures();
            }

            _autoRefreshCancellation = new CancellationTokenSource();
            AutoRefreshLoop(_autoRefreshCancellation.Token).Forget();
            
            return true;
        }

        /// <summary>
        /// If the given feature is active it will return its current component.
        /// </summary>
        public bool TryGetFeatureComponent(IAnimationFeature feature, out GenieComponent component)
        {
            return _components.TryGetValue(feature, out component);
        }
        
        public void AddFeature(IAnimationFeature feature)
        {
            if (feature is not null && _features.Add(feature))
            {
                RefreshFeatures();
            }
        }

        public void RemoveFeature(IAnimationFeature feature)
        {
            if (feature is not null && _features.Remove(feature))
            {
                RefreshFeatures();
            }
        }
        
        public void AddFeatures(IEnumerable<IAnimationFeature> features)
        {
            int previousCount = _features.Count;
            foreach (IAnimationFeature feature in features)
            {
                if (feature is not null)
                {
                    _features.Add(feature);
                }
            }
            
            if (previousCount != _features.Count)
            {
                RefreshFeatures();
            }
        }
        
        public void RemoveFeatures(IEnumerable<IAnimationFeature> features)
        {
            int previousCount = _features.Count;
            foreach (IAnimationFeature feature in features)
            {
                if (feature is not null)
                {
                    _features.Remove(feature);
                }
            }
            
            if (previousCount != _features.Count)
            {
                RefreshFeatures();
            }
        }

        public void SetFeatures(IEnumerable<IAnimationFeature> features)
        {
            _features.Clear();
            AddFeatures(features);
        }

        public void ClearFeatures()
        {
            if (_features.Count == 0)
            {
                return;
            }

            _features.Clear();
            RefreshFeatures();
        }

        public bool IsFeatureActive(IAnimationFeature feature)
        {
            return feature is not null && _components.ContainsKey(feature);
        }
        
        /// <summary>
        /// Forces a refresh of all animation features. A refresh activates all supported features and deactivates the
        /// rest. This refresh is usually done automatically when animator parameters are changed, but you can call it
        /// if you run into edge cases where you know that features changed or the animator controller changed but
        /// features were not refreshed yet, and you need access to them.
        /// </summary>
        public void RefreshFeatures()
        {
            if (_refreshedAnimator != Genie.Animator)
            {
                AnimatorParameters = new AnimatorParameters(Genie.Animator, refresh: false);
            }

            _refreshedAnimator = Genie.Animator;
            _refreshedController = _refreshedAnimator.runtimeAnimatorController;
            AnimatorParameters.Refresh();
            _refreshedParametersHash = AnimatorParameters.HashParameters();
            
            RefreshFeaturesWithoutStateRefresh();
        }

        /// <summary>
        /// Same as <see cref="RefreshFeatures"/> but it will only refresh if the animator parameters changed. This is
        /// called automatically on every frame if <see cref="AutoRefreshFeatures"/> is enabled.
        /// </summary>
        public void RefreshFeaturesIfDirty()
        {
            // always refresh if the animator changed
            if (_refreshedAnimator != Genie.Animator)
            {
                RefreshFeatures();
                return;
            }
            
            // if controller is the same don't refresh
            if (_refreshedController == _refreshedAnimator.runtimeAnimatorController)
            {
                return;
            }

            _refreshedController = _refreshedAnimator.runtimeAnimatorController;
            
            // if controller changed but parameters are the same don't refresh
            AnimatorParameters.Refresh();
            int parametersHash = AnimatorParameters.HashParameters();
            if (parametersHash == _refreshedParametersHash)
            {
                return;
            }

            _refreshedParametersHash = parametersHash;
            
            RefreshFeaturesWithoutStateRefresh();
        }
        
        public void RefreshFeaturesWithoutStateRefresh()
        {
            _features.FinishTracking();
            
            // pass over removed features and make sure they are deactivated
            foreach (IAnimationFeature feature in _features.RemovedItems)
            {
                if (_components.TryGetValue(feature, out GenieComponent component))
                {
                    Genie.Components.Remove(component);
                }
            }

            // pass over added features and activate the ones that are supported
            foreach (IAnimationFeature feature in _features.AddedItems)
            {
                if (!feature.SupportsParameters(AnimatorParameters))
                {
                    continue;
                }

                ActivateFeature(feature);
            }

            // pass over kept features and activate/deactivate them if their support state changed
            foreach (IAnimationFeature feature in _features.UnchangedItems)
            {
                // check if the feature is active (in that case we will get its currently added component)
                bool isActive = _components.TryGetValue(feature, out GenieComponent component);
                
                // the active state is the same as its support state, so there is no need to activate/deactivate it
                if (feature.SupportsParameters(AnimatorParameters) == isActive)
                {
                    continue;
                }

                // the feature is active but no longer supported, deactivate it
                if (isActive)
                {
                    Genie.Components.Remove(component);
                    _components.Remove(feature);
                    continue;
                }

                // the feature is not active but it is now supported, activate it
                ActivateFeature(feature);
            }
            
            // pass over active kept features to notify that the manager was refreshed
            foreach (IAnimationFeature feature in _features.UnchangedItems)
            {
                if (_components.TryGetValue(feature, out GenieComponent component))
                {
                    component.OnAnimationFeatureManagerRefreshed();
                }
            }

            // begin tracking so next time we refresh we can see features diff
            _features.BeginTracking();
            
            FeaturesRefreshed.Invoke();
        }
        
        private async UniTaskVoid AutoRefreshLoop(CancellationToken cancellationToken)
        {
            while (true)
            {
                await UniTask.Yield(PlayerLoopTiming.Update);
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                if (AutoRefreshFeatures)
                {
                    RefreshFeaturesIfDirty();
                }
            }
        }

        private void ActivateFeature(IAnimationFeature feature)
        {
            GenieComponent component;

            try
            {
                component = feature.CreateFeatureComponent(AnimatorParameters);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[{nameof(AnimationFeatureManager)}] exception thrown while creating feature component for feature: {feature}\n{exception}");
                return;
            }

            if (component is null)
            {
                Debug.LogError($"[{nameof(AnimationFeatureManager)}] animation feature returned null genie component: {feature}");
                return;
            }
            
            component.IsAnimationFeature = true;
            // disable serialization for the component since we want this manager to be serialized with all the features
            component.ShouldSkipSerialization = true;
            
            component.Added += () =>
            {
                if (component.Genie == Genie)
                {
                    _components.Add(feature, component);
                }
            };
            
            component.Removed += () =>
            {
                if (_components.TryGetValue(feature, out GenieComponent currentComponent) && component == currentComponent)
                {
                    _components.Remove(feature);
                }
            };
            
            Genie.Components.Add(component);
        }

        protected override void OnRemoved()
        {
            // stop auto refresh loop
            _autoRefreshCancellation.Cancel();
            _autoRefreshCancellation = null;

            // remove all feature components from the genie but keep the features as they are part of the components config
            var toRemove = new List<GenieComponent>(_components.Values); // this is important, we cannot iterate directly over _component.Values
            foreach (GenieComponent component in toRemove)
            {
                Genie.Components.Remove(component);
            }

            _components.Clear();
        }
        
        private void SetRuntimeAnimatorController(RuntimeAnimatorController controller)
        {
            if (controller == Genie.Animator.runtimeAnimatorController)
            {
                return;
            }

            Genie.Animator.runtimeAnimatorController = controller;
            RefreshFeaturesIfDirty();
        }
        
#region Serialization
        public JToken Serialize()
        {
            var featureTokens = new JArray();
            foreach (IAnimationFeature feature in _features)
            {
                if (feature.TrySerialize(out JToken featureToken))
                {
                    featureTokens.Add(featureToken);
                }
            }
            
            var token = new JObject()
            {
                { "features", featureTokens }
            };
            
            return token;
        }

        public static GenieComponent Deserialize(JToken token)
        {
            if (token is not JObject obj)
            {
                return null;
            }

            // try get the features array from the token
            if (!obj.TryGetValue("features", out JToken featureTokens) || featureTokens is not JArray featureArray)
            {
                return new AnimationFeatureManager();
            }

            // try deserialize all features
            var features = new List<IAnimationFeature>(featureArray.Count);
            foreach (JToken featureToken in featureArray)
            {
                if (SerializerAs<IAnimationFeature>.TryDeserialize(featureToken, out IAnimationFeature feature))
                {
                    features.Add(feature);
                }
            }
            
            return new AnimationFeatureManager(features);
        }
#endregion
    }
}