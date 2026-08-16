// Rendu du relief planétaire à partir d'un heightmap équirectangulaire (RFloat).
// La géométrie reste une sphère lisse: normales et couleur sont calculées dans le
// fragment shader à partir de _HeightTex (différences finies).
// Pipeline: Universal Render Pipeline (URP).
Shader "PlanetHistory/PlanetSurface"
{
    Properties
    {
        [NoScaleOffset] _HeightTex ("Height Map (R, equirect)", 2D) = "black" {}

        _NormalStrength ("Normal Strength", Range(0, 200)) = 40

        [Header(Height Ramp)]
        _OceanLevel ("Ocean Level", Float) = 0.02
        _ShoreLevel ("Shore Level", Float) = 0.08
        _LandLevel ("Land Level", Float) = 0.35
        _MountainLevel ("Mountain Level", Float) = 0.7

        _OceanColor ("Ocean Color", Color) = (0.02, 0.12, 0.32, 1)
        _ShoreColor ("Shore Color", Color) = (0.72, 0.68, 0.45, 1)
        _LandColor ("Land Color", Color) = (0.16, 0.35, 0.14, 1)
        _MountainColor ("Mountain Color", Color) = (0.38, 0.33, 0.29, 1)
        _IceColor ("Ice Color", Color) = (0.92, 0.95, 1.0, 1)

        [Header(Poles)]
        _IceLatitude ("Ice Latitude (0..1)", Range(0, 1)) = 0.82
        _IceBlend ("Ice Blend", Range(0.001, 0.5)) = 0.08

        [Header(Lighting)]
        _Smoothness ("Smoothness", Range(0, 1)) = 0.15
        _SpecularStrength ("Specular Strength", Range(0, 1)) = 0.2

        [Header(Optional Displacement)]
        [Toggle(_DISPLACE)] _Displace ("Vertex Displacement", Float) = 0
        _DisplaceScale ("Displacement Scale", Float) = 0.5

        [Header(Lava and Water States)]
        _SurfaceTemperature ("Surface Temperature (K)", Float) = 1800.0
        _WaterRatio ("Water Ratio (0..1)", Float) = 0.0
        _LavaEmissionStrength ("Lava Emission Strength", Range(0, 5)) = 2.0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        TEXTURE2D(_HeightTex);
        SAMPLER(sampler_HeightTex);

        CBUFFER_START(UnityPerMaterial)
            float4 _HeightTex_TexelSize;
            float _NormalStrength;
            float _OceanLevel;
            float _ShoreLevel;
            float _LandLevel;
            float _MountainLevel;
            float4 _OceanColor;
            float4 _ShoreColor;
            float4 _LandColor;
            float4 _MountainColor;
            float4 _IceColor;
            float _IceLatitude;
            float _IceBlend;
            float _Smoothness;
            float _SpecularStrength;
            float _Displace;
            float _DisplaceScale;
            float _SurfaceTemperature;
            float _WaterRatio;
            float _LavaEmissionStrength;
        CBUFFER_END

        // UV équirectangulaire depuis une direction unitaire.
        // Calculée par pixel: pas de couture visible (le saut de u n'est jamais interpolé).
        float2 DirToEquirectUV(float3 dir)
        {
            float u = atan2(dir.z, dir.x) * (1.0 / (2.0 * PI)) + 0.5;
            float v = asin(clamp(dir.y, -1.0, 1.0)) * (1.0 / PI) + 0.5;
            return float2(u, v);
        }

        float SampleHeight(float2 uv)
        {
            // LOD explicite: les dérivées d'atan2 explosent sur la couture.
            return SAMPLE_TEXTURE2D_LOD(_HeightTex, sampler_HeightTex, uv, 0).r;
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma shader_feature_local _DISPLACE

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _FORWARD_PLUS _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 dirOS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float fogCoord : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 positionOS = input.positionOS.xyz;
                float3 dirOS = normalize(positionOS);

                #ifdef _DISPLACE
                    // Le heightmap étant identique de part et d'autre de la couture (wrap Repeat),
                    // l'échantillonnage par vertex reste cohérent.
                    float h = SampleHeight(input.uv);
                    positionOS += dirOS * (h * _DisplaceScale);
                #endif

                output.positionWS = TransformObjectToWorld(positionOS);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.dirOS = dirOS;
                output.uv = input.uv;
                output.fogCoord = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            float3 PerturbedNormalOS(float3 dirOS, float2 uv, out float height)
            {
                float du = _HeightTex_TexelSize.x;
                float dv = _HeightTex_TexelSize.y;

                height = SampleHeight(uv);
                float hL = SampleHeight(float2(uv.x - du, uv.y));
                float hR = SampleHeight(float2(uv.x + du, uv.y));
                float hD = SampleHeight(float2(uv.x, saturate(uv.y - dv)));
                float hU = SampleHeight(float2(uv.x, saturate(uv.y + dv)));

                // Les texels se resserrent en longitude près des pôles.
                float cosLat = max(sqrt(saturate(1.0 - dirOS.y * dirOS.y)), 0.15);

                float dhdx = (hR - hL) * 0.5 * _NormalStrength / cosLat;
                float dhdy = (hU - hD) * 0.5 * _NormalStrength;

                float3 up = float3(0, 1, 0);
                float3 east = cross(dirOS, up);
                float eastLen = length(east);
                east = eastLen > 1e-4 ? east / eastLen : float3(0, 0, 1);
                float3 north = cross(east, dirOS);

                return normalize(dirOS - dhdx * east - dhdy * north);
            }

            // Simple hash/noise helper
            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453123);
            }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(hash(i + float2(0.0,0.0)), hash(i + float2(1.0,0.0)), u.x),
                            lerp(hash(i + float2(0.0,1.0)), hash(i + float2(1.0,1.0)), u.x), u.y);
            }

            float fbm(float2 p)
            {
                float v = 0.0;
                float a = 0.5;
                float2 shift = float2(100.0, 100.0);
                for (int i = 0; i < 4; ++i) {
                    v += a * noise(p);
                    p = p * 2.0 + shift;
                    a *= 0.5;
                }
                return v;
            }

            float3 HeightAlbedo(float height, float3 dirOS, float2 uv)
            {
                // 1. Calculate standard land colors (vibrant scheme)
                float3 standardShore = _ShoreColor.rgb;
                float3 standardLand = lerp(standardShore, _LandColor.rgb, smoothstep(_ShoreLevel, _LandLevel, height));
                float3 standardMountain = lerp(standardLand, _MountainColor.rgb, smoothstep(_LandLevel, _MountainLevel, height));

                // 2. Calculate lifeless volcanic ash / basalt grey scheme (for dry/hot planet)
                float3 dryShore = float3(0.12, 0.12, 0.13); // dark charcoal/soot valleys
                float3 dryLand = float3(0.18, 0.18, 0.20);  // dark basalt/ash land
                float3 dryMountain = float3(0.35, 0.35, 0.35); // ash grey peaks

                float3 volcanicLand = lerp(dryShore, dryLand, smoothstep(_ShoreLevel, _LandLevel, height));
                volcanicLand = lerp(volcanicLand, dryMountain, smoothstep(_LandLevel, _MountainLevel, height));

                // Blend between dry volcanic ash and living standard land based on WaterRatio
                float3 landColor = lerp(volcanicLand, standardMountain, _WaterRatio);

                // Ice / poles also scale or appear with _WaterRatio
                float latitude01 = abs(dirOS.y); // 0 = equator, 1 = pole
                float ice = smoothstep(_IceLatitude - _IceBlend, _IceLatitude + _IceBlend, latitude01) * _WaterRatio;
                landColor = lerp(landColor, _IceColor.rgb, ice);

                float3 color = landColor;

                // 3. Add flowing lava layer on top of land if the temperature is high enough
                // Lava starts appearing above 500 K and is fully dominant at 1400 K+
                float lavaMask = smoothstep(500.0, 1400.0, _SurfaceTemperature);
                if (lavaMask > 0.001)
                {
                    // Add lower height bias so lava is more prominent in lower/depression regions (valley/oceans)
                    float heightLavaBias = smoothstep(0.5, 0.1, height);
                    lavaMask = saturate(lavaMask * (0.3 + 0.7 * heightLavaBias));

                    if (lavaMask > 0.001)
                    {
                        // Scrolling UVs for the dual-texture/flow effect
                        float2 uvFlow1 = uv * 8.0 + float2(_Time.y * 0.15, _Time.y * 0.08);
                        float2 uvFlow2 = uv * 8.0 - float2(_Time.y * 0.1, _Time.y * 0.12);

                        float n1 = fbm(uvFlow1);
                        float n2 = fbm(uvFlow2);
                        float flow = smoothstep(0.3, 0.7, (n1 + n2) * 0.5);

                        // Liquid magma colors (bright red-orange to deep dark red)
                        float3 lavaBaseColor = float3(0.75, 0.02, 0.0); // deep dark glowing red
                        float3 lavaHotColor = float3(1.0, 0.28, 0.0);   // vibrant fiery red-orange
                        float3 currentLavaColor = lerp(lavaBaseColor, lavaHotColor, flow);

                        color = lerp(color, currentLavaColor, lavaMask);
                    }
                }

                // 4. Dynamic progressive water layer on top of terrain/lava (water floods and extinguishes lava)
                if (_WaterRatio > 0.001)
                {
                    float maxWaterLevel = _OceanLevel * 1.5;
                    float waterLevel = maxWaterLevel * _WaterRatio;

                    if (height < waterLevel + _OceanLevel)
                    {
                        // Noise-based height perturbation to create organic shorelines on flat terrain (height=0)
                        float floorNoise = fbm(uv * 20.0) * _OceanLevel;
                        float perturbHeight = height + floorNoise;

                        float depth = waterLevel - perturbHeight;
                        if (depth > 0.0)
                        {
                            // Smooth blend/fade of the water color based on depth
                            float waterBlend = saturate(depth * 100.0);
                            color = lerp(color, _OceanColor.rgb, waterBlend);
                        }
                    }
                }

                return color;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float3 dirOS = normalize(input.dirOS);
                float2 uv = DirToEquirectUV(dirOS);

                float height;
                float3 normalOS = PerturbedNormalOS(dirOS, uv, height);
                float3 normalWS = normalize(TransformObjectToWorldNormal(normalOS));
                float3 viewDirWS = normalize(GetWorldSpaceViewDir(input.positionWS));

                float3 albedo = HeightAlbedo(height, dirOS, uv);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = albedo;
                surfaceData.specular = _SpecularStrength.xxx;
                surfaceData.smoothness = _Smoothness;
                surfaceData.normalTS = float3(0, 0, 1);
                surfaceData.alpha = 1;
                surfaceData.occlusion = 1;

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = viewDirWS;
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                inputData.fogCoord = input.fogCoord;
                inputData.bakedGI = SampleSH(normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = half4(1, 1, 1, 1);

                half4 color = UniversalFragmentBlinnPhong(inputData, surfaceData);

                // Add Emission for glowing lava (extinguished under water)
                float lavaMask = smoothstep(500.0, 1400.0, _SurfaceTemperature);
                if (lavaMask > 0.001)
                {
                    float heightLavaBias = smoothstep(0.5, 0.1, height);
                    lavaMask = saturate(lavaMask * (0.3 + 0.7 * heightLavaBias));

                    if (_WaterRatio > 0.001)
                    {
                        float maxWaterLevel = _OceanLevel * 1.5;
                        float waterLevel = maxWaterLevel * _WaterRatio;
                        if (height < waterLevel + _OceanLevel)
                        {
                            float floorNoise = fbm(uv * 20.0) * _OceanLevel;
                            float perturbHeight = height + floorNoise;
                            float depth = waterLevel - perturbHeight;
                            if (depth > 0.0)
                            {
                                float waterBlend = saturate(depth * 100.0);
                                lavaMask *= (1.0 - waterBlend);
                            }
                        }
                    }

                    if (lavaMask > 0.001)
                    {
                        float2 uvFlow1 = uv * 8.0 + float2(_Time.y * 0.15, _Time.y * 0.08);
                        float2 uvFlow2 = uv * 8.0 - float2(_Time.y * 0.1, _Time.y * 0.12);
                        float n1 = fbm(uvFlow1);
                        float n2 = fbm(uvFlow2);
                        float flow = smoothstep(0.3, 0.7, (n1 + n2) * 0.5);

                        float3 lavaBaseColor = float3(0.75, 0.02, 0.0); // deep dark glowing red
                        float3 lavaHotColor = float3(1.0, 0.28, 0.0);   // vibrant fiery red-orange
                        float3 currentLavaColor = lerp(lavaBaseColor, lavaHotColor, flow);

                        color.rgb += currentLavaColor * lavaMask * _LavaEmissionStrength;
                    }
                }

                color.rgb = MixFog(color.rgb, input.fogCoord);
                color.a = 1;
                return color;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag

            #pragma shader_feature_local _DISPLACE
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct ShadowAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct ShadowVaryings
            {
                float4 positionCS : SV_POSITION;
            };

            ShadowVaryings ShadowVert(ShadowAttributes input)
            {
                ShadowVaryings output;

                float3 positionOS = input.positionOS.xyz;
                float3 dirOS = normalize(positionOS);

                #ifdef _DISPLACE
                    positionOS += dirOS * (SampleHeight(input.uv) * _DisplaceScale);
                #endif

                float3 positionWS = TransformObjectToWorld(positionOS);
                float3 normalWS = TransformObjectToWorldNormal(dirOS);

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));

                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                output.positionCS = positionCS;
                return output;
            }

            half4 ShadowFrag(ShadowVaryings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag

            #pragma shader_feature_local _DISPLACE

            struct DepthAttributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct DepthVaryings
            {
                float4 positionCS : SV_POSITION;
            };

            DepthVaryings DepthVert(DepthAttributes input)
            {
                DepthVaryings output;

                float3 positionOS = input.positionOS.xyz;

                #ifdef _DISPLACE
                    positionOS += normalize(positionOS) * (SampleHeight(input.uv) * _DisplaceScale);
                #endif

                output.positionCS = TransformObjectToHClip(positionOS);
                return output;
            }

            half4 DepthFrag(DepthVaryings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
