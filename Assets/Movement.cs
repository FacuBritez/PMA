using UnityEngine;

public class Movement : MonoBehaviour
{
    public float speed = 2f;

    [Header("Rotacion opciones")]
    public float amplitudRotacion = 10f;
    public float velocidadRotacion = 5f;
    private bool movingRight = true;

    [Header("Limites pantalla")]
    public float limitePantalla = 20f;
    private float leftLimit;
    private float rightLimit;

    [Header("Centro del plano")]
    public Vector2 centro = Vector2.zero;

    [Header("Direccion inicial (1 = derecha, -1 = izquierda)")]
    public int direccionInicial = 1;

    [Header("Movimiento vertical")]
    public float amplitudVertical = 0.5f;
    public float velocidadVertical = 1f;
    private float baseY;
    private float tiempoOffset;
    public float limiteVertical = 3f;

    // Guardamos el tamaño absoluto para no perderlo al flipear
    private float scaleAbsX;
    private float scaleAbsY;

    void Start()
    {
        leftLimit  = centro.x - limitePantalla;
        rightLimit = centro.x + limitePantalla;

        // El loader ya seteó localScale con el signo de la dirección inicial.
        // Guardamos los valores absolutos.
        scaleAbsX = Mathf.Abs(transform.localScale.x);
        scaleAbsY = Mathf.Abs(transform.localScale.y);

        movingRight = direccionInicial >= 0;
        ApplyFlip();

        float rangoY = Mathf.Max(0f, limiteVertical - amplitudVertical);
        baseY = Mathf.Clamp(transform.position.y, centro.y - rangoY, centro.y + rangoY);

        tiempoOffset = Random.Range(0f, Mathf.PI * 2f);
    }

    void Update()
    {
        if (movingRight)
        {
            transform.Translate(Vector3.right * speed * Time.deltaTime, Space.World);
            if (transform.position.x > rightLimit)
            {
                movingRight = false;
                ApplyFlip();
                ElegirNuevaAlturaBase();
            }
        }
        else
        {
            transform.Translate(Vector3.left * speed * Time.deltaTime, Space.World);
            if (transform.position.x < leftLimit)
            {
                movingRight = true;
                ApplyFlip();
                ElegirNuevaAlturaBase();
            }
        }

        float angulo = Mathf.Sin(Time.time * velocidadRotacion) * amplitudRotacion;
        Quaternion rotBalanceo = Quaternion.Euler(0f, 0f, angulo);
        transform.rotation = rotBaseFija * rotBalanceo;

        float newY = baseY + Mathf.Sin(Time.time * velocidadVertical + tiempoOffset) * amplitudVertical;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    /// <summary>
    /// Flip por escala. movingRight=true → escala X positiva (sprite mira a la derecha).
    /// movingRight=false → escala X negativa (sprite mira a la izquierda).
    /// </summary>
    void ApplyFlip()
    {
        float signX = movingRight ? -1f : 1f;
        transform.localScale = new Vector3(scaleAbsX * signX, scaleAbsY, 1f);
    }

    void ElegirNuevaAlturaBase()
    {
        float rangoY = Mathf.Max(0f, limiteVertical - amplitudVertical);
        baseY = centro.y + Random.Range(-rangoY, rangoY);
        tiempoOffset = Random.Range(0f, Mathf.PI * 2f);
    }
}