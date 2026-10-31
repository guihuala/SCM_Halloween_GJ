Shader "UI/Courier Landmark Glow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _GlowRadius ("Glow UV Radius", Vector) = (0.02,0.02,0,0)
        _SpriteUVRect ("Sprite UV Bounds", Vector) = (0,0,1,1)
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
            float4 _ClipRect;
            float4 _GlowRadius;
            float4 _SpriteUVRect;
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
            // Clamp every sample to this sprite's cell: packed neighbours must never glow.
            half SpriteAlpha(float2 uv)
            {
                float2 lo = _SpriteUVRect.xy, hi = _SpriteUVRect.zw;
                half inside = step(lo.x, uv.x) * step(lo.y, uv.y) * step(uv.x, hi.x) * step(uv.y, hi.y);
                float2 inset = min((hi-lo)*.001, float2(.0001,.0001));
                return (tex2D(_MainTex, clamp(uv, lo+inset, hi-inset)).a + _TextureSampleAdd.a) * inside;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                half center = SpriteAlpha(i.uv);
                half nearEdge = 0, softEdge = 0;
                [unroll] for (int sampleIndex = 0; sampleIndex < 16; sampleIndex++)
                {
                    float angle = sampleIndex * 6.2831853 / 16;
                    float2 direction = float2(cos(angle), sin(angle)) * _GlowRadius.xy;
                    nearEdge = max(nearEdge, SpriteAlpha(i.uv + direction * .28));
                    softEdge += SpriteAlpha(i.uv + direction) * .028;
                    softEdge += SpriteAlpha(i.uv + direction * .62) * .034;
                }
                // A narrow bright rim with a soft halo outside the actual alpha silhouette.
                half glow = saturate(nearEdge * .7 + softEdge * .85) * (1-center);
                fixed4 color = fixed4(i.color.rgb, i.color.a * glow);
                #ifdef UNITY_UI_CLIP_RECT
                half2 m = saturate((_ClipRect.zw - _ClipRect.xy - abs(i.mask.xy)) * i.mask.zw);
                color.a *= m.x * m.y;
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - .001);
                #endif
                return color;
            }
            ENDCG
        }
    }
}
