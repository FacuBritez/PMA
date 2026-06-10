Shader "Custom/SpriteGlowContourOptimized"
{
    Properties
    {
        [MainTexture] _MainTex ("Sprite Texture", 2D) = "white" {}
        [HDR] _GlowColor("Glow Color (HDR)", Color) = (2, 2, 2, 1)
        
        [Header(Umbral de la Silueta Alfa)]
        _AlphaThreshold ("Alpha Threshold", Range(0.0, 0.9)) = 0.9

        [Header(Configuracion del Glow de Contorno)]
        _MaxGlowWidth("Radio Maximo del Brillo", Range(0.0, 0.05)) = 0.0233
        
        [Header(Control de Tiempos)]
        _FadeInDuration("1. Fade In", Range(0.0, 5.0)) = 2.21
        _GlowOnDuration("2. Glow ON", Range(0.0, 10.0)) = 3.72
        _FadeOutDuration("3. Fade Out", Range(0.0, 5.0)) = 2.24
        _GlowOffDuration("4. Glow OFF", Range(0.0, 20.0)) = 4
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float _AlphaThreshold;
                float4 _GlowColor;
                float _MaxGlowWidth;
                float _FadeInDuration;
                float _GlowOnDuration;
                float _FadeOutDuration;
                float _GlowOffDuration;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            // Muestreo ultra-rápido de un punto alfa
            half SampleAlpha(float2 uv)
            {
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a > _AlphaThreshold ? 1.0 : 0.0;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 1. OBTENER EL TIEMPO EN LÍNEA DE TIEMPO SIN CONDICIONALES "IF"
                float totalCycleTime = _FadeInDuration + _GlowOnDuration + _FadeOutDuration + _GlowOffDuration;
                float currentTime = fmod(_Time.y, max(0.1, totalCycleTime));

                // Cálculo matemático de la máscara de tiempo (reemplaza los IFS para ejecución paralela en GPU)
                float step1 = step(currentTime, _FadeInDuration);
                float step2 = step(currentTime, _FadeInDuration + _GlowOnDuration);
                float step3 = step(currentTime, _FadeInDuration + _GlowOnDuration + _FadeOutDuration);

                float fadeInProg = saturate(currentTime / max(0.01, _FadeInDuration));
                float fadeOutProg = saturate((currentTime - (_FadeInDuration + _GlowOnDuration)) / max(0.01, _FadeOutDuration));

                float timeMask = step1 * (fadeInProg * fadeInProg) +
                                 (1.0 - step1) * step2 * 1.0 +
                                 (1.0 - step2) * step3 * (1.0 - (fadeOutProg * fadeOutProg));

                // 2. MUESTREO SIMPLIFICADO DE CONTORNO COMPLETO (Ahorra un 50% de muestras de textura)
                float currentWidth = _MaxGlowWidth * timeMask;
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half currentSilhouette = texColor.a > _AlphaThreshold ? 1.0 : 0.0;

                // Muestreo en cruz optimizado (suficiente para siluetas completas en texturas estándar)
                half outline = 0.0;
                outline += SampleAlpha(input.uv + float2(currentWidth, 0));
                outline += SampleAlpha(input.uv - float2(currentWidth, 0));
                outline += SampleAlpha(input.uv + float2(0, currentWidth));
                outline += SampleAlpha(input.uv - float2(0, currentWidth));
                
                // Convertimos el acumulado en una máscara sólida de contorno exterior
                half glowMask = saturate(outline) - currentSilhouette;

                // 3. MEZCLA FINAL EFICIENTE
                half3 finalRGB = texColor.rgb + (_GlowColor.rgb * glowMask * timeMask);
                half finalAlpha = saturate(texColor.a + (glowMask * timeMask));

                return half4(finalRGB, finalAlpha) * input.color;
            }
            ENDHLSL
        }
    }
}