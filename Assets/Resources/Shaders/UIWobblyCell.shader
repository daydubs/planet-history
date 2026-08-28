Shader "UI/WobblyCell"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _WobbleSpeed ("Wobble Speed", Float) = 2.0
        _WobbleAmplitude ("Wobble Amplitude", Float) = 0.1
        _WobbleFrequency ("Wobble Frequency", Float) = 5.0

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
            Name "Default"
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

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
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float _WobbleSpeed;
            float _WobbleAmplitude;
            float _WobbleFrequency;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);

                OUT.texcoord = v.texcoord;
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // Convert UV (0 to 1) to centered coordinates (-0.5 to 0.5)
                float2 uv = IN.texcoord - 0.5;

                // Calculate angle and radius
                float angle = atan2(uv.y, uv.x);
                float radius = length(uv);

                // Use world position to create a unique offset for each instance
                // So they don't all wobble in perfect synchronization
                float posOffset = IN.worldPosition.x * 0.01 + IN.worldPosition.y * 0.01;

                // Create organic wobble using multiple sine waves
                float wave1 = sin(angle * _WobbleFrequency + _Time.y * _WobbleSpeed + posOffset);
                float wave2 = cos(angle * (_WobbleFrequency * 0.5) - _Time.y * (_WobbleSpeed * 0.7) - posOffset);

                float combinedWobble = (wave1 + wave2) * 0.5;

                // Base radius is 0.4 instead of 0.5 to leave room for the wobble without clipping the UI quad
                float targetRadius = 0.4 + combinedWobble * _WobbleAmplitude;

                // Smooth edge (anti-aliasing)
                // We use fwidth to determine how thick the antialiased edge should be
                float edgeSoftness = fwidth(radius) * 1.5;
                // Fallback for edgeSoftness if fwidth isn't supported well on some platforms
                if(edgeSoftness < 0.001) edgeSoftness = 0.01;

                float alpha = smoothstep(targetRadius, targetRadius - edgeSoftness, radius);

                fixed4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;

                color.a *= alpha;

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif

                return color;
            }
        ENDCG
        }
    }
}
