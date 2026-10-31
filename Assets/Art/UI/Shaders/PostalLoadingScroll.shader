Shader "UI/Postal Loading Scroll"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tape Color", Color) = (0.66,0.84,0.76,1)
        _StripeColor ("Stripe Color", Color) = (0.88,0.94,0.85,1)
        _StripeWidth ("Repeat Width (UI units)", Float) = 48
        _Slant ("Stripe Slant", Range(-2,2)) = 0.6
        _Speed ("Scroll Speed (cycles/sec; negative reverses)", Float) = 0.6
        [HideInInspector] _ScrollPhase ("Scroll Phase", Float) = 0
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
            fixed4 _Color, _StripeColor, _TextureSampleAdd;
            float4 _ClipRect;
            float _StripeWidth, _Slant, _ScrollPhase;
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
            fixed4 frag(v2f i) : SV_Target
            {
                // Local coordinates keep stripe spacing uniform across nine-sliced sprites.
                float p = (i.localPosition.x + i.localPosition.y * _Slant) / max(abs(_StripeWidth), 1) - _ScrollPhase;
                float wave = sin(p * 6.2831853);
                float aa = max(fwidth(wave), 0.001);
                fixed4 tape = lerp(_Color, _StripeColor, smoothstep(-aa, aa, wave));
                fixed4 color = (tex2D(_MainTex, i.uv) + _TextureSampleAdd) * i.color * tape;
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
