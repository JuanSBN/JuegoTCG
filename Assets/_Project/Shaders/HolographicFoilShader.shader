Shader "Shader Graphs/HolographicFoilShader"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _TiltPos ("Tilt Position", Vector) = (0,0,0,0)
        _HoloIntensity ("Holo Intensity", Range(0, 1)) = 0.65
        _ShimmerSpeed ("Shimmer Speed", Float) = 1.5
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
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            fixed4 _Color;
            sampler2D _MainTex;
            float4 _TiltPos;
            float _HoloIntensity;
            float _ShimmerSpeed;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            fixed3 RainbowSpectrum(float t)
            {
                t = frac(t);
                fixed3 c;
                c.r = saturate(abs(t * 6.0 - 3.0) - 1.0);
                c.g = saturate(2.0 - abs(t * 6.0 - 2.0));
                c.b = saturate(2.0 - abs(t * 6.0 - 4.0));
                // Golden highlight boost
                c += fixed3(0.2, 0.12, 0.0) * (1.0 - abs(t - 0.5) * 2.0);
                return saturate(c);
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 texColor = tex2D(_MainTex, IN.texcoord) * IN.color;

                if (texColor.a < 0.02)
                {
                    return fixed4(0, 0, 0, 0);
                }

                // 1. Silky Smooth Rainbow Foil (reacts smoothly to mouse/tilt & time)
                float tiltFactor = (_TiltPos.x * 1.5 + _TiltPos.y * 1.5);
                float foilCoord = (IN.texcoord.x * 1.2 + IN.texcoord.y * 0.8) * 1.5 + _Time.y * 0.25 + tiltFactor;
                fixed3 foilColor = RainbowSpectrum(foilCoord);

                // 2. Dual Smooth Diagonal Shimmer Sweeps (No noise, pure smooth glow waves)
                float sweep1 = frac((IN.texcoord.x + IN.texcoord.y * 0.85) * 1.1 - _Time.y * _ShimmerSpeed * 0.55 + tiltFactor * 0.4);
                float shimmer1 = smoothstep(0.0, 0.12, sweep1) * smoothstep(0.24, 0.12, sweep1);

                float sweep2 = frac((IN.texcoord.x * 0.85 - IN.texcoord.y * 1.1) * 0.9 - _Time.y * _ShimmerSpeed * 0.3 - tiltFactor * 0.4);
                float shimmer2 = smoothstep(0.0, 0.08, sweep2) * smoothstep(0.18, 0.08, sweep2);

                fixed3 glintColor = fixed3(1.0, 0.95, 0.8) * shimmer1 * 0.85 + fixed3(0.75, 0.9, 1.0) * shimmer2 * 0.65;

                // 3. Clean Screen Blend (Pure smooth colors, zero pixelated noise)
                fixed3 holoGlow = (foilColor * 1.3 + glintColor) * _HoloIntensity;
                fixed3 screenBlend = 1.0 - (1.0 - texColor.rgb) * (1.0 - holoGlow * 0.7);
                fixed3 finalRGB = screenBlend + glintColor * 0.4;

                return fixed4(saturate(finalRGB), texColor.a);
            }
            ENDCG
        }
    }
}
