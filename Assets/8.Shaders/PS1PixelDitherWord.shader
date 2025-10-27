Shader "Custom/PS1PixelDitherWorld"
{
    Properties
    {
        [Header(Base Settings)]
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _BaseMap ("Palette Color Map", 2D) = "white" {}
        
        [Header(Pixelation)]
        _PixelDensity ("Pixel Density", Range(16, 512)) = 128
        
        [Header(Dithering)]
        _DitherStrength ("Dither Strength", Range(0, 1)) = 0.5
        _DitherScale ("Dither Pattern Scale", Range(0.5, 4)) = 1.0
        [KeywordEnum(Bayer4, Bayer8, Bayer16)] _DitherPattern ("Dither Pattern", Float) = 1
        
        [Header(Color Grading)]
        _ColorSteps ("Color Banding Steps", Range(4, 64)) = 16
        _ShadowTint ("Shadow Tint", Color) = (0.7, 0.75, 0.85, 1)
        _HighlightTint ("Highlight Tint", Color) = (1.0, 0.98, 0.9, 1)
        _TintStrength ("Tint Strength", Range(0, 0.3)) = 0.08
        
        [Header(Edge Highlighting)]
        [Toggle] _UseEdges ("Use Edge Darkening", Float) = 0
        _EdgeDarkness ("Edge Darkness", Range(0, 1)) = 0.2
        _EdgeThreshold ("Edge Threshold", Range(0.01, 0.5)) = 0.1
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }
        
        Pass
        {
            Name "PS1PixelDither"
            Tags { "LightMode" = "UniversalForward" }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma shader_feature _DITHERPATTERN_BAYER4 _DITHERPATTERN_BAYER8 _DITHERPATTERN_BAYER16
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float4 shadowCoord : TEXCOORD3;
                float fogFactor : TEXCOORD4;
            };
            
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseMap_ST;
                float _PixelDensity;
                float _DitherStrength;
                float _DitherScale;
                float _ColorSteps;
                float4 _ShadowTint;
                float4 _HighlightTint;
                float _TintStrength;
                float _UseEdges;
                float _EdgeDarkness;
                float _EdgeThreshold;
            CBUFFER_END
            
            // Bayer matrix 4x4
            static const float bayer4[16] = {
                0.0/16.0,  8.0/16.0,  2.0/16.0, 10.0/16.0,
                12.0/16.0, 4.0/16.0, 14.0/16.0,  6.0/16.0,
                3.0/16.0, 11.0/16.0,  1.0/16.0,  9.0/16.0,
                15.0/16.0, 7.0/16.0, 13.0/16.0,  5.0/16.0
            };
            
            // Bayer matrix 8x8
            static const float bayer8[64] = {
                0.0/64.0, 32.0/64.0,  8.0/64.0, 40.0/64.0,  2.0/64.0, 34.0/64.0, 10.0/64.0, 42.0/64.0,
                48.0/64.0, 16.0/64.0, 56.0/64.0, 24.0/64.0, 50.0/64.0, 18.0/64.0, 58.0/64.0, 26.0/64.0,
                12.0/64.0, 44.0/64.0,  4.0/64.0, 36.0/64.0, 14.0/64.0, 46.0/64.0,  6.0/64.0, 38.0/64.0,
                60.0/64.0, 28.0/64.0, 52.0/64.0, 20.0/64.0, 62.0/64.0, 30.0/64.0, 54.0/64.0, 22.0/64.0,
                3.0/64.0, 35.0/64.0, 11.0/64.0, 43.0/64.0,  1.0/64.0, 33.0/64.0,  9.0/64.0, 41.0/64.0,
                51.0/64.0, 19.0/64.0, 59.0/64.0, 27.0/64.0, 49.0/64.0, 17.0/64.0, 57.0/64.0, 25.0/64.0,
                15.0/64.0, 47.0/64.0,  7.0/64.0, 39.0/64.0, 13.0/64.0, 45.0/64.0,  5.0/64.0, 37.0/64.0,
                63.0/64.0, 31.0/64.0, 55.0/64.0, 23.0/64.0, 61.0/64.0, 29.0/64.0, 53.0/64.0, 21.0/64.0
            };
            
            // Get dither threshold value
            float getDitherThreshold(float2 screenPos, float scale)
            {
                float2 ditherCoord = screenPos * scale;
                
                #if defined(_DITHERPATTERN_BAYER4)
                    int2 coord = int2(ditherCoord) % 4;
                    return bayer4[coord.y * 4 + coord.x];
                #elif defined(_DITHERPATTERN_BAYER16)
                    // 16x16 is made by tiling 4x4
                    int2 coord = int2(ditherCoord) % 4;
                    float base = bayer4[coord.y * 4 + coord.x];
                    int2 offset = (int2(ditherCoord) / 4) % 4;
                    float subPattern = bayer4[offset.y * 4 + offset.x] * 0.0625; // 1/16
                    return base + subPattern;
                #else // BAYER8
                    int2 coord = int2(ditherCoord) % 8;
                    return bayer8[coord.y * 8 + coord.x];
                #endif
            }
            
            // RGB to HSV conversion
            float3 rgb2hsv(float3 c)
            {
                float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
                float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
                float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));
                
                float d = q.x - min(q.w, q.y);
                float e = 1.0e-10;
                return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
            }
            
            // HSV to RGB conversion
            float3 hsv2rgb(float3 c)
            {
                float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
                float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
                return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
            }
            
            // Color posterization
            float3 posterize(float3 color, float steps)
            {
                return floor(color * steps) / steps;
            }
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);
                output.normalWS = normalInput.normalWS;
                
                // Calculate shadow coordinates
                output.shadowCoord = GetShadowCoord(vertexInput);
                
                output.fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                // Sample base texture
                half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                
                // === PIXELATION (World-Space based) ===
                float3 pixelatedPos = floor(input.positionWS * _PixelDensity) / _PixelDensity;
                
                // Triplanar world-space coordinates for dithering
                float3 worldPos = input.positionWS;
                float3 normal = normalize(input.normalWS);
                float3 blend = abs(normal);
                blend = pow(blend, 4.0);
                blend /= dot(blend, 1.0);
                
                // Get dither coordinates from triplanar projection
                float2 ditherCoordX = worldPos.zy * _PixelDensity * _DitherScale;
                float2 ditherCoordY = worldPos.xz * _PixelDensity * _DitherScale;
                float2 ditherCoordZ = worldPos.xy * _PixelDensity * _DitherScale;
                
                float2 ditherCoord = ditherCoordX * blend.x + ditherCoordY * blend.y + ditherCoordZ * blend.z;
                
                // === REAL LIGHTING ===
                // Prepare lighting input data
                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = normalize(input.normalWS);
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.shadowCoord = input.shadowCoord;
                inputData.fogCoord = input.fogFactor;
                
                // Get main light
                Light mainLight = GetMainLight(inputData.shadowCoord);
                
                // Simple Lambert lighting
                float NdotL = saturate(dot(inputData.normalWS, mainLight.direction));
                float3 lighting = mainLight.color * (mainLight.shadowAttenuation * NdotL);
                
                // Add ambient
                lighting += SampleSH(inputData.normalWS);
                
                // Additional lights
                #ifdef _ADDITIONAL_LIGHTS
                uint pixelLightCount = GetAdditionalLightsCount();
                for (uint lightIndex = 0u; lightIndex < pixelLightCount; ++lightIndex)
                {
                    Light light = GetAdditionalLight(lightIndex, inputData.positionWS);
                    float NdotL2 = saturate(dot(inputData.normalWS, light.direction));
                    lighting += light.color * (light.shadowAttenuation * light.distanceAttenuation * NdotL2);
                }
                #endif
                
                // === COLOR PROCESSING ===
                float3 workingColor = baseColor.rgb * lighting;
                
                // Convert to HSV for luminance
                float3 hsv = rgb2hsv(workingColor);
                float luminance = hsv.z;
                
                // === DITHERING ===
                float ditherThreshold = getDitherThreshold(ditherCoord, 1.0); // Scale already applied above
                
                // Apply dithering to luminance
                float ditheredLuminance = luminance;
                float ditherStep = 1.0 / _ColorSteps;
                
                // Quantize with dithering
                float quantized = floor(luminance / ditherStep) * ditherStep;
                float nextStep = quantized + ditherStep;
                float lerpFactor = (luminance - quantized) / ditherStep;
                
                // Dither decision
                if (lerpFactor > ditherThreshold)
                {
                    ditheredLuminance = nextStep;
                }
                else
                {
                    ditheredLuminance = quantized;
                }
                
                // Blend between dithered and original based on strength
                hsv.z = lerp(luminance, ditheredLuminance, _DitherStrength);
                
                float3 finalColor = hsv2rgb(hsv);
                
                // === COLOR BANDING ===
                finalColor = posterize(finalColor, _ColorSteps);
                
                // === COLOR TINTING ===
                // Warm highlights, cool shadows
                float3 tintColor = lerp(_ShadowTint.rgb, _HighlightTint.rgb, hsv.z);
                finalColor = lerp(finalColor, finalColor * tintColor, _TintStrength);
                
                // === EDGE DARKENING ===
                if (_UseEdges > 0.5)
                {
                    float3 normalDDX = ddx(input.normalWS);
                    float3 normalDDY = ddy(input.normalWS);
                    float edgeFactor = length(normalDDX) + length(normalDDY);
                    
                    float edgeMask = smoothstep(0.0, _EdgeThreshold, edgeFactor);
                    finalColor *= (1.0 - edgeMask * _EdgeDarkness);
                }
                
                // === FINAL OUTPUT ===
                half4 color = half4(finalColor, baseColor.a);
                
                // Apply fog
                color.rgb = MixFog(color.rgb, input.fogFactor);
                
                return color;
            }
            ENDHLSL
        }
    }
    
    FallBack "Universal Render Pipeline/Unlit"
}
