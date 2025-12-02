using System;
using System.Collections.Generic;
using UMA;

namespace Genies.Avatars
{
    internal sealed class UmaGenieDnaManager : IDisposable
    {
        // dependencies
        private readonly UmaGenie _genie;
        
        // state
        private IDictionary<string, DnaSetter> _dnaSetters;

        public UmaGenieDnaManager(UmaGenie genie)
        {
            _genie = genie;
            
            _genie.Avatar.CharacterDnaUpdated.AddListener(OnCharacterDnaUpdated);
        }
        
        public bool SetDna(string name, float value)
        {
            UpdateDnaSetters();

            if (name is null || !_dnaSetters.TryGetValue(name, out DnaSetter setter))
            {
                return false;
            }

            // dna exists but it is already set the the given value
            if (setter.Value == value)
            {
                return true;
            }

            setter.Set(value);
            _genie.SetUmaAvatarDirty();
            return true;
        }

        public float GetDna(string name)
        {
            UpdateDnaSetters();
            return name != null && _dnaSetters.TryGetValue(name, out DnaSetter setter) ? setter.Value : 0.0f;
        }

        public bool ContainsDna(string name)
        {
            UpdateDnaSetters();
            return name != null && _dnaSetters.ContainsKey(name);
        }
        
        public void Dispose()
        {
            _dnaSetters = null;
            _genie.Avatar.CharacterDnaUpdated.RemoveListener(OnCharacterDnaUpdated);
        }
        
        private void UpdateDnaSetters()
        {
            _dnaSetters ??= _genie.Avatar.GetDNA();
        }

        private void OnCharacterDnaUpdated(UMAData umaData)
        {
            _dnaSetters = null;
        }
    }
}