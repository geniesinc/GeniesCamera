Shader "Custom/FourColorSwatch"
{
    Properties
    {
        _ColorBase("ColorBase", Color) = (0, 0, 0, 1)
        _ColorR("ColorR", Color) = (1, 0, 0, 1)
        _ColorG("ColorG", Color) = (0, 1, 0, 1)
        _ColorB("ColorB", Color) = (0, 0, 1, 1)
        _OutlineColor("OutlineColor", Color) = (0.8, 0.8, 0.8, 1)
        _OutlineWidth("OutlineWidth", Float) = 0.03
        _Radius("Radius", Float) = 1.0
        // Unity expects any UI shader to have this
        [HideInInspector] _MainTex("Main Texture - Unused", 2D) = "black" {}
    }
    SubShader
    {
        Tags{
             "RenderType" = "Transparent"
             "Queue" = "Transparent"
             "RenderPipeline" = "UniversalPipeline"
             "IgnoreProjector" = "True"
        }
        LOD 300

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On
            ZTest LEqual
            Lighting Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            
            struct vertdata
            {
                float4 vertex : POSITION;  // vertex position input
                float2 uv : TEXCOORD0;  // 1st texture coordinate input
            };

            struct v2f  // interpolated
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            half4 _ColorBase;
            half4 _ColorR;
            half4 _ColorG; 
            half4 _ColorB;
            half4 _OutlineColor;
            half _OutlineWidth;
            half _Radius;

            v2f vert(vertdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            half rounded_square(half2 in_uv, half side_len, half radius, bool invert) 
            {
                // Ported from the rounded rectangle node doc here: https://docs.unity3d.com/Packages/com.unity.shadergraph@6.9/manual/Rounded-Rectangle-Node.html
                const half clamped_radius = max(min(radius * 2, side_len), 0);
                half2 uv = max(abs(in_uv * 2 - 1) - side_len + clamped_radius, 0);
                half d = length(uv);
                half step_d = step(clamped_radius, d);

                // anti-aliasing
                const half aa = ddx(in_uv.x) * 2;
                const half smoothstep_d = smoothstep(max(clamped_radius - aa, 0), min(clamped_radius + aa, 1), d);

                if(invert)
                    return 1 - smoothstep_d;
                
                return smoothstep_d;
            }
            
            half2 rotate_degrees(half2 uv, half2 center, half rotation)
            {
                // Ported from rotate node doc here: https://docs.unity3d.com/Packages/com.unity.shadergraph@6.9/manual/Rotate-Node.html
                rotation = rotation * (3.1415926f / 180.0f);
                uv -= center;
                half s = sin(rotation);
                half c = cos(rotation);
                half2x2 rMatrix = half2x2(c, -s, s, c);
                rMatrix *= 0.5;
                rMatrix += 0.5;
                rMatrix = rMatrix * 2 - 1;
                uv.xy = mul(uv.xy, rMatrix);
                uv += center;
                return uv;
            }

            half4 frag(v2f i) : SV_Target
            {
                // Create rounded square mask in alpha
                half alpha = _ColorBase.a;
                const half alpha_mask = rounded_square(i.uv, 1, _Radius, true);
                alpha *= alpha_mask;

                // Rotate UV -45 deg
                half2 rotated_uv = rotate_degrees(i.uv, (0.5, 0.5), -45);

                // Gradient with swap points from 17-27%, 45-55%, and 73-83%
                const half stripe1_mask = smoothstep(0.17, 0.27, rotated_uv.x);
                const half stripe2_mask = smoothstep(0.45, 0.55, rotated_uv.x);
                const half stripe3_mask = smoothstep(0.73, 0.83, rotated_uv.x);

                const half3 stripe1 = lerp(_ColorBase.rgb, _ColorR.rgb, stripe1_mask);
                const half3 stripe2 = lerp(stripe1, _ColorG.rgb, stripe2_mask);
                const half3 stripe3 = lerp(stripe2, _ColorB.rgb, stripe3_mask);


                // Create rounded square mask for outline
                const half inner_square_size = 1 - _OutlineWidth;
                const half outline_mask = rounded_square(i.uv, inner_square_size, _Radius * inner_square_size, true);
                half3 color = lerp(_OutlineColor.rgb, stripe3, outline_mask);

                return half4(color, alpha);
            }

            ENDHLSL
        }
    }
}
