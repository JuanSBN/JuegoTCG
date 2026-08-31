Shader "Hidden/GaussianBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Offset ("Blur Radius", Float) = 1.2
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        // Pass 0: Horizontal Continuous 7-Tap Gaussian
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _Offset;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 texel = float2(_MainTex_TexelSize.x * _Offset, 0);
                fixed4 col = fixed4(0,0,0,0);
                col += tex2D(_MainTex, i.uv - texel * 3.0) * 0.0545;
                col += tex2D(_MainTex, i.uv - texel * 2.0) * 0.1209;
                col += tex2D(_MainTex, i.uv - texel * 1.0) * 0.2041;
                col += tex2D(_MainTex, i.uv)              * 0.2410;
                col += tex2D(_MainTex, i.uv + texel * 1.0) * 0.2041;
                col += tex2D(_MainTex, i.uv + texel * 2.0) * 0.1209;
                col += tex2D(_MainTex, i.uv + texel * 3.0) * 0.0545;
                return col;
            }
            ENDCG
        }

        // Pass 1: Vertical Continuous 7-Tap Gaussian
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _Offset;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 texel = float2(0, _MainTex_TexelSize.y * _Offset);
                fixed4 col = fixed4(0,0,0,0);
                col += tex2D(_MainTex, i.uv - texel * 3.0) * 0.0545;
                col += tex2D(_MainTex, i.uv - texel * 2.0) * 0.1209;
                col += tex2D(_MainTex, i.uv - texel * 1.0) * 0.2041;
                col += tex2D(_MainTex, i.uv)              * 0.2410;
                col += tex2D(_MainTex, i.uv + texel * 1.0) * 0.2041;
                col += tex2D(_MainTex, i.uv + texel * 2.0) * 0.1209;
                col += tex2D(_MainTex, i.uv + texel * 3.0) * 0.0545;
                return col;
            }
            ENDCG
        }
    }
}
