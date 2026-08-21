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
                return c;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 texColor = tex2D(_MainTex, IN.texcoord) * IN.color;

                // 1. Interactive Rainbow Foil (Follows UV + Time + Mouse/Tilt)
                float foilCoord = (IN.texcoord.x + IN.texcoord.y) * 1.8 + _Time.y * 0.35 + (_TiltPos.x + _TiltPos.y) * 0.8;
                fixed3 foilColor = RainbowSpectrum(foilCoord);

                // 2. Animated Diagonal Shimmer Sweep (Like in HTML prototype)
                float shimmerLine = frac((IN.texcoord.x + IN.texcoord.y * 0.8) - _Time.y * _ShimmerSpeed * 0.5);
                float shimmer = smoothstep(0.0, 0.15, shimmerLine) * smoothstep(0.3, 0.15, shimmerLine);
                fixed3 shimmerColor = fixed3(1.0, 0.95, 0.8) * shimmer * 0.8;

                // Blend holographic foil + shimmer into sprite texture
                fixed3 finalRGB = texColor.rgb + (foilColor * 0.7 + shimmerColor) * _HoloIntensity;
                
                return fixed4(finalRGB, texColor.a);
            }
            ENDCG
        }
    }
}
