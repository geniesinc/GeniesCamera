Shader "Genies/Utils/Texture Channel Mapper"
{
    Properties
    {
        [NoScaleOffset] _MainTex ("Texture", 2D) = "white" { }
        [KeywordEnum(None, A, B, BA, G, GA, GB, GBA, R, RA, RB, RBA, RG, RGA, RGB, RGBA)]
        _ChannelMask ("Channel Mask", Int) = 15
        [KeywordEnum(R, G, B, A)] _ROutput ("R Channel Output", Integer) = 0
        [KeywordEnum(R, G, B, A)] _GOutput ("G Channel Output", Integer) = 1
        [KeywordEnum(R, G, B, A)] _BOutput ("B Channel Output", Integer) = 2
        [KeywordEnum(R, G, B, A)] _AOutput ("A Channel Output", Integer) = 3
    }

    SubShader
    {
        ColorMask [_ChannelMask]
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

            // enum values of the channel source properties
            #define R 0
            #define G 1
            #define B 2
            #define A 3

            uniform sampler2D _MainTex;
            uniform int _ROutput;
            uniform int _GOutput;
            uniform int _BOutput;
            uniform int _AOutput;

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

            /**
             * I have tested this same approach using shader variants. The performance improvement is only a 8% in my machine and we need
             * 256 variants for all the possible remapping configurations which results in a 150000+ lines of code shader that increases build
             * size in 5 MB. I think that dynamic branching is the best option since branches are symmetric and the shader itself is super simple.
             */
            fixed4 fragment (FragmentInput input) : SV_TARGET
            {
                fixed4 color = tex2D(_MainTex, input.uv);
                fixed r, g, b, a;

                if (_ROutput == G)
                    r = color.g;
                else if (_ROutput == B)
                    r = color.b;
                else if (_ROutput == A)
                    r = color.a;
                else
                    r = color.r;

                if (_GOutput == R)
                    g = color.r;
                else if (_GOutput == B)
                    g = color.b;
                else if (_GOutput == A)
                    g = color.a;
                else
                    g = color.g;

                if (_BOutput == R)
                    b = color.r;
                else if (_BOutput == G)
                    b = color.g;
                else if (_BOutput == A)
                    b = color.a;
                else
                    b = color.b;

                if (_AOutput == R)
                    a = color.r;
                else if (_AOutput == G)
                    a = color.g;
                else if (_AOutput == B)
                    a = color.b;
                else
                    a = color.a;

                return fixed4(r, g, b, a);
            }
            ENDCG
        }
    }
}
