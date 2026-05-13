Shader "Custom/SpriteWave"
{
    Properties
    {
        [MainTexture] _MainTex ("Sprite Texture", 2D) = "white" {}
        _WaveSpeed ("Wave Speed", Float) = 2.0
        _WaveFreq ("Wave Frequency", Float) = 10.0
        _WaveAmp ("Wave Amplitude", Float) = 0.05
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
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
               
                // Calculamos la ondulación basada en la posición X y el tiempo
                // Usamos input.uv.x para que la cola se mueva más que la cabeza si fuera necesario
                float wave = sin(_Time.y * _WaveSpeed + (input.positionOS.x * _WaveFreq)) * _WaveAmp;
               
                // Aplicamos el desplazamiento al eje Y (u horizontal si el pez nada hacia arriba)
                input.positionOS.y += wave;

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                return texColor * input.color;
            }
            ENDHLSL
        }
    }
}