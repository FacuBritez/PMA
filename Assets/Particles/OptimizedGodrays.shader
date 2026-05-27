Shader "Custom/OptimizedGodrays"
{
    Properties
    {
        [HDR] _RayColor ("Color Base del Rayo", Color) = (0, 0.4, 0.8, 1)
        [HDR] _SurfaceColor ("Color de la Superficie (Brillo)", Color) = (2, 2, 2, 1)
        _Power ("Concentración Lateral", Range(1, 5)) = 2.0
        _Falloff ("Concentración del Brillo Superior", Range(1, 8)) = 4.0
        
        // Nuevos parámetros para controlar la oscilación desde el Inspector sin tocar el código
        _AnimSpeed ("Velocidad de Oscilación", Range(0, 5)) = 1.5
        _AnimWidth ("Amplitud de Oscilación", Range(0, 0.5)) = 0.2
    }
    SubShader
    {
        // Forzamos explícitamente la cola a Transparent (3000) dentro del rango permitido
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 100

        ZWrite Off
        Blend One One  // Blend aditivo para la acumulación lumínica
        Cull Off       

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
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float4 _RayColor;
            float4 _SurfaceColor;
            float _Power;
            float _Falloff;
            
            // Declaración de variables de control en el pass de la GPU
            float _AnimSpeed;
            float _AnimWidth;

            v2f vert (appdata v)
            {
                v2f o;
                // Uso de la macro nativa de UnityCG.cginc para transformar coordenadas locales a clip space
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float verticalFade = i.uv.y;
                float surfaceMask = pow(i.uv.y, _Falloff);

                // CONTROL DE TIEMPO SINTÁCTICAMENTE CORRECTO:
                // _Time es un float4 nativo de Unity: (t/20, t, t*2, t*3). La componente .y es el tiempo real en segundos.
                float wave = sin(_Time.y * _AnimSpeed);
                
                // Variación del exponente. Si _Power es 2.0 y _AnimWidth es 0.2, dynamicPower oscila entre 1.8 y 2.2.
                float dynamicPower = _Power + (wave * _AnimWidth);

                float horizontalFade = 1.0 - abs((i.uv.x * 2.0) - 1.0);
                
                // Reemplazo del exponente estático _Power por la variable dinámica oscilante
                horizontalFade = pow(horizontalFade, dynamicPower);

                float4 blendedColor = lerp(_RayColor, _SurfaceColor, surfaceMask);
                float finalAlpha = verticalFade * horizontalFade;

                return blendedColor * finalAlpha;
            }
            ENDCG
        }
    }
}