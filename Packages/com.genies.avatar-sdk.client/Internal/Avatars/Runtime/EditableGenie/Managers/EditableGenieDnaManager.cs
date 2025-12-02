using System;
using System.Collections.Generic;
using UMA;
using UnityEngine;

namespace Genies.Avatars
{
    internal sealed class EditableGenieDnaManager
    {
        public bool IsDirty { get; private set; }

        // dependencies
        private readonly EditableGenie _genie;

        // state
        private Dictionary<string, float> _dna;
        private BlendshapeDNAConverterPlugin _converter;
        private SkinnedMeshRenderer _renderer;
        private bool _converterIsRuntimeInstance;

        public EditableGenieDnaManager(EditableGenie genie, DynamicDNAConverterController controller, SkinnedMeshRenderer renderer)
        {
            _genie = genie;
            _renderer = renderer;
            _converter = controller.GetPlugin(0) as BlendshapeDNAConverterPlugin;
            _converterIsRuntimeInstance = false;
            _dna = new Dictionary<string, float>();

            foreach (var name in controller.dnaAsset.Names)
            {
                SetDna(name, 0.0f);
            }
        }
        
        public EditableGenieDnaManager(EditableGenie genie, string[] blendshapeNames, SkinnedMeshRenderer renderer)
        {
            _genie = genie;
            _renderer = renderer;
            _dna = new Dictionary<string, float>();
            
            // instantiate new converter from blendshape names
            _converter = ScriptableObject.CreateInstance<BlendshapeDNAConverterPlugin>();
            List<BlendshapeDNAConverterPlugin.BlendshapeDNAConverter> blendshapeConverters = 
                new List<BlendshapeDNAConverterPlugin.BlendshapeDNAConverter>();
            foreach (string name in blendshapeNames)
            {
                blendshapeConverters.Add(new BlendshapeDNAConverterPlugin.BlendshapeDNAConverter(name));
            }

            _converter.blendshapeDNAConverters = blendshapeConverters;
            _converterIsRuntimeInstance = true;

            foreach (var name in blendshapeNames)
            {
                SetDna(name, 0.0f);
            }
        }


        public bool SetDna(string name, float value)
        {
            if (name is null)
            {
                return false;
            }

            _dna[name] = value;
            IsDirty = true;
            return true;
        }

        public float GetDna(string name)
        {
            return name != null && _dna.TryGetValue(name, out float value) ? value : 0.0f;
        }

        public bool ContainsDna(string name)
        {
            return name != null && _dna.ContainsKey(name);
        }

        public void ApplyDnaIfDirty()
        {
            if (IsDirty)
            {
                ApplyDna();
            }
        }

        public void ApplyDna()
        {
            IsDirty = false;

            Mesh mesh = _renderer.sharedMesh;
            if (!mesh)
            {
                return;
            }

            foreach (var converter in _converter.blendshapeDNAConverters)
            {
                int shapeIndex = mesh.GetBlendShapeIndex(converter.blendshapeToApply);
                if (shapeIndex < 0)
                {
                    continue;
                }

                float value = converter.modifyingDNA.Evaluate(_dna);
                _renderer.SetBlendShapeWeight(shapeIndex, value * 100.0f);
            }
        }

        public void Dispose()
        {
            if (_converterIsRuntimeInstance && _converter != null)
            {
                ScriptableObject.DestroyImmediate(_converter);
            }

            _dna.Clear();
            _converter = null;
            _renderer = null;
        }
        
    }
}
