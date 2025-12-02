using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Genies.Utilities
{
    /// <summary>
    /// Utility methods to remap texture channels.
    /// </summary>
    public static class TextureChannelTool
    {
        private const string _shaderName = "Genies/Utils/Texture Channel Mapper";

        private static readonly int _channelMaskId = Shader.PropertyToID("_ChannelMask");
        private static readonly int _rOutputId = Shader.PropertyToID("_ROutput");
        private static readonly int _gOutputId = Shader.PropertyToID("_GOutput");
        private static readonly int _bOutputId = Shader.PropertyToID("_BOutput");
        private static readonly int _aOutputId = Shader.PropertyToID("_AOutput");

        private static Material _material;

        public static RenderTexture CreateFromChannels(TextureConfig config, params ChannelMap[] channelMaps)
        {
            return CreateFromChannels(config, config.GetRenderTextureDescriptor(), channelMaps as IEnumerable<ChannelMap>);
        }

        public static RenderTexture CreateFromChannels(TextureConfig config, IEnumerable<ChannelMap> channelMaps)
        {
            return CreateFromChannels(config, config.GetRenderTextureDescriptor(), channelMaps);
        }

        public static RenderTexture CreateFromChannels(TextureConfig config, RenderTextureDescriptor descriptor, params ChannelMap[] channelMaps)
        {
            return CreateFromChannels(config, descriptor, channelMaps as IEnumerable<ChannelMap>);
        }

        public static RenderTexture CreateFromChannels(TextureConfig config, RenderTextureDescriptor descriptor, IEnumerable<ChannelMap> channelMaps)
        {
            RenderTexture renderTexture = config.CreateRenderTexture(descriptor);
            WriteChannels(renderTexture, channelMaps);

            return renderTexture;
        }

        public static void WriteChannels(this RenderTexture renderTexture, params ChannelMap[] channelMaps)
        {
            WriteChannels(renderTexture, channelMaps as IEnumerable<ChannelMap>);
        }

        public static void WriteChannels(this RenderTexture renderTexture, IEnumerable<ChannelMap> channelMaps)
        {
            if (!TryToInitializeMaterial() || channelMaps is null)
            {
                return;
            }

            foreach (ChannelMap channelMap in channelMaps)
            {
                WriteChannels(renderTexture, channelMap);
            }
        }

        public static void WriteChannels(this RenderTexture renderTexture, ChannelMap channelMap)
        {
            if (!TryToInitializeMaterial() || !channelMap.texture)
            {
                return;
            }

            ColorWriteMask colorMask = 0;

            if (channelMap.rOutput is not TextureChannel.None)
            {
                colorMask |= ColorWriteMask.Red;
                _material.SetInteger(_rOutputId, (int)channelMap.rOutput);
            }

            if (channelMap.gOutput is not TextureChannel.None)
            {
                colorMask |= ColorWriteMask.Green;
                _material.SetInteger(_gOutputId, (int)channelMap.gOutput);
            }

            if (channelMap.bOutput is not TextureChannel.None)
            {
                colorMask |= ColorWriteMask.Blue;
                _material.SetInteger(_bOutputId, (int)channelMap.bOutput);
            }

            if (channelMap.aOutput is not TextureChannel.None)
            {
                colorMask |= ColorWriteMask.Alpha;
                _material.SetInteger(_aOutputId, (int)channelMap.aOutput);
            }

            // save the blit operation as there is nothing mapped
            if (colorMask == 0)
            {
                return;
            }

            _material.SetInt(_channelMaskId, (int)colorMask);
            Graphics.Blit(channelMap.texture, renderTexture, _material);
        }

        public static void WriteColor(this RenderTexture renderTexture, Color color)
        {
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture.active = renderTexture;
            GL.Clear(clearDepth: false, clearColor: true, color);
            RenderTexture.active = previousActive;
        }

        private static bool TryToInitializeMaterial()
        {
            if (_material)
            {
                return true;
            }

            var shader = Shader.Find(_shaderName);
            if (!shader)
            {
                Debug.LogError($"[{nameof(TextureChannelTool)}] couldn't load texture channel mapper shader from name: {_shaderName}. Make sure the shader is included in the build");
                return false;
            }

            _material = new Material(shader);
            return true;
        }
    }

    /// <summary>
    /// Maps the <see cref="rOutput"/>, <see cref="gOutput"/>, <see cref="bOutput"/> and <see cref="aOutput"/> channels
    /// to the color channels on the <see cref="texture"/>.
    /// </summary>
    [Serializable]
    public struct ChannelMap
    {
        public Texture texture;
        public TextureChannel rOutput;
        public TextureChannel gOutput;
        public TextureChannel bOutput;
        public TextureChannel aOutput;
    }

    [Serializable]
    public enum TextureChannel
    {
        // these integer values match the integers used in the TextureChannelMapper shader
        R = 0,
        G = 1,
        B = 2,
        A = 3,
        None = 4,
    }
}
