Shader "PlanetHistory/Atmosphere"
{
    Properties
    {
        _AtmosphereColor ("Atmosphere Color", Color) = (0.5, 0.7, 1.0, 1.0)
        _AtmosphereDensity ("Atmosphere Density", Range(0, 10)) = 1.5
        _RimPower ("Rim Power", Range(0.1, 10.0)) = 3.0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 viewDirWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _AtmosphereColor;
                float _AtmosphereDensity;
                float _RimPower;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.viewDirWS = normalize(GetCameraPositionWS() - positionWS);
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float3 normal = normalize(input.normalWS);
                float3 viewDir = normalize(input.viewDirWS);

                // Rim lighting
                float NdotV = saturate(dot(normal, viewDir));
                float rim = 1.0 - NdotV;
                rim = pow(rim, _RimPower);

                // Basic directional lighting for day/night
                Light mainLight = GetMainLight();
                float NdotL = saturate(dot(normal, mainLight.direction));
                float diffuse = NdotL * 0.8 + 0.2; // Softer terminator

                float alpha = rim * _AtmosphereDensity * diffuse * _AtmosphereColor.a;
                return float4(_AtmosphereColor.rgb, saturate(alpha));
            }
            ENDHLSL
        }
    }
}
