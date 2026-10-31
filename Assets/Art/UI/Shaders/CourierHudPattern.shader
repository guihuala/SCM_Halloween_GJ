Shader "UI/Courier HUD Pattern"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Mint Ink", Color) = (0.64,0.83,0.75,0.13)
        _AccentColor ("Warm Ink", Color) = (0.93,0.84,0.65,0.13)
        _GhostTex ("Little Ghost", 2D) = "black" {}
        _MoonTex ("Moon", 2D) = "black" {}
        _StarTex ("Sparkle", 2D) = "black" {}
        _ParcelTex ("Parcel", 2D) = "black" {}
        _TileSize ("Icon Spacing (UI units)", Float) = 180
        _IconSize ("Icon Size (UI units)", Float) = 42
        _Speed ("Scroll Speed (cycles/sec)", Float) = 0.018
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
            sampler2D _MainTex, _GhostTex, _MoonTex, _StarTex, _ParcelTex;
            fixed4 _Color, _AccentColor;
            float4 _ClipRect;
            float _TileSize, _IconSize, _ScrollPhase;
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
                // The complete 2 x 2 motif moves by two cells on each axis per cycle.
                // Both icon selection and UVs therefore match exactly at the wrap.
                float tile = max(_TileSize, 1.0);
                float2 grid = i.localPosition / tile + float2(-2, 2) * frac(_ScrollPhase);
                float2 cell = floor(grid);
                float2 parity = cell - 2 * floor(cell / 2);
                float2 p = (frac(grid) - 0.5) * tile / max(_IconSize, 1.0);
                float angle = (parity.x * 2 - 1) * 0.18;
                p = float2(p.x * cos(angle) - p.y * sin(angle), p.x * sin(angle) + p.y * cos(angle));
                float2 uv = p + 0.5;
                float inside = step(0, uv.x) * step(uv.x, 1) * step(0, uv.y) * step(uv.y, 1);
                float ghost = tex2D(_GhostTex, saturate(uv)).a;
                float moon = tex2D(_MoonTex, saturate(uv)).a;
                float star = tex2D(_StarTex, saturate(uv)).a;
                float parcel = tex2D(_ParcelTex, saturate(uv)).a;
                float shape = lerp(lerp(ghost, moon, parity.x), lerp(star, parcel, parity.x), parity.y);
                fixed4 ink = lerp(_Color, _AccentColor, abs(parity.x - parity.y));
                // A quiet center keeps the map readable; the margins carry most of the pattern.
                float edge = smoothstep(0.45, 0.95, max(abs(i.uv.x * 2 - 1), abs(i.uv.y * 2 - 1)));
                fixed4 color = ink * i.color;
                color.a *= shape * inside * lerp(0.22, 1.0, edge);
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
