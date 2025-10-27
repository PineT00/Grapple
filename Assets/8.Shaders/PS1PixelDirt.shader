Shader "Custom/PS1PixelDirt"
{
    Properties
    {
        [Header(Base Settings)]
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _BaseMap ("Palette Color Map", 2D) = "white" {}
        
        [Header(Pixelation)]
        _PixelDensity ("Pixel Density", Range(16, 512)) = 128
        
        [Header(Dirt Settings)]
        _DirtDensity ("Dirt Density", Range(0, 1)) = 0.2
        _DirtDarkness ("Dirt Darkness", Range(0, 1)) = 0.35
        _NoiseScale ("Noise Scale", Range(0.1, 10)) = 2.0
        _DirtSize ("Dirt Pixel Size", Range(1, 3)) = 1.0
        
        [Header(Color Grading)]
        _ColorSteps ("Color Banding Steps", Range(4, 64)) = 16
        _HueShiftStrength ("Hue Shift Strength", Range(0, 0.2)) = 0.05
        
        [Header(Triplanar)]
        _TriplanarBlend ("Triplanar Blend Sharpness", Range(1, 10)) = 4
        
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
            Name "PS1PixelDirt"
            Tags { "LightMode" = "UniversalForward" }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
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
                float fogFactor : TEXCOORD3;
            };
            
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseMap_ST;
                float _PixelDensity;
                float _DirtDensity;
                float _DirtDarkness;
                float _NoiseScale;
                float _DirtSize;
                float _ColorSteps;
                float _HueShiftStrength;
                float _TriplanarBlend;
                float _UseEdges;
                float _EdgeDarkness;
                float _EdgeThreshold;
            CBUFFER_END
            
            // Hash function for pseudo-random noise
            float hash(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.13);
                p3 += dot(p3, p3.yzx + 3.333);
                return frac((p3.x + p3.y) * p3.z);
            }
            
            // Value noise
            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f); // Smoothstep
                
                float a = hash(i);
                float b = hash(i + float2(1.0, 0.0));
                float c = hash(i + float2(0.0, 1.0));
                float d = hash(i + float2(1.0, 1.0));
                
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
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
            
            // Color posterization (banding)
            float3 posterize(float3 color, float steps)
            {
                return floor(color * steps) / steps;
            }
            
            // Triplanar mapping
            float triplanarNoise(float3 worldPos, float3 normal, float scale)
            {
                // Project onto 3 planes
                float2 uvX = worldPos.zy * scale;
                float2 uvY = worldPos.xz * scale;
                float2 uvZ = worldPos.xy * scale;
                
                // Get noise for each plane
                float noiseX = noise(uvX);
                float noiseY = noise(uvY);
                float noiseZ = noise(uvZ);
                
                // Blend weights based on normal
                float3 blend = abs(normal);
                blend = pow(blend, _TriplanarBlend);
                blend /= dot(blend, 1.0);
                
                return noiseX * blend.x + noiseY * blend.y + noiseZ * blend.z;
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
                
                output.fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                // Sample base texture
                half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                
                // === PIXELATION ===
                // Pixelate world position for consistent pixel size
                float3 pixelatedPos = floor(input.positionWS * _PixelDensity) / _PixelDensity;
                
                // === TRIPLANAR DIRT GENERATION ===
                float noiseValue = triplanarNoise(pixelatedPos, normalize(input.normalWS), _NoiseScale);
                
                // Add multi-octave noise for more organic look
                noiseValue += triplanarNoise(pixelatedPos, normalize(input.normalWS), _NoiseScale * 2.3) * 0.5;
                noiseValue /= 1.5;
                
                // Dirt mask based on noise threshold
                float dirtMask = step(1.0 - _DirtDensity, noiseValue);
                
                // === COLOR PROCESSING ===
                float3 finalColor = baseColor.rgb;
                
                // Apply dirt darkening
                float3 hsv = rgb2hsv(finalColor);
                float darkenAmount = dirtMask * _DirtDarkness;
                hsv.z *= (1.0 - darkenAmount); // Reduce value (brightness)
                
                // Subtle hue shift based on brightness (warm highlights, cool shadows)
                float hueShift = (hsv.z - 0.5) * _HueShiftStrength;
                hsv.x += hueShift;
                
                finalColor = hsv2rgb(hsv);
                
                // === COLOR BANDING (Posterization) ===
                finalColor = posterize(finalColor, _ColorSteps);
                
                // === EDGE DARKENING (Optional) ===
                if (_UseEdges > 0.5)
                {
                    // Detect edges using normal derivatives
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
