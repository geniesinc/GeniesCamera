using System;

namespace Genies.Avatars
{
    /// <summary>
    /// Represents a component that can be added to a <see cref="IGenie"/> instance through its exposed <see cref="GenieComponentManager"/>.
    /// A genie component adds extra functionality to the avatar.
    /// <br/><br/>
    /// It's important that any implementation is prepared to Add/Remove the component across multiple avatar instances. This requirement
    /// is necessary for the correct function of the avatar cloning features.
    /// </summary>
    public abstract class GenieComponent
    {
        /// <summary>
        /// The name of this component instance.
        /// </summary>
        public abstract string Name { get; }
        
        /// <summary>
        /// The genie that this component is added to.
        /// </summary>
        public IGenie Genie { get; private set; }
        
        /// <summary>
        /// Whether this component is currently managed as an animation feature by an <see cref="AnimationFeatureManager"/>.
        /// </summary>
        public bool IsAnimationFeature { get; internal set; }
        
        public event Action Added;
        public event Action Removed;
        
        /// <summary>
        /// If enabled, this component won't be serialized by the <see cref="GenieComponentManager"/>.
        /// </summary>
        public bool ShouldSkipSerialization;

        internal bool TryAdd(IGenie genie, bool notify)
        {
            Genie = genie;

            try
            {
                if (TryInitialize())
                {
                    if (notify)
                    {
                        Added?.Invoke();
                    }

                    return true; 
                }
            }
            catch (Exception)
            {
                Genie = null;
                throw;
            }
            
            Genie = null;
            return false;
        }
        
        internal void Remove(bool notify)
        {
            try
            {
                OnRemoved();
            }
            finally
            {
                Genie = null;
                if (notify)
                {
                    Removed?.Invoke();
                }
            }
        }
        
        /// <summary>
        /// Creates a copy of this component with its current state that is ready to be added to a new genie.
        /// </summary>
        public abstract GenieComponent Copy();
        
        /// <summary>
        /// Called when being added to a genie, must return true if the initialization was correct.
        /// </summary>
        /// <returns>Whether the initialization succeeded. Return false to avoid the component from being added to the genie.</returns>
        protected abstract bool TryInitialize();
        
        /// <summary>
        /// Called when being removed from a genie, it should dispose all resources and leave the genie in its original state
        /// </summary>
        protected abstract void OnRemoved();
        
        /// <summary>
        /// Called by the <see cref="AnimationFeatureManager"/> every time it refreshes. Override this if you implemented
        /// a component for an animation feature, and it needs to rebuild something when animation parameters change.
        /// </summary>
        protected internal virtual void OnAnimationFeatureManagerRefreshed() { }
    }
}