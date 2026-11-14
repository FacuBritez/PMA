using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class AspectRatioController : MonoBehaviour
{
    [Header("Aspect Ratio Target")]
    [Tooltip("Relación de aspecto objetivo. 16:3 (16/3 = 5.333333f)")]
    public float targetAspect = 16f / 3f;

    [Tooltip("Si es true, ajusta el viewport de la cámara con letterbox o pillarbox si la pantalla o GameView tiene otra resolución.")]
    public bool constrainCameraViewport = true;

    private Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
        ApplyAspect();
    }

    void Start()
    {
        ApplyAspect();
    }

    void Update()
    {
        #if UNITY_EDITOR
        ApplyAspect();
        #endif
    }

    public void ApplyAspect()
    {
        if (cam == null) cam = GetComponent<Camera>();
        if (cam == null) return;

        if (constrainCameraViewport)
        {
            float currentAspect = (float)Screen.width / Screen.height;
            float scaleHeight = currentAspect / targetAspect;

            if (scaleHeight < 1.0f)
            {
                // La pantalla actual es más alta que 16:3 (ej. monitor estándar 16:9) -> Letterbox arriba y abajo
                Rect rect = cam.rect;
                rect.width = 1.0f;
                rect.height = scaleHeight;
                rect.x = 0;
                rect.y = (1.0f - scaleHeight) / 2.0f;
                cam.rect = rect;
            }
            else
            {
                // La pantalla actual es más ancha que 16:3 -> Pillarbox a los lados
                float scaleWidth = 1.0f / scaleHeight;
                Rect rect = cam.rect;
                rect.width = scaleWidth;
                rect.height = 1.0f;
                rect.x = (1.0f - scaleWidth) / 2.0f;
                rect.y = 0;
                cam.rect = rect;
            }
        }
        else
        {
            cam.rect = new Rect(0, 0, 1, 1);
            cam.aspect = targetAspect;
        }
    }
}
