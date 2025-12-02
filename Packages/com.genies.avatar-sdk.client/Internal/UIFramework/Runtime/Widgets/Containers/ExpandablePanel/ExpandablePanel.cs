using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace Genies.UI
{
    // size of this GameObject should not be controlled from other classes
    [RequireComponent(typeof(RectTransform))]
    public sealed class ExpandablePanel : MonoBehaviour, IExpandablePanel<int>
    {
        public enum Direction
        {
            Up, Down, Right, Left
        }

#region INSPECTOR
        public DragInputHandler dragHandler;
        public bool enableInput = true;
        public Direction direction = Direction.Up;
        public bool transitionOnAwake = true;
        public float minElasticity;
        public float maxElasticity;
        public float transitionSmoothTime = 0.15f;
        public float minSpeedForSwap = 1000.0f; // in pixels per second
        [SerializeField] private List<float> states = new List<float>();
#endregion

        public int State { get; private set; }
        public int MinState { get; private set; }
        public int MaxState { get; private set; }
        public bool IsLocked => MinState != 0 || MaxState != states.Count - 1;

        public float Size { get; private set; }
        public bool IsTransitioning { get; private set; }

        public IReadOnlyList<float> States => states;
        public float StateSize => states is null || states.Count == 0 ? 0.0f : states[Mathf.Clamp(State, MinState, MaxState)];
        public float MinStateSize => states is null || states.Count == 0 ? 0.0f : states[MinState];
        public float MaxStateSize => states is null || states.Count == 0 ? 0.0f : states[MaxState];

        public event IExpandablePanel<int>.TransitionStartedHandler TransitionStarted;
        public event IExpandablePanel<int>.TransitionUpdatedHandler TransitionUpdated;
        public event IExpandablePanel<int>.TransitionEndedHandler TransitionEnded;

        private RectTransform _rectTransform;
        private float _velocity;
        private int _targetState;
        private bool _initialized;

        private void Awake()
        {
            Initialize();

            if (transitionOnAwake)
            {
                CheckForTransition();
            }
        }

        public void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = false;
            _rectTransform = GetComponent<RectTransform>();
            if (!_rectTransform)
            {
                Debug.LogError($"The {nameof(ExpandablePanel)} component needs a {nameof(RectTransform)} component to work");
                return;
            }

            ValidateStates();

            // update current size/state and start transition if necessary
            Vector2 size = _rectTransform.rect.size;
            Size = direction switch
            {
                Direction.Up => size.y,
                Direction.Down => size.y,
                Direction.Right => size.x,
                Direction.Left => size.x,
                _ => size.y
            };

            MinState = 0;
            MaxState = states.Count - 1;
            State = GetStateForSize(Size);
            _initialized = true;
        }

        public void SetState(int state, bool smoothTransition = true)
        {
            //Make sure we initialize just in case.
            Initialize();

            if (state < MinState || state > MaxState)
            {
                return;
            }

            if (smoothTransition)
            {
                _targetState = state;

                StartTransition(_targetState);
            }
            else
            {
                State = state;
                Size = states[state];
                SetSize(Size);
                StopTransition(State);
            }
        }

        /// <summary>
        /// If an external system can also set the size of this panel (ie the chat window which is draggable, but also
        /// auto-snaps to specific sizes based on preprompt visibility/input focus) we want to be able to set the
        /// current state values WITHOUT any accompanying transitions, allowing the external system to do that
        /// without interference
        /// </summary>
        public void SetStateWithoutTransition(int state)
        {
            State = state;
            if (state < states.Count)
            {
                Size = states[state];
            }
        }

        public void AddState(float size, int? insertAtState = null, bool doTransition = true)
        {
            if (insertAtState.HasValue)
            {
                states.Insert(insertAtState.Value, size);
            }
            else
            {
                states.Add(size);
            }

            ValidateStates();

            if (doTransition)
            {
                CheckForTransition();
            }
        }

        public void RemoveState(int state, bool doTransition = true)
        {
            if (state < 0 || state >= states.Count)
            {
                return;
            }

            states.RemoveAt(state);

            if (doTransition)
            {
                CheckForTransition();
            }
        }

        public void RemoveStateBySize(float size)
        {
            RemoveState(states.IndexOf(size));
            CheckForTransition();
        }

        public void SetStateSize(int state, float size, bool transitionToState = false)
        {
            if (state < 0 || state >= states.Count)
            {
                return;
            }

            states[state] = size;
            ValidateStates();

            if (transitionToState)
            {
                StopTransition(State);
                SetState(state);
            }
        }

        public float GetStateSize(int state)
        {
            return state >= 0 && state < states.Count ? states[state] : 0.0f;
        }

        public void Lock(int minState, int maxState, bool smoothTransition = true)
        {
            if (maxState >= 0 && maxState < states.Count)
            {
                MaxState = maxState;
            }

            if (minState >= 0 && minState <= MaxState)
            {
                MinState = minState;
            }

            if (State < MinState)
            {
                SetState(MinState, smoothTransition);
            }
            else if (State > MaxState)
            {
                SetState(MaxState, smoothTransition);
            }
        }

        public void Unlock()
        {
            MinState = 0;
            MaxState = states.Count - 1;
        }

        public void LockMin(int minState, bool smoothTransition = true)
            => Lock(minState, MaxState, smoothTransition);

        public void LockMax(int maxState, bool smoothTransition = true)
            => Lock(MinState, maxState, smoothTransition);

        private void ValidateStates()
        {
            // remove duplicates and sort ascending by size
            var statesSet = new HashSet<float>(states);
            states = statesSet.ToList();
            states.Sort((a, b) => a.CompareTo(b));
        }

        private void CheckForTransition()
        {
            if (Size == StateSize)
            {
                return;
            }

            _targetState = GetStateForSize(Size);
            State = _targetState;

            if (_targetState >= MinState && _targetState < MaxState)
            {
                float distanceToTarget = Size - states[_targetState];
                float distanceToNext = states[_targetState + 1] - Size;

                if (distanceToTarget >= distanceToNext)
                {
                    ++_targetState;
                }
            }
            else if (_targetState < MinState)
            {
                _targetState = MinState;
            }

            StartTransition(State);
        }

        private void SetSize(float size)
        {
            var axis = direction switch
            {
                Direction.Up => RectTransform.Axis.Vertical,
                Direction.Down => RectTransform.Axis.Vertical,
                Direction.Right => RectTransform.Axis.Horizontal,
                Direction.Left => RectTransform.Axis.Horizontal,
                _ => RectTransform.Axis.Vertical
            };

            _rectTransform.SetSizeWithCurrentAnchors(axis, size);
        }

        private int GetStateForSize(float size)
        {
            if (states.Count == 0)
            {
                return 0;
            }

            int state = MinState;

            while (state <= MaxState && states[state] <= size)
            {
                ++state;
            }

            return --state;
        }

        private void Update()
        {
            if (dragHandler is null || states is null || states.Count == 0)
            {
                return;
            }

            if (enableInput && dragHandler.PointerDown)
            {
                OnPointerDown();
            }
            else if (enableInput && dragHandler.PointerReleased)
            {
                OnPointerReleased();
            }
            else if (IsTransitioning)
            {
                OnTransitioning();
            }

            if (IsTransitioning)
            {
                UpdateState();
            }
        }

        private void OnPointerDown()
        {
            _velocity = direction switch
            {
                Direction.Up => dragHandler.DragDelta.y,
                Direction.Down => -dragHandler.DragDelta.y,
                Direction.Right => dragHandler.DragDelta.x,
                Direction.Left => -dragHandler.DragDelta.x,
                _ => dragHandler.DragDelta.y

            };

            float maxSize = MaxStateSize;
            float minSize = MinStateSize;

            // apply elasticity if past the min/max sizes
            if (minElasticity != 0.0f && _velocity < 0 && Size < minSize)
            {
                _velocity *= 1.0f - Mathf.Clamp01((minSize - Size) / minElasticity);
            }

            if (maxElasticity != 0.0f && _velocity > 0 && Size > maxSize)
            {
                _velocity *= 1.0f - Mathf.Clamp01((Size - MaxStateSize) / maxElasticity);
            }

            Size += _velocity;

            // clamp Size value to min/max (including elasticity)
            minSize -= minElasticity;
            maxSize += maxElasticity;

            if (Size < minSize)
            {
                Size = minSize;
            }

            if (Size > maxSize)
            {
                Size = maxSize;
            }

            _velocity /= Time.deltaTime;
            SetSize(Size);

            if (!IsTransitioning && StateSize != Size)
            {
                State = GetStateForSize(Size);
                StartTransition(State);
            }
        }

        private void OnPointerReleased()
        {
            if (StateSize == Size)
            {
                IsTransitioning = false;
                return;
            }

            // start a transition to the closest state or go to the farthest if user swapped
            IsTransitioning = true;
            _targetState = GetStateForSize(Size);

            // clamp to min/max states
            if (_targetState < MinState)
            {
                _targetState = MinState;
            }
            else if (_targetState >= MaxState)
            {
                _targetState = MaxState;
            }
            // if swapping, go to the next state in the swap direction
            else if (_velocity <= -minSpeedForSwap || minSpeedForSwap <= _velocity)
            {
                _targetState = _velocity < 0 ? _targetState : _targetState + 1;
                _targetState = Mathf.Clamp(_targetState, 0, states.Count - 1);
            }
            // if not swapping, just round to nearest state
            else
            {
                float distanceToTarget = Size - states[_targetState];
                float distanceToNext = states[_targetState + 1] - Size;

                if (distanceToTarget >= distanceToNext)
                {
                    ++_targetState;
                }
            }

            if (!IsTransitioning ||  _targetState != State)
            {
                StartTransition(_targetState);
            }
        }

        private void OnTransitioning()
        {
            float targetSize = states[_targetState];
            Size = Mathf.SmoothDamp(Size, targetSize, ref _velocity, transitionSmoothTime);

            if (Mathf.Abs(targetSize - Size) < 1.0f)
            {
                Size = targetSize;
                SetSize(Size);
                _velocity = 0.0f;
                State = _targetState;
                StopTransition(_targetState);
            }
            else
            {
                SetSize(Size);
            }
        }

        private void StartTransition(int index)
        {
            IsTransitioning = true;
            TransitionStarted?.Invoke(index, index + 1);
        }

        private void StopTransition(int index)
        {
            IsTransitioning = _targetState != index;
            TransitionEnded?.Invoke(index);
        }

        private void UpdateState()
        {
            // get the current state based on current size
            int currentState = GetStateForSize(Size);

            // special case when touching the min size with no min elasticity
            if (minElasticity == 0.0f && Size == MinStateSize)
            {
                StopTransition(MinState);
                return;
            }

            // check if we are ending/stepping the current transition
            if (currentState != State)
            {
                int previousState = State;
                State = currentState;
                StopTransition(currentState < previousState ? previousState : currentState);

                // make sure to not start another transition if touching max size without max elasticity
                if (maxElasticity != 0.0f || currentState < MaxState)
                {
                    StartTransition(currentState);
                }
            }

            if (!IsTransitioning)
            {
                return;
            }

            // check if we are making the elastic transitions past the min/max sizes, in that case pass a lerp relative to min/max elasticity values
            if (State < MinState)
            {
                TransitionUpdated?.Invoke(MinState - 1, MinState, Mathf.Clamp01((states[MinState] - Size) / minElasticity));
            }
            else if (State >= MaxState)
            {
                TransitionUpdated?.Invoke(MaxState, MaxState + 1, Mathf.Clamp01((Size - states[MaxState]) / maxElasticity));
            }
            // normal transition update
            else
            {
                float lerp = Mathf.InverseLerp(states[State], states[State + 1], Size);
                TransitionUpdated?.Invoke(State, State + 1, lerp);
            }
        }

        #if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying)
                CheckForTransition();
        }
        #endif
    }
}
