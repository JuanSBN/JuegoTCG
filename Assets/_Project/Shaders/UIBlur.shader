Shader "UI/UIBlur"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint Color", Color) = (0, 0, 0, 0.65)
        _BlurSize ("Blur Size", Range(0, 10)) = 3.5
        _Darkness ("Darkness Intensity", Range(0, 1)) = 0.6
        
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
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
            Name "UIBlurPass"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord  : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float _BlurSize;
            float _Darkness;
            float4 _ClipRect;
            sampler2D _CameraOpaqueTexture;
            float4 _CameraOpaqueTexture_TexelSize;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.vertex = UnityObjectToClipPos(v.vertex);
                OUT.screenPos = ComputeScreenPos(OUT.vertex);
                OUT.texcoord = v.texcoord;
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 uv = IN.screenPos.xy / IN.screenPos.w;
                float2 texel = _CameraOpaqueTexture_TexelSize.xy * _BlurSize;

                // 9-Tap Poisson / Gaussian sampling for smooth frosted blur
                fixed4 col = tex2D(_CameraOpaqueTexture, uv) * 0.227027;
                col += tex2D(_CameraOpaqueTexture, uv + float2( texel.x * 1.3846,  texel.y * 1.3846)) * 0.158594;
                col += tex2D(_CameraOpaqueTexture, uv - float2( texel.x * 1.3846,  texel.y * 1.3846)) * 0.158594;
                col += tex2D(_CameraOpaqueTexture, uv + float2(-texel.x * 1.3846,  texel.y * 1.3846)) * 0.158594;
                col += tex2D(_CameraOpaqueTexture, uv + float2( texel.x * 1.3846, -texel.y * 1.3846)) * 0.158594;
                col += tex2D(_CameraOpaqueTexture, uv + float2( 0,  texel.y * 3.2307)) * 0.035078;
                col += tex2D(_CameraOpaqueTexture, uv - float2( 0,  texel.y * 3.2307)) * 0.035078;
                col += tex2D(_CameraOpaqueTexture, uv + float2( texel.x * 3.2307, 0)) * 0.035078;
                col += tex2D(_CameraOpaqueTexture, uv - float2( texel.x * 3.2307, 0)) * 0.035078;

                // Fallback check if opaque texture is dark or unavailable
                if (col.a <= 0.01)
                {
                    col = fixed4(0.04, 0.08, 0.05, 1.0);
                }

                // Blend with dark emerald frosted glass tint
                fixed4 tint = IN.color;
                fixed4 finalCol = lerp(col * (1.0 - _Darkness * 0.5), tint, tint.a);
                finalCol.a = tint.a;

                #ifdef UNITY_UI_CLIP_RECT
                finalCol.a *= UnityGet2DClipping(IN.vertex.xy, _ClipRect);
                #endif

                return finalCol;
            }
            ENDCG
        }
    }
}
