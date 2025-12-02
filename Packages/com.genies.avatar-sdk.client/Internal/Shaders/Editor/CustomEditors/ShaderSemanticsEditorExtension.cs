using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;

namespace Genies.Components.Shaders
{
    /// <summary>
    /// A custom editor for shaders that allow you to set some shader semantic fields in
    /// material inspector that weren't previously possible. These fields will
    /// be set globally, meaning for all materials using the shader.
    /// </summary>
    /// <remarks>
    /// To use -
    /// for the shader whose properties you'd like to set from material inspector.. in the graph inspector.. in the CustomEditorGUI field..
    /// set it to Genies.Components.Shaders.ShaderSemanticsEditorExtension
    /// the material inspector using that shader should then have a properties section at the top
    /// </remarks>
    public class ShaderSemanticsEditorExtension : ShaderGUI
    {
        //regex
        private const string _renderFacePattern = @"""m_RenderFace"":\s*(\d+)";
        private const string _surfaceTypePattern = @"""m_SurfaceType"":\s*(\d+)";
        private const string _alphaClipPattern = @"""m_AlphaClip"":\s*(true|false)";

        private enum SurfaceType
        {
            Opaque = 0,
            Transparent = 1,
        }

        private enum RenderFace
        {
            Front,
            Back,
            Both,
        }

        //state of shader
        private Shader _currShader;
        private SurfaceType _currSurfaceType;
        private RenderFace _currRenderFace;
        private bool _useAlphaClip;
        
        //update frequency params
        private Stopwatch _stopwatch;
        private float _hasBeenLongerThan = 3f;

        override public void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            //in case they updated from within the shadergraph, update values every so often
            var forceUpdate = false;
            if (_stopwatch?.ElapsedMilliseconds >= _hasBeenLongerThan * 1000)
            {
                forceUpdate = true;
            }

            Shader shader = (materialEditor.target as Material).shader;
            if ((shader != null && _currShader != shader) || forceUpdate)
            {
                _currShader = shader;
                GetShaderValues(_currShader);
                _stopwatch = new Stopwatch();
                _stopwatch.Start();
            }

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.LabelField("Global Properties (applied to every material using the shader)");
            
            _currSurfaceType = (SurfaceType) EditorGUILayout.EnumPopup("Surface Type", _currSurfaceType);
            _currRenderFace = (RenderFace)EditorGUILayout.EnumPopup("Render Face", _currRenderFace);
            _useAlphaClip = EditorGUILayout.Toggle("Alpha Clipping", _useAlphaClip);

            EditorGUILayout.Separator();
            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(2));
            
            var hasChanged = EditorGUI.EndChangeCheck();
            if (hasChanged)
            {
                UpdateShadergraphAsset(_currShader);
            }

            //default editor as well
            base.OnGUI(materialEditor, properties);
        }

        //gets the values of concern from the shader and sets them
        private void GetShaderValues(Shader shader)
        {
            string content = GetShaderText(shader);
            var match = Regex.Match(content, _surfaceTypePattern);
            if (match.Success)
            {
                var value = match.Groups[1].Value;
                switch (value)
                {
                    case "0":
                        _currSurfaceType = SurfaceType.Opaque;
                        break;
                    case "1":
                        _currSurfaceType = SurfaceType.Transparent;
                        break;
                }
            }
            match = Regex.Match(content, _renderFacePattern);
            if (match.Success)
            {
                var value = match.Groups[1].Value;
                switch (value)
                {
                    case "0":
                        _currRenderFace = RenderFace.Both;
                        break;
                    case "1":
                        _currRenderFace = RenderFace.Back;
                        break;
                    case "2":
                        _currRenderFace = RenderFace.Front;
                        break;
                }
            }
            match = Regex.Match(content, _alphaClipPattern);
            if (match.Success)
            {
                var value = match.Groups[1].Value;
                switch (value)
                {
                    case "true":
                        _useAlphaClip = true;
                        break;
                    case "false":
                        _useAlphaClip = false;
                        break;
                }
            }
        }

        private void UpdateShadergraphAsset(Shader shader)
        {
            var content = GetShaderText(shader);
            string replaced = Regex.Replace(content, _surfaceTypePattern, $"\"m_SurfaceType\": {(int)_currSurfaceType}");
            replaced = Regex.Replace(replaced, _renderFacePattern, $"\"m_RenderFace\": {2 - (int)_currRenderFace}");
            replaced = Regex.Replace(replaced, _alphaClipPattern, $"\"m_AlphaClip\": {(_useAlphaClip ? "true" : "false")}");
            SaveShaderText(shader, replaced);
        }

        private string GetShaderText(Shader shader)
        {
            string shaderPath = AssetDatabase.GetAssetPath(shader);
            string fullPath = Path.Combine(Directory.GetCurrentDirectory(), shaderPath);
            string content = File.ReadAllText(fullPath);
            return content;
        }

        private void SaveShaderText(Shader shader, string replacement)
        {
            string shaderPath = AssetDatabase.GetAssetPath(shader);
            string fullPath = Path.Combine(Directory.GetCurrentDirectory(), shaderPath);
            File.WriteAllText(fullPath, replacement);
            AssetDatabase.ImportAsset(shaderPath, ImportAssetOptions.ForceUpdate);
        }
    }
}
