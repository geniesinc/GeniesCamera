Shader "Genies/LoadingShader"
{
    Properties
    {
        _Color ("Tint", Color) = (1,1,1,1)

        _MinColor ("Min Color", Color) = (.75,.75,.75,1)
        _MaxColor ("Max Color", Color) = (.9,.9,.9,1)
        _ScrollSpeed ("Scroll Speed", Float) = .8
        _Pow ("Power", Float) = 15
        _Mult ("Mult", Float) = 1

        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}

        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }


        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord  : TEXCOORD0;
                half4  mask : TEXCOORD2;
                float4 screenPosition : TEXCOORD3;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float4 _ClipRect;
            float4 _MainTex_ST;
            float _UIMaskSoftnessX;
            float _UIMaskSoftnessY;

            float4 _MinColor;
            float4 _MaxColor;
            float _ScrollSpeed;
            float _Pow;
            float _Mult;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                float4 vPosition = UnityObjectToClipPos(v.vertex);
                OUT.screenPosition = ComputeScreenPos(vPosition);
                OUT.vertex = vPosition;

                float2 pixelSize = vPosition.w;
                pixelSize /= float2(1, 1) * abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));

                float4 clampedRect = clamp(_ClipRect, -2e10, 2e10);
                float2 maskUV = (v.vertex.xy - clampedRect.xy) / (clampedRect.zw - clampedRect.xy);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord.xy, _MainTex);
                OUT.mask = half4(v.vertex.xy * 2 - clampedRect.xy - clampedRect.zw, 0.25 / (0.25 * half2(_UIMaskSoftnessX, _UIMaskSoftnessY) + abs(pixelSize.xy)));
                OUT.color = v.color * _Color;

                return OUT;
            }

            float Remap(float In, float2 InMinMax, float2 OutMinMax)
            {
                return OutMinMax.x + (In - InMinMax.x) * (OutMinMax.y - OutMinMax.x) / (InMinMax.y - InMinMax.x);
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 screenPosition = (IN.screenPosition.xy/IN.screenPosition.w);
                float diag = saturate((screenPosition.x + screenPosition.y) * 0.5) * _Mult;
                float time = _Time.y * _ScrollSpeed;

                float val = sin((diag + time) * 5);
                val = Remap(val, float2(-1, 1), float2(0,1));
                val = pow(val, _Pow);
                float4 color = lerp(_MinColor, _MaxColor, val);
                color.a = tex2D(_MainTex, IN.texcoord).a * IN.color.a;

                #ifdef UNITY_UI_CLIP_RECT
                half2 m = saturate((_ClipRect.zw - _ClipRect.xy - abs(IN.mask.xy)) * IN.mask.zw);
                color.a *= m.x * m.y;
                #endif

                return color;
            }
        ENDCG
        }
    }
}
