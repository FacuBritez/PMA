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
    [Header("Movimiento vertical")]
    public float amplitudVertical = 0.5f;
    public float velocidadVertical = 1f;
    private float baseY;
    private float tiempoOffset;
    public float limiteVertical = 3f;

    private Quaternion rotBaseFija = Quaternion.Euler(90f, 0f, 0f);

    void Start()
    {
        leftLimit = -limitePantalla;
        rightLimit = limitePantalla;
        baseY = Random.Range(-4f, 4f);
        tiempoOffset = Random.Range(0f, Mathf.PI * 2);
        transform.position = new Vector3(leftLimit, baseY, transform.position.z);
        movingRight = true;
    }

    void Update()
    {
        if (movingRight)
        {
            transform.Translate(Vector3.right * speed * Time.deltaTime, Space.World);
            if (transform.position.x > rightLimit)
            {
                movingRight = false;
                Invertir();
                ElegirNuevaAlturaBase();
            }
        }
        else
        {
            transform.Translate(Vector3.left * speed * Time.deltaTime, Space.World);
            if (transform.position.x < leftLimit)
            {
                movingRight = true;
                Invertir();
                ElegirNuevaAlturaBase();
            }
        }

        float angulo = Mathf.Sin(Time.time * velocidadRotacion) * amplitudRotacion;
        Quaternion rotBalanceo = Quaternion.Euler(0f, 0f, angulo);
        transform.rotation = rotBaseFija * rotBalanceo;

        float newY = baseY + Mathf.Sin(Time.time * velocidadVertical + tiempoOffset) * amplitudVertical;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    void Invertir()
    {
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    void ElegirNuevaAlturaBase()
    {
        baseY = Random.Range(-limiteVertical, limiteVertical);
        tiempoOffset = Random.Range(0f, Mathf.PI * 2f);
    }
}