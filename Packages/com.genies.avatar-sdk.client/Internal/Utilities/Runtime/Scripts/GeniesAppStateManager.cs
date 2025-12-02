using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Genies.CrashReporting;
using Genies.ServiceManagement;
using UnityEngine;
using VContainer;
using static Genies.Utilities.Validation;

namespace Genies.Utilities
{
    [Serializable]
    public class State
    {
        private object _value;
        public T GetValue<T>() {
            T castValue;
            try
            {
                castValue = (T)_value;
            } catch (Exception e)
            {
                CrashReporter.LogHandledException(e);
                return default;
            }
            return castValue;
        }

        public void SetValue<T>(T value) {
            _value = DeepClone(value);
        }

        private T DeepClone<T>(T obj) {
            using (var ms = new MemoryStream()) {
                var formatter = new BinaryFormatter();
                formatter.Serialize(ms, obj);
                ms.Position = 0;
                return (T)formatter.Deserialize(ms);
            }
        }
    }

    [AutoResolve]
    public class GeniesAppStateManagerInstaller : IGeniesInstaller
    {
        public int OperationOrder => DefaultInstallationGroups.CoreDependency;

        public void Install(IContainerBuilder builder)
        {
            builder.Register<GeniesAppStateManager>(Lifetime.Singleton);
        }
    }

    public class GeniesAppStateManager {

        protected string AppStateStoragePath => $"{Application.persistentDataPath}//AppState_V2.bin";
        protected Dictionary<string, State> ApplicationStates = new Dictionary<string, State>();
        protected Dictionary<string, State> PermanentStates = new Dictionary<string, State>();


#if QA_BUILD || UNITY_EDITOR
        public HashSet<string> ExposedApplicationStates => new HashSet<string>(ApplicationStates.Keys);
        public HashSet<string> ExposedPermanentStates => new HashSet<string>(PermanentStates.Keys);
#endif

        public GeniesAppStateManager()
        {
            LoadPermanentState();
        }

        private void SavePermanentState() {
            SaveStatesToBinaryFile(PermanentStates);
        }

        private void SaveStatesToBinaryFile(object obj) {
            try
            {

                if (File.Exists(AppStateStoragePath))
                {
                    File.Delete(AppStateStoragePath);
                }

                using var ms        = new MemoryStream();
                var       formatter = new BinaryFormatter();
                formatter.Serialize(ms, obj);
                ms.Position = 0;
                File.WriteAllBytes(AppStateStoragePath, ms.GetBuffer());
            }
            catch (Exception e)
            {
                CrashReporter.LogHandledException(e);
            }
        }

        private Dictionary<string, State> LoadStatesFromBinaryFile(string filepath)
        {
            #region Validations
            string errorMessage = $"Invalid filepath received when trying to load states from a binary file.";
            if (!IsValid(filepath, errorMessage))
            {
                return new Dictionary<string, State>();
            }

            //We didn't create the file yet.
            if (!File.Exists(filepath))
            {
                return new Dictionary<string, State>();
            }
            #endregion

            var ms = new MemoryStream();

            try
            {
                var formatter = new BinaryFormatter();
                byte[] data = File.ReadAllBytes(filepath);
                ms.Write(data, 0, data.Length);
                ms.Position = 0;

                Dictionary<string, State> dictionary = formatter.Deserialize(ms) as Dictionary<string, State>;
                errorMessage = $"Failed to Deserialize requested file when trying to load states from a binary file.";
                if (!IsValid(dictionary, errorMessage))
                {
                    return new Dictionary<string, State>();
                }

                return dictionary;
            }
            catch (Exception exc)
            {
                CrashReporter.LogHandledException(exc);
            }
            finally
            {
                ms.Dispose();
            }

            return new Dictionary<string, State>();
        }

        private void LoadPermanentState()
        {
            PermanentStates = LoadStatesFromBinaryFile(AppStateStoragePath);
        }

        /// <summary>
        /// Permanent - means storable on hdd
        /// </summary>
        /// <param name="StateId">Key</param>
        /// <returns></returns>
        public bool IsPermanent(string StateId) {
            return PermanentStates.ContainsKey(StateId);
        }


        public void SetState<T>(string StateId, T value) {
            if (PermanentStates.ContainsKey(StateId)) {
                Debug.LogError($"You already have permanent state with key {StateId} use MarkStateAsNormal");
                return;
            }
            var newState = new State();
            newState.SetValue(value);
            ApplicationStates[StateId] = newState;
        }

        /// <summary>
        /// Permanent states are stored on the disc in persistant data path
        /// and will be available after application restart!
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="StateId">Key</param>
        /// <param name="value">Value</param>
        public void SetPermanentState<T>(string StateId, T value) {
            if (ApplicationStates.ContainsKey(StateId)) {
                Debug.LogError($"You already have non permanent state with key {StateId}, use MarkStateAsPermanent");
                return;
            }
            var newState = new State();
            newState.SetValue(value);
            PermanentStates[StateId] = newState;
            SavePermanentState();
        }
        /// <summary>
        /// Saves state on the disc
        /// </summary>
        /// <param name="StateId">Key for storing</param>
        public void MarkStateAsPermanent(string StateId) {
            if (ApplicationStates.ContainsKey(StateId)) {
                PermanentStates[StateId] = ApplicationStates[StateId];
                RemoveState(StateId);
                SavePermanentState();
            }
            else {
                Debug.LogError($"You're trying to mark not existing state {StateId} as permanent");
            }
        }

        public void MarkStateAsNormal(string StateId) {
            if (PermanentStates.ContainsKey(StateId)) {
                ApplicationStates[StateId] = PermanentStates[StateId];
                RemovePermanentState(StateId);
            }
            else {
                Debug.LogError($"You're trying to mark not existing state {StateId} as normal");
            }
        }

        public void RemoveState(string StateId) {
            if (ApplicationStates.ContainsKey(StateId)) {
                ApplicationStates.Remove(StateId);
            }
        }

        public void RemoveAllStates()
        {
            ApplicationStates.Clear();
        }

        public void RemovePermanentState(string StateId) {
            if (PermanentStates.ContainsKey(StateId)) {
                PermanentStates.Remove(StateId);
                SavePermanentState();
            }
        }

        public void RemoveAllPermanentStates()
        {
            PermanentStates.Clear();
            SavePermanentState();
        }

        public virtual T GetState<T>(string StateId) {
            try
            {
                if (ApplicationStates.ContainsKey(StateId))
                {
                    return ApplicationStates[StateId].GetValue<T>();
                }

                if (PermanentStates.ContainsKey(StateId))
                {
                    return PermanentStates[StateId].GetValue<T>();
                }
            }
            catch (Exception exception)
            {
                CrashReporter.Log($"Failed to get state {StateId}: {exception}", LogSeverity.Exception);
            }

            return default;
        }

        public virtual bool HasState(string StateId) {
               return ApplicationStates.ContainsKey(StateId) || PermanentStates.ContainsKey(StateId);
        }
    }
}
