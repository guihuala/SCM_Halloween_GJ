Shader "UI/Main Menu Atmosphere"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Strength ("Atmosphere Strength", Range(0,1)) = 0.7
        _FogColor ("Moonlit Fog", Color) = (0.67,0.88,0.79,1)
        _AtmosphereTime ("Unscaled Time", Float) = 0
        _Departure ("Departure", Range(0,1)) = 0
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
                float4 screenPosition : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            sampler2D _MainTex;
            fixed4 _FogColor, _TextureSampleAdd;
            float4 _ClipRect;
            float _Strength, _AtmosphereTime, _Departure;
            float _UIMaskSoftnessX, _UIMaskSoftnessY;
            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.screenPosition = ComputeScreenPos(o.vertex);
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
                float2 uv = i.screenPosition.xy / i.screenPosition.w;
                float2 p = uv * float2(_ScreenParams.x / _ScreenParams.y, 1);
                float t = _AtmosphereTime;
                // Low-frequency layered mist: no texture fetches, no full-screen UV distortion.
                float mist = sin(p.x * 5.5 + p.y * 3.0 - t * .16)
                           * sin(p.y * 8.0 - p.x * 2.0 + t * .11);
                mist = smoothstep(-.35, .85, mist) * (.35 + .65 * (1 - uv.y));
                float glow = .5 + .5 * sin(p.x * 3.0 + p.y * 2.0 + t * .23);
                // Sparse drifting specks in a screen-aspect-correct grid.
                float2 grid = (p + float2(t * .008, -t * .016)) * 14;
                float2 cell = floor(grid);
                float seed = frac(sin(dot(cell, float2(127.1,311.7))) * 43758.5453);
                float2 delta = frac(grid) - float2(.25 + seed * .5, .5);
                float speck = (1 - smoothstep(.012, .055, length(delta)))
                            * step(.84, seed) * (.55 + .45 * sin(t * 1.3 + seed * 30));
                fixed4 color = (tex2D(_MainTex, i.uv) + _TextureSampleAdd) * i.color;
                color.rgb = lerp(color.rgb, _FogColor.rgb, mist * .14 * _Strength);
                color.rgb += _FogColor.rgb * (speck * .38 + glow * .025) * _Strength;
                float vignette = smoothstep(.28, .85, length((uv - .5) * float2(1, .8)));
                color.rgb *= 1 - vignette * .13 * _Strength;
                color.rgb *= 1 - _Departure * .45;
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
