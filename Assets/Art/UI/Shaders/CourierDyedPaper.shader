Shader "UI/Courier Dyed Paper"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Grain ("Paper Grain", Range(0,1)) = 0.65
        _Mottle ("Uneven Dye", Range(0,1)) = 0.65
        _Fibers ("Paper Fibers", Range(0,1)) = 0.4
        _Vignette ("Edge Depth", Range(0,0.5)) = 0.20
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float2 localPosition : TEXCOORD1;
                half4 mask : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            sampler2D _MainTex;
            fixed4 _TextureSampleAdd;
            float _Grain, _Mottle, _Fibers, _Vignette;
            float4 _ClipRect;
            float _UIMaskSoftnessX, _UIMaskSoftnessY;
            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.localPosition = v.vertex.xy;
                o.uv = v.uv;
                o.color = v.color;
                float2 pixelSize = o.vertex.w;
                pixelSize /= abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));
                float4 rect = clamp(_ClipRect, -2e10, 2e10);
                o.mask = half4(v.vertex.xy * 2 - rect.xy - rect.zw,
                    0.25 / (0.25 * half2(_UIMaskSoftnessX, _UIMaskSoftnessY) + abs(pixelSize)));
                return o;
            }
            // Stable local-space fibers stay attached to the paper, independent of time.
            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 345.45));
                p += dot(p, p + 34.345);
                return frac(p.x * p.y);
            }
            float paperNoise(float2 p)
            {
                float2 cell = floor(p), f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(hash21(cell), hash21(cell + float2(1,0)), f.x),
                    lerp(hash21(cell + float2(0,1)), hash21(cell + 1), f.x), f.y);
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float2 p = i.localPosition;
                float grain = paperNoise(p * 0.38) - 0.5;
                float dye = (paperNoise(p / 160.0) - 0.5) * 0.7
                    + (paperNoise(p / 51.0) - 0.5) * 0.3;
                float fiber = paperNoise(p * float2(0.025, 0.8)) - 0.5;
                float2 edge = abs(i.uv * 2.0 - 1.0);
                float vignette = smoothstep(0.25, 1.25, length(edge * float2(0.72,0.86)));
                float4 color = (tex2D(_MainTex, i.uv) + _TextureSampleAdd) * i.color;
                // Absolute ink variation remains visible on the very dark UI stock.
                // Purely multiplying that stock hid the earlier texture at game-view scale.
                float tooth = grain * _Grain * 0.016 + fiber * _Fibers * 0.012;
                float wash = dye * _Mottle;
                float pool = paperNoise(p / 310.0);
                color.rgb *= 1.12 - vignette * _Vignette;
                color.rgb += tooth * float3(0.78, 0.91, 0.82);
                color.rgb += wash * float3(0.022, 0.034, 0.028);
                color.rgb += (1.0 - vignette) * float3(0.006, 0.012, 0.010);
                color.rgb += smoothstep(0.48, 0.86, pool) * float3(0.008, 0.006, 0.003);
                color.rgb = max(color.rgb, 0.0);
                #ifdef UNITY_UI_CLIP_RECT
                half2 m = saturate((_ClipRect.zw - _ClipRect.xy - abs(i.mask.xy)) * i.mask.zw);
                color.a *= m.x * m.y;
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif
                return color;
            }
            ENDCG
        }
    }
}
