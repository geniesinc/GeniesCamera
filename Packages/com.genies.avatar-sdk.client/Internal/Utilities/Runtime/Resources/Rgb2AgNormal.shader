Shader "Genies/Utils/RGB to AG Normal"
{
    Properties
    {
        [NoScaleOffset] _MainTex ("Texture", 2D) = "white" { }
    }

    SubShader
    {
        Blend One Zero // disable blending so we override render target channels
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
                fixed4 color = tex2D(_MainTex, input.uv);
                return fixed4(1.0, color.g, 1.0, color.r);
            }
            ENDCG
        }
    }
}
