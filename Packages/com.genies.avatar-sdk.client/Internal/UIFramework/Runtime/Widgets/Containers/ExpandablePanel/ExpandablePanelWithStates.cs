using System;
using System.Collections.Generic;
using UnityEngine;

namespace Genies.UI
{
    /// <summary>
    /// Extend this class to create components with the same functionality as <see cref="ExpandablePanel"/> that use an
    /// enum type to specify a fixed set of possible states.
    /// </summary>
    public abstract class ExpandablePanelWithStates<TState> : MonoBehaviour, IExpandablePanel<TState>
        where TState : struct, Enum
    {
        private static readonly TState[] PossibleStates;
        private static readonly Dictionary<TState, int> StateToIndex;

        static ExpandablePanelWithStates()
        {
            PossibleStates = Enum.GetValues(typeof(TState)) as TState[];
            if (PossibleStates is null)
            {
                Debug.LogError($"Couldn't determine the possible states for type {typeof(TState)}");
                return;
            }

            StateToIndex = new Dictionary<TState, int>();

            for (int i = 0; i < PossibleStates.Length; ++i)
            {
                StateToIndex[PossibleStates[i]] = i;
            }
        }

        public static int IndexOf(TState state)
            => StateToIndex.TryGetValue(state, out int index) ? index : -1;

        public static TState StateOf(int index)
            => index >= 0 && index < PossibleStates.Length ? PossibleStates[index] : default;

        protected ExpandablePanel Panel;

        protected abstract float GetInitialSizeForState(TState state);

        protected virtual void Awake()
        {
            Panel ??= GetComponent<ExpandablePanel>();
            if (!Panel)
            {
                Debug.LogError($"The {nameof(ExpandablePanelWithStates<TState>)} component needs a {nameof(ExpandablePanel)} component to work");
                return;
            }

            // remove any states that could be registered on the expandable panel
            for (int i = Panel.States.Count - 1; i >= 0; --i)
            {
                Panel.RemoveState(i);
            }

            // initialize the panel states
            float lastStateSize = float.NegativeInfinity;

            foreach (TState state in PossibleStates)
            {
                float stateSize = GetInitialSizeForState(state);

                if (stateSize <= lastStateSize)
                {
                    Debug.LogError($"[{nameof(ExpandablePanelWithStates<TState>)}] state sizes must be ascending in the same order of the states");
                    return;
                }

                Panel.AddState(stateSize);
                lastStateSize = stateSize;
            }

            // subscribe to panel events
            Panel.TransitionStarted += OnTransitionStarted;
            Panel.TransitionUpdated += OnTransitionUpdated;
            Panel.TransitionEnded += OnTransitionEnded;
        }

        protected virtual void OnDestroy()
        {
            // unsubscribe from panel events
            Panel.TransitionStarted -= OnTransitionStarted;
            Panel.TransitionUpdated -= OnTransitionUpdated;
            Panel.TransitionEnded -= OnTransitionEnded;
        }

#region PANEL_WRAPPER
        public TState State
            => StateOf(Panel.State);
        public TState MinState
            => StateOf(Panel.MinState);
        public TState MaxState
            => StateOf(Panel.MaxState);
        public bool IsLocked
            => Panel.IsLocked;

        public float Size
            => Panel.Size;
        public bool IsTransitioning
            => Panel.IsTransitioning;

        public float StateSize
            => Panel.StateSize;
        public float MinStateSize
            => Panel.MinStateSize;
        public float MaxStateSize
            => Panel.MaxStateSize;

        public event IExpandablePanel<TState>.TransitionStartedHandler TransitionStarted;
        public event IExpandablePanel<TState>.TransitionUpdatedHandler TransitionUpdated;
        public event IExpandablePanel<TState>.TransitionEndedHandler TransitionEnded;

        public void SetState(TState state, bool smoothTransition = true)
            => Panel.SetState(IndexOf(state), smoothTransition);
        public void SetStateWithoutTransition(int state)
            => Panel.SetStateWithoutTransition(state);
        public void SetStateSize(TState state, float size, bool transitionToState = false)
            => Panel.SetStateSize(IndexOf(state), size, transitionToState);
        public float GetStateSize(TState state)
            => Panel.GetStateSize(IndexOf(state));
        public void Lock(TState minState, TState maxState, bool smoothTransition = true)
            => Panel.Lock(IndexOf(minState), IndexOf(maxState), smoothTransition);
        public void Unlock()
            => Panel.Unlock();
        public void LockMin(TState minState, bool smoothTransition = true)
            => Panel.LockMin(IndexOf(minState), smoothTransition);
        public void LockMax(TState maxState, bool smoothTransition = true)
            => Panel.LockMax(IndexOf(maxState), smoothTransition);

        private void OnTransitionStarted(int fromIndex, int toIndex)
            => TransitionStarted?.Invoke(StateOf(fromIndex), StateOf(toIndex));
        private void OnTransitionUpdated(int fromIndex, int toIndex, float lerp)
            => TransitionUpdated?.Invoke(StateOf(fromIndex), StateOf(toIndex), lerp);
        private void OnTransitionEnded(int index)
            => TransitionEnded?.Invoke(StateOf(index));
#endregion
    }
}
