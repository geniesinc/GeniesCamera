// UI shader
Shader "Genies/ColorPresetIcon"
{
    Properties
    {
        _InnerColor("InnerColor", Color) = (1,1,1,1)
		_MidColor("MidColor", Color) = (1,1,1,1)
        _OuterColor("OuterColor", Color) = (1,1,1,1)
        _Border("Border Width", Float) = 0.06
        _BorderColor("BorderColor", Color) = (1,1,1,1)

        // Unity expects any UI shader to have this
        [PerRendererData] [HideInInspector] _MainTex("Main Texture - Unused", 2D) = "black" {}
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
        
        _StencilComp("Stencil Comparison", Float) = 8
        _Stencil("Stencil ID", Float) = 0
        _StencilOp("Stencil Operation", Float) = 0
        _StencilWriteMask("Stencil Write Mask", Float) = 255
        _StencilReadMask("Stencil Read Mask", Float) = 255
    }

    SubShader
    {
        Tags
        {
             "RenderType" = "Transparent"
             "Queue" = "Transparent"
             "RenderPipeline" = "UniversalPipeline"
             "IgnoreProjector" = "True"
        }
        LOD 300

        Pass
        {
            Cull Off
            Lighting Off
            ZWrite Off
            ZTest [unity_GUIZTestMode]
            Blend SrcAlpha OneMinusSrcAlpha
            Stencil
            {
                Ref[_Stencil]
                Comp[_StencilComp]
                Pass[_StencilOp]
                ReadMask[_StencilReadMask]
                WriteMask[_StencilWriteMask]
            }
            ColorMask [_ColorMask]

            HLSLPROGRAM
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT

            struct vertdata
            {
                float4 vertex : POSITION;  // vertex position input
                float2 uv : TEXCOORD0;  // 1st texture coordinate input
                float4 color    : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID //For instancing support
            };

            struct v2f  // interpolated
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color    : COLOR;
                half4  mask : TEXCOORD2;
            };

			half4 _InnerColor;
			half4 _MidColor;
			half4 _OuterColor;
            half _Border;
            half4 _BorderColor;

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
            float _UIMaskSoftnessX;
            float _UIMaskSoftnessY;

            v2f vert (vertdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);

                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                float2 pixelSize = o.pos.w;
                float4 clampedRect = clamp(_ClipRect, -2e10, 2e10);
                o.mask = half4(v.vertex.xy * 2 - clampedRect.xy - clampedRect.zw, 0.25 / (0.25 * half2(_UIMaskSoftnessX, _UIMaskSoftnessY) + abs(pixelSize.xy)));
                o.color = v.color;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                half2 delta = (i.uv - half2(0.5, 0.5))*2.f;
                half rad = length(delta);

                half aa = ddx(i.uv.x);
                half r = 1.0 - _Border - aa;

                half4 inner_color_edge = smoothstep(r * 0.33f - aa, r * 0.33f + aa, rad);
                half4 mid_color_edge = smoothstep(r * 0.66f - aa, r * 0.66f + aa, rad);
                half4 outer_color_edge = smoothstep(r - aa, r + aa, rad);
                half4 color = lerp(_InnerColor, _MidColor, inner_color_edge);
                color = lerp(color, _OuterColor, mid_color_edge);
                color = lerp(color, _BorderColor, outer_color_edge);
                half alpha = color.a * smoothstep(r + _Border + aa, r + _Border - aa, rad);

                //For RectMask2D Support
                #ifdef UNITY_UI_CLIP_RECT
                half2 m = saturate((_ClipRect.zw - _ClipRect.xy - abs(i.mask.xy)) * i.mask.zw);
                alpha *= m.x * m.y;
                #endif

                alpha *= i.color.a;
                color.rgb *= i.color.rgb;
                
                return half4(color.rgb, alpha);
            }
            ENDHLSL
        }
    }
}
