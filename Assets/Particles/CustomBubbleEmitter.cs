using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class CustomBubbleEmitter : MonoBehaviour
{
    [Header("Emission Settings")]
    [Tooltip("Cantidad de burbujas por segundo.")]
    [SerializeField] private float emissionRate = 10f;
    
    [Header("Size Settings")]
    [Tooltip("Tamaño mínimo base de la burbuja.")]
    [SerializeField] private float minBaseSize = 0.1f;
    [Tooltip("Tamaño máximo base de la burbuja.")]
    [SerializeField] private float maxBaseSize = 0.5f;

    private ParticleSystem particleSystemComponent;
    private ParticleSystem.ShapeModule shapeModule;
    private ParticleSystem.EmissionModule emissionModule;
    private ParticleSystem.MainModule mainModule;
    private ParticleSystem.SizeOverLifetimeModule sizeOverLifetimeModule;

    private void Awake()
    {
        InitializeComponents();
        ConfigureParticleSystem();
    }

    private void OnValidate()
    {
        if (Application.isPlaying && particleSystemComponent != null)
        {
            UpdateEmissionRate();
        }
    }

    private void InitializeComponents()
    {
        particleSystemComponent = GetComponent<ParticleSystem>();
        shapeModule = particleSystemComponent.shape;
        emissionModule = particleSystemComponent.emission;
        mainModule = particleSystemComponent.main;
        sizeOverLifetimeModule = particleSystemComponent.sizeOverLifetime;
    }

    private void ConfigureParticleSystem()
    {
        // 1. Configuración de la forma basada en el plano asignado
        shapeModule.enabled = true;
        shapeModule.shapeType = ParticleSystemShapeType.Box;
        
        // Obtener la escala del plano para ajustar el emisor
        Vector3 parentScale = transform.localScale;
        shapeModule.scale = new Vector3(parentScale.x * 10f, 0.1f, parentScale.z * 10f);

        // 2. Configuración de la emisión dinámica
        UpdateEmissionRate();

        // 3. Configuración del ciclo de vida y tamaño base aleatorio
        mainModule.startSize3D = false;
        mainModule.startSizeMultiplier = 1f;
        
        // Curva aleatoria entre dos constantes para el tamaño inicial base
        ParticleSystem.MinMaxCurve sizeCurve = new ParticleSystem.MinMaxCurve(minBaseSize, maxBaseSize);
        mainModule.startSize = sizeCurve;

        // 4. Curva de crecimiento: 0% -> 100% (Base) -> 250% (150% de crecimiento adicional)
        sizeOverLifetimeModule.enabled = true;
        
        AnimationCurve growthCurve = new AnimationCurve();
        growthCurve.AddKey(0.0f, 0.0f);     // Inicia en tamaño 0
        growthCurve.AddKey(0.3f, 1.0f);     // Alcanza el tamaño aleatorio base rápido
        growthCurve.AddKey(1.0f, 2.5f);     // Crece un 150% extra (Tamaño base * 2.5)

        sizeOverLifetimeModule.size = new ParticleSystem.MinMaxCurve(1f, growthCurve);
    }

    private void UpdateEmissionRate()
    {
        ParticleSystem.MinMaxCurve rate = new ParticleSystem.MinMaxCurve(emissionRate);
        emissionModule.rateOverTime = rate;
    }
}