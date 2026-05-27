Shader "Custom/sacarBlanco"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Threshold ("Umbral de Blanco", Range(0, 1)) = 0.9
        _Softness ("Suavizado", Range(0, 0.5)) = 0.1
    }
    
    SubShader
    {
        // Importante: Tag para permitir transparencia
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        // Configuración de mezcla (Blending) para el Alpha
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR; // Soporte para el color del SpriteRenderer
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
            };

            sampler2D _MainTex;
            float _Threshold;
            float _Softness;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Leemos el color original de la textura
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;

                // Calculamos el promedio de los canales RGB para medir la intensidad
                // También puedes usar: dot(col.rgb, float3(0.299, 0.587, 0.114)) para luminancia real
                float brightness = (col.r + col.g + col.b) / 3.0;

                // Creamos una máscara basada en el umbral
                // Si la intensidad es mayor al umbral, el alpha será 0
                float alphaMask = smoothstep(_Threshold, _Threshold - _Softness, brightness);

                // Aplicamos la máscara al canal alpha original
                col.a *= alphaMask;

                // Descartamos el píxel si el alpha es muy bajo (optimización)
                clip(col.a - 0.01);

                return col;
            }
            ENDCG
        }
    }
}