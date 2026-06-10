Shader "Custom/SpriteWaveWithKeying"
{
    Properties
    {
        [MainTexture] _MainTex ("Sprite Texture", 2D) = "white" {}
        _WaveSpeed ("Wave Speed", Float) = 4
        _WaveFreq ("Wave Frequency", Float) = 0.5
        _WaveAmp ("Wave Amplitude", Float) = 1
        
        // Nuevas propiedades para controlar el descarte de blancos/grises
        _CutoffThreshold ("Cutoff Threshold", Range(0.0, 1.0)) = 0.459
        _Feather ("Feather Smoothness", Range(0.001, 0.5)) = 0.169
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
                float _WaveSpeed;
                float _WaveFreq;
                float _WaveAmp;
                float _CutoffThreshold;
                float _Feather;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                // Algoritmo de deformación geométrica (idéntico al anterior)
                float wave = sin(_Time.z * _WaveSpeed + (input.positionOS.x * _WaveFreq)) * _WaveAmp;
                input.positionOS.z += wave;

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 1. Muestrear el color original de la textura
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                
                // 2. Calcular la luminancia (brillo percibido por el ojo humano)
                // Se usan los pesos estándar del espectro para los canales R, G y B
                half luminance = dot(texColor.rgb, half3(0.2126, 0.7152, 0.0722));
                
                // 3. Crear una máscara suave usando smoothstep
                // Si la luminancia es baja, devuelve 1 (mantiene el alfa original).
                // A medida que se acerca al umbral blanco/gris, decae suavemente a 0.
                half alphaMask = 1.0 - smoothstep(_CutoffThreshold - _Feather, _CutoffThreshold, luminance);
                
                // 4. Multiplicar el alfa del píxel por la máscara calculada
                texColor.a *= alphaMask;
                
                // 5. Aplicar el color del componente SpriteRenderer
                return texColor * input.color;
            }
            ENDHLSL
        }
    }
}