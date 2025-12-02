using System;
using System.Collections.Generic;
using UnityEngine;
using Genies.CrashReporting;

namespace Genies.Events {
    public interface IEvent {}
    public class Event : IEvent {
        public List<Action> Actions { get; private set; } = new List<Action>();

        public void AddListener(Action action) {
            if (!Actions.Contains(action)) {
                Actions.Add(action);
            } else {
                Debug.Log("EventBus: Action you're tryng to add already exists!");
            }
        }

        public void Fire() {
            foreach (var action in Actions) {
                try {
                    action.Invoke();
                }
                catch(Exception ex) {
                    CrashReporter.LogHandledException(ex);
                }
            }
        }

        public void RemoveListener(Action action) {
            if (Actions.Contains(action)) {
                Actions.Remove(action);
            } else {
                Debug.Log("EventBus: Action is not exists!");
            }
        }
    }
    public class Event<T> : IEvent {
        public List<Action<T>> Actions { get; private set; } = new List<Action<T>>();
        public void AddListener(Action<T> action) {
            if (!Actions.Contains(action)) {
                Actions.Add(action);
            } else {
                Debug.Log("EventBus: Action you're trying to add already exists!");
            }
        }

        public void Fire(T argument) {
            foreach (var action in Actions) {
                try {
                    action.Invoke(argument);
                }
                catch (Exception ex) {
                    CrashReporter.LogHandledException(ex);
                }
            }
        }

        public void RemoveListener(Action<T> action) {
            if (Actions.Contains(action)) {
                Actions.Remove(action);
            } else {
                Debug.Log("EventBus: Action is not exists!");
            }
        }

    }
    public class EventBus {

        public EventBus() {
            _events = new Dictionary<string, IEvent>();
        }

        private Dictionary<string, IEvent> _events;
        /// <summary>
        /// Subscribe to specified parameterless event
        /// </summary>
        /// <param name="eventName">Name of the event</param>
        /// <param name="action">Action to invoke</param>
        public void Subscribe(string eventName, Action action) {
            if (!_events.ContainsKey(eventName))
            {
                _events.Add(eventName, new Event());
            }

            var eventToSubscribe = _events[eventName] as Event;
            eventToSubscribe?.AddListener(action);
        }

        /// <summary>
        /// Subscribe to specified event which sends parameter of type T
        /// </summary>
        /// <typeparam name="T">Action parameter type</typeparam>
        /// <param name="eventName">Name of the event</param>
        /// <param name="action">Action to invoke</param>
        public void Subscribe<T>(string eventName, Action<T> action) {
            if (!_events.ContainsKey(eventName))
            {
                _events.Add(eventName, new Event<T>());
            }
            var eventToSubscribe = _events[eventName] as Event<T>;
            eventToSubscribe?.AddListener(action);
        }

        /// <summary>
        /// Unsubscribe from specified parameterless event
        /// </summary>
        /// <param name="eventName">Name of the event</param>
        /// <param name="action">Action to invoke</param>
        public void Unsubscribe(string eventName, Action action) {
            if (!_events.ContainsKey(eventName)) {

                if (Application.isEditor)
                {
                    Debug.LogWarning($"No Event found to Unsubscribe from: {eventName}");
                }

                return;
            }

            var eventToUnsubscribe = _events[eventName] as Event;
            eventToUnsubscribe?.RemoveListener(action);
        }

        /// <summary>
        /// Unsubscribe from specified event which sends parameter of type T
        /// </summary>
        /// <typeparam name="T">Action parameter type</typeparam>
        /// <param name="eventName">Name of the event</param>
        /// <param name="action">Action to invoke</param>
        public void Unsubscribe<T>(string eventName, Action<T> action) {
            if (!_events.ContainsKey(eventName)) {

                if (Application.isEditor)
                {
                    Debug.LogWarning($"No Event found to Unsubscribe from: {eventName}");
                }

                return;
            }

            var eventToUnsubscribe = _events[eventName] as Event<T>;
            eventToUnsubscribe?.RemoveListener(action);
        }

        /// <summary>
        /// Fires the parameterless event
        /// </summary>
        /// <param name="eventName">The name of the event</param>
        public void Fire(string eventName) {
            if (_events.ContainsKey(eventName)) {
                var eventToInvoke = _events[eventName] as Event;
                eventToInvoke?.Fire();
            } else if(Application.isEditor) {
                CrashReporter.LogInternal($"Firing event with no subscribers: {eventName}");
            }
        }

        /// <summary>
        /// Fires the specified event with parameter of type T
        /// </summary>
        /// <typeparam name="T">Parameter type</typeparam>
        /// <param name="eventName">The name of the event</param>
        /// <param name="argument">Parameter of the event</param>
        public void Fire<T>(string eventName, T argument) {
            if (_events.ContainsKey(eventName)) {
                var eventToInvoke = _events[eventName] as Event<T>;

                if (eventToInvoke != null) {
                    eventToInvoke.Fire(argument);
                } else if(Application.isEditor){
                    Debug.LogError($"Event {eventName} exists, but has wrong parameter type, please check subscription and invocation");
                }

            } else {
                CrashReporter.LogInternal($"Firing event with no subscribers: {eventName}");
            }
        }
    }
}
