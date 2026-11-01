Shader "UI/Courier Gameplay Entrance Fog"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Progress ("Reveal", Range(0,1)) = 0
        _FogTime ("Unscaled Time", Float) = 0
        _ReducedMotion ("Reduced Motion", Float) = 0
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        GrabPass { "_CourierEntranceBackground" }
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        Cull Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float4 screen : TEXCOORD0; float2 uv : TEXCOORD1; };
            sampler2D _CourierEntranceBackground;
            float4 _CourierEntranceBackground_TexelSize;
            float _Progress, _FogTime, _ReducedMotion;
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.screen = ComputeGrabScreenPos(o.vertex);
                o.uv = v.uv;
                return o;
            }
            float hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }
            float noise(float2 p)
            {
                float2 cell = floor(p), f = frac(p);
                f = f * f * (3 - 2 * f);
                return lerp(lerp(hash(cell), hash(cell + float2(1,0)), f.x),
                    lerp(hash(cell + float2(0,1)), hash(cell + 1), f.x), f.y);
            }
            float clouds(float2 p)
            {
                return noise(p) * .57 + noise(p * 2.03 + 9.7) * .29
                    + noise(p * 4.11 + 23.1) * .14;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.screen.xy / i.screen.w;
                fixed3 scene = tex2D(_CourierEntranceBackground, uv).rgb;
                if (_ReducedMotion > .5)
                    return fixed4(lerp(float3(.2,.29,.29), scene, _Progress), 1);

                float aspect = _ScreenParams.x / _ScreenParams.y;
                float2 p = (i.uv - .5) * float2(aspect, 1);
                float distance = length(p) / length(float2(aspect, 1) * .5);
                float2 outward = p / max(length(p), .001);
                // Two differently drifting cloud banks, advected away from the center.
                float2 drift = p * 3.5 - outward * _FogTime * .32;
                float n = clouds(drift + float2(_FogTime * .035, -_FogTime * .07));
                float detail = clouds(p * 6 - float2(_FogTime * .12, _FogTime * .04));
                float progress = smoothstep(0, 1, _Progress);
                float radius = lerp(-.38, 1.55, progress);
                float bank = smoothstep(radius - .25, radius + .25,
                    distance + (n - .5) * .55);
                float opacity = saturate(bank * (.88 + n * .3));

                // Blurred silhouettes become sharp as the local fog thins.
                float2 blur = abs(_CourierEntranceBackground_TexelSize.xy)
                    * (7 * _ScreenParams.y / 1080) * bank;
                fixed3 soft = scene * .2;
                soft += tex2D(_CourierEntranceBackground, uv + float2(blur.x,0)).rgb * .1;
                soft += tex2D(_CourierEntranceBackground, uv - float2(blur.x,0)).rgb * .1;
                soft += tex2D(_CourierEntranceBackground, uv + float2(0,blur.y)).rgb * .1;
                soft += tex2D(_CourierEntranceBackground, uv - float2(0,blur.y)).rgb * .1;
                soft += tex2D(_CourierEntranceBackground, uv + blur).rgb * .1;
                soft += tex2D(_CourierEntranceBackground, uv - blur).rgb * .1;
                soft += tex2D(_CourierEntranceBackground, uv + float2(blur.x,-blur.y)).rgb * .1;
                soft += tex2D(_CourierEntranceBackground, uv + float2(-blur.x,blur.y)).rgb * .1;
                fixed3 fog = lerp(float3(.14,.23,.25), float3(.63,.71,.65),
                    smoothstep(.18,.82,n * .75 + detail * .25));
                return fixed4(lerp(soft, fog, opacity), 1);
            }
            ENDCG
        }
    }
    Fallback "UI/Default"
}
