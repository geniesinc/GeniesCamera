namespace Genies.UI
{
    public interface IExpandablePanel<TState>
    {
        delegate void TransitionStartedHandler(TState fromState, TState toState);
        delegate void TransitionUpdatedHandler(TState fromState, TState toState, float lerp);
        delegate void TransitionEndedHandler(TState state);

        TState State { get; }
        TState MinState { get; }
        TState MaxState { get; }
        bool IsLocked { get; }

        float Size { get; }
        bool IsTransitioning { get; }

        float StateSize { get; }
        float MinStateSize { get; }
        float MaxStateSize { get; }

        event TransitionStartedHandler TransitionStarted;
        event TransitionUpdatedHandler TransitionUpdated;
        event TransitionEndedHandler TransitionEnded;

        void SetState(TState state, bool smoothTransition = true);
        void SetStateWithoutTransition(int state);
        void SetStateSize(TState state, float size, bool transitionToState = false);
        float GetStateSize(TState state);
        void Lock(TState minState, TState maxState, bool smoothTransition = true);
        void Unlock();
        void LockMin(TState minState, bool smoothTransition = true);
        void LockMax(TState maxState, bool smoothTransition = true);
    }
}
