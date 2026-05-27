Shader "Custom/SpriteGlowWithKeyingSimplified"
{
    Properties
    {
        [MainTexture] _MainTex ("Sprite Texture", 2D) = "white" {}
        [HDR] _GlowColor("Glow Color (HDR)", Color) = (2, 2, 2, 1)
        
        [Header(Descarte de Blancos y Grises)]
        _CutoffThreshold ("Cutoff Threshold", Range(0.0, 1.0)) = 0.459
        _Feather ("Feather Smoothness", Range(0.001, 0.5)) = 0.169

        [Header(Configuracion del Glow)]
        _MaxGlowWidth("Radio Maximo del Brillo", Range(0.0, 0.2)) = 0.05
        _GlowThreshold("Umbral de Borde del Glow", Range(0.0, 1.0)) = 0.1
        
        [Header(Control de Tiempos Simplificado)]
        _FadeInDuration("1. Fade In (Antes de encender)", Range(0.0, 5.0)) = 0.5
        _GlowOnDuration("2. Glow ON (Brillo Maximo Fijo)", Range(0.0, 10.0)) = 2.0
        _FadeOutDuration("3. Fade Out (Despues de encender)", Range(0.0, 5.0)) = 0.5
        _GlowOffDuration("4. Glow OFF (Tiempo Apagado)", Range(0.0, 20.0)) = 4.0

        [Header(Suavizado de Curvas (Slowness))]
        _FadeInSlowdown("Ralentizar Encendido (Exponente)", Range(1.0, 5.0)) = 2.0
        _FadeOutSlowdown("Ralentizar Apagado (Exponente)", Range(1.0, 5.0)) = 2.0
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
                float _CutoffThreshold;
                float _Feather;
                float4 _GlowColor;
                float _MaxGlowWidth;
                float _GlowThreshold;
                float _FadeInDuration;
                float _GlowOnDuration;
                float _FadeOutDuration;
                float _GlowOffDuration;
                float _FadeInSlowdown;
                float _FadeOutSlowdown;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half GetObjectSilhouette(float2 uvCoords)
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvCoords);
                half luma = dot(tex.rgb, half3(0.2126, 0.7152, 0.0722));
                return 1.0 - smoothstep(_CutoffThreshold - _Feather, _CutoffThreshold, luma);
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half currentSilhouette = GetObjectSilhouette(input.uv);
                texColor.a = currentSilhouette;

                // 1. CALCULO AUTOMÁTICO DEL CICLO TOTAL
                // La suma de todas tus variables define la duración completa del bucle
                float totalCycleTime = _FadeInDuration + _GlowOnDuration + _FadeOutDuration + _GlowOffDuration;
                
                // Reloj por módulo protegido
                float currentTime = fmod(_Time.y, max(0.1, totalCycleTime));

                // 2. HITOS SECUENCIALES DE LA LÍNEA DE TIEMPO
                float endFadeIn = _FadeInDuration;
                float endGlowOn = endFadeIn + _GlowOnDuration;
                float endFadeOut = endGlowOn + _FadeOutDuration;

                float timeMask = 0.0;

                // Fase 1: FADE IN (Antes del Glow ON)
                if (currentTime < endFadeIn)
                {
                    float progress = saturate(currentTime / max(0.01, _FadeInDuration));
                    timeMask = pow(progress, _FadeInSlowdown);
                }
                // Fase 2: GLOW ON (Brillo al Máximo)
                else if (currentTime >= endFadeIn && currentTime < endGlowOn)
                {
                    timeMask = 1.0;
                }
                // Fase 3: FADE OUT (Después del Glow ON)
                else if (currentTime >= endGlowOn && currentTime < endFadeOut)
                {
                    float progress = saturate((currentTime - endGlowOn) / max(0.01, _FadeOutDuration));
                    timeMask = 1.0 - pow(progress, _FadeOutSlowdown);
                }
                // Fase 4: GLOW OFF (Inactividad/Apagado total)
                else
                {
                    timeMask = 0.0;
                }

                // 3. MUESTREO RADIAL DE 8 DIRECCIONES
                float currentWidth = _MaxGlowWidth * timeMask;

                float2 dirUp    = float2(0.0, 1.0) * currentWidth;
                float2 dirDown  = float2(0.0, -1.0) * currentWidth;
                float2 dirRight = float2(1.0, 0.0) * currentWidth;
                float2 dirLeft  = float2(-1.0, 0.0) * currentWidth;
                
                float2 dirUR    = float2(0.7071, 0.7071) * currentWidth;
                float2 dirUL    = float2(-0.7071, 0.7071) * currentWidth;
                float2 dirDR    = float2(0.7071, -0.7071) * currentWidth;
                float2 dirDL    = float2(-0.7071, -0.7071) * currentWidth;

                half a1 = GetObjectSilhouette(input.uv + dirUp);
                half a2 = GetObjectSilhouette(input.uv + dirDown);
                half a3 = GetObjectSilhouette(input.uv + dirRight);
                half a4 = GetObjectSilhouette(input.uv + dirLeft);
                half a5 = GetObjectSilhouette(input.uv + dirUR);
                half a6 = GetObjectSilhouette(input.uv + dirUL);
                half a7 = GetObjectSilhouette(input.uv + dirDR);
                half a8 = GetObjectSilhouette(input.uv + dirDL);

                half glowMask = (a1 + a2 + a3 + a4 + a5 + a6 + a7 + a8) * 0.125;
                glowMask = saturate(glowMask - currentSilhouette);
                glowMask = smoothstep(_GlowThreshold, 1.0, glowMask);

                // 4. MEZCLA FINAL
                half3 dynamicGlowColor = _GlowColor.rgb * timeMask;
                half3 finalRGB = texColor.rgb + (dynamicGlowColor * glowMask);
                half finalAlpha = saturate(texColor.a + (glowMask * timeMask));

                return half4(finalRGB, finalAlpha) * input.color;
            }
            ENDHLSL
        }
    }
}