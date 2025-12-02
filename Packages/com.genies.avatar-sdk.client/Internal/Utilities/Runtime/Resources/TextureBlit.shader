/**
    Used by the TextureBlitter.cs utility to efficiently blit textures into a render texture.
*/

Shader "Genies/Utils/Texture Blit"
{
    Properties
    {
        _Color ("Main Color", Color) = (1,1,1,1)
        [NoScaleOffset] _MainTex ("Texture", 2D) = "white" { }
    }

    SubShader
    {
        Blend One Zero
        Cull Back
        ZWrite Off

        Tags
        {
            "Queue" = "Geometry"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vertex
            #pragma fragment fragment
            
            #include "UnityCG.cginc"

            uniform sampler2D _MainTex;
            uniform fixed4    _Color;
            
            struct VertexInput
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct FragmentInput
            {
                float4 position : SV_POSITION;
                float2 uv       : TEXCOORD0;
            };

            FragmentInput vertex (VertexInput input)
            {
                FragmentInput output;
                output.position = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                
                return output;
            }

            fixed4 fragment (FragmentInput input) : SV_TARGET
            {
                // in the case that the main text comes with mipmaps, make sure to always use the highest res one using the text2Dlod sampler method
                return _Color * tex2Dlod(_MainTex, float4(input.uv, 0, 0));
            }
            ENDCG
        }
    }
}
