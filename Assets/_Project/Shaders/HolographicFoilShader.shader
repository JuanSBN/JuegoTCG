Shader "Shader Graphs/HolographicFoilShader"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _TiltPos ("Tilt Position", Vector) = (0,0,0,0)
        _HoloSpeed ("Holo Speed", Float) = 1.0
        _HoloIntensity ("Holo Intensity", Float) = 0.55
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
        Blend One OneMinusSrcAlpha

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
            float _HoloSpeed;
            float _HoloIntensity;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            fixed3 Rainbow(float t)
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
                fixed4 c = tex2D(_MainTex, IN.texcoord) * IN.color;
                
                // Calculate rainbow spectrum gradient animated by time & mouse tilt
                float t = IN.texcoord.x * 1.5 + IN.texcoord.y * 1.5 + _Time.y * 0.4 + _TiltPos.x * 0.5 + _TiltPos.y * 0.5;
                fixed3 holo = Rainbow(t);
                
                // Blend holographic rainbow shine over texture
                c.rgb = lerp(c.rgb, c.rgb + holo * _HoloIntensity, c.a * 0.45);
                
                c.rgb *= c.a;
                return c;
            }
            ENDCG
        }
    }
}
