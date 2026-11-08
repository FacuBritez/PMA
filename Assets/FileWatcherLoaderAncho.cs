using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class FileWatcherLoaderAncho : MonoBehaviour
{
    [Header("Carpeta de imágenes")]
    public string watchFolder = "Assets/WatchedImages";

    [Header("Shaders (arrastrar desde el proyecto)")]
    [Tooltip("Shader de onda + keying (uno solo hace ambos efectos).")]
    public Shader shaderOndulacion;

    [Header("Ondulación")]
    public float waveSpeed = 6f;
    public float waveFreq = 3f;
    [Tooltip("Amplitud en unidades de MUNDO (se compensa por escala automáticamente).")]
    public float waveAmp = 0.15f;
    [Range(0f, 0.9f)]
    public float waveRandomVariation = 0.25f;

    [Header("Keying (eliminar blanco)")]
    [Range(0f, 1f)] public float cutoffThreshold = 0.459f;
    [Range(0.001f, 0.5f)] public float feather = 0.169f;

    [Header("Planos de spawn")]
    [Tooltip("Arrastrá acá los 3 planos donde aparecerán los peces.")]
    public List<Renderer> planosSpawn = new List<Renderer>();

    [Header("Tamaño de spawn")]
    public float tamañoMin = 0.5f;
    public float tamañoMax = 2f;

    [Header("Ajustes de render")]
    public float zOffset = -0.1f;
    public int sortingOrder = 0;

    private FileSystemWatcher watcher;
    private string pendingImagePath;
    private readonly List<Renderer> zonasSpawn = new List<Renderer>();

    void Start()
    {
        Debug.Log("pantallas detectadas " + Display.displays.Length);
        for (int i = 1; i < Display.displays.Length; i++)
            Display.displays[i].Activate();

        Application.runInBackground = true;

        if (!Application.isEditor)
        {
            watchFolder = Path.Combine(Path.GetDirectoryName(Application.dataPath), "WatchedImages");
            Debug.Log("Juego exportado");
        }

        if (!Directory.Exists(watchFolder))
            Directory.CreateDirectory(watchFolder);

        RebuildZonas();

        Camera cam = Camera.main;
        if (cam != null && cam.orthographic)
            Debug.LogWarning("Cámara ORTOGRÁFICA: la onda en Z no será visible.");

        watcher = new FileSystemWatcher(watchFolder);
        watcher.Filter = "*.*";
        watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite;
        watcher.Created += OnImageAdded;
        watcher.EnableRaisingEvents = true;
    }

    // 👇 Botón en el Inspector (click derecho sobre el componente) para diagnosticar
    [ContextMenu("Rebuild Spawn Zones")]
    void RebuildZonas()
    {
        zonasSpawn.Clear();
        HashSet<Renderer> unicos = new HashSet<Renderer>();

        for (int i = 0; i < planosSpawn.Count; i++)
        {
            Renderer r = planosSpawn[i];
            if (r == null)
            {
                Debug.LogWarning($"planosSpawn[{i}] es NULL. Revisá el Inspector.");
                continue;
            }
            if (!unicos.Add(r))
            {
                Debug.LogWarning($"planosSpawn[{i}] '{r.name}' está DUPLICADO. Se ignora.");
                continue;
            }
            zonasSpawn.Add(r);
            Bounds bb = r.bounds;
            Debug.Log($"Zona {zonasSpawn.Count - 1}: '{r.name}' " +
                      $"center={bb.center} extents={bb.extents} " +
                      $"active={r.gameObject.activeInHierarchy}");
        }

        // Fallback por tag si la lista quedó vacía
        if (zonasSpawn.Count == 0)
        {
            GameObject[] planos = GameObject.FindGameObjectsWithTag("PlanoSpawner");
            foreach (var go in planos)
            {
                Renderer rend = go.GetComponent<Renderer>();
                if (rend != null) zonasSpawn.Add(rend);
            }
            if (zonasSpawn.Count > 0)
                Debug.Log($"Usando {zonasSpawn.Count} plano(s) por tag 'PlanoSpawner'.");
        }

        if (zonasSpawn.Count == 0)
            Debug.LogError("No hay planos de spawn válidos.");
        else
            Debug.Log($"✅ {zonasSpawn.Count} zona(s) activas: " +
                      string.Join(", ", zonasSpawn.ConvertAll(z => z.name)));
    }

    void OnImageAdded(object sender, FileSystemEventArgs e)
    {
        string ext = Path.GetExtension(e.FullPath).ToLower();
        if (ext == ".png" || ext == ".jpg" || ext == ".jpeg")
            pendingImagePath = e.FullPath;
    }

    void Update()
    {
        if (pendingImagePath != null)
        {
            LoadImage(pendingImagePath);
            pendingImagePath = null;
        }
    }

    void LoadImage(string path)
    {
        if (zonasSpawn.Count == 0)
        {
            Debug.LogError("FileWatcherLoaderAncho: sin zonas de spawn.");
            return;
        }

        byte[] bytes = File.ReadAllBytes(path);
        Texture2D tex = new Texture2D(2, 2);
        tex.LoadImage(bytes);

        Sprite sprite = Sprite.Create(tex,
            new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f));

        int idx = Random.Range(0, zonasSpawn.Count);
        Renderer zonaRend = zonasSpawn[idx];
        Bounds b = zonaRend.bounds;

        bool desdeIzquierda = Random.value > 0.5f;
        float spawnX = desdeIzquierda ? b.min.x : b.max.x;
        float spawnY = Random.Range(b.min.y, b.max.y);
        float spawnZ = b.center.z + zOffset;

        Debug.Log($"Spawn '{Path.GetFileName(path)}' → idx={idx}/{zonasSpawn.Count - 1} " +
                  $"plano='{zonaRend.name}' centro={b.center} extents={b.extents} " +
                  $"pos=({spawnX:F2},{spawnY:F2},{spawnZ:F2}) desdeIzq={desdeIzquierda}");

        GameObject go = new GameObject(Path.GetFileName(path));
        go.transform.position = new Vector3(spawnX, spawnY, spawnZ);

        float tamaño = Random.Range(tamañoMin, tamañoMax);

        // 👇 FLIP por escala: el signo de X lo maneja Movement.
        // Acá solo fijamos el tamaño absoluto y la dirección inicial por el signo.
        float signX = desdeIzquierda ? -1f : 1f; // si viene de izq, "mira" a la der (+x)
        go.transform.localScale = new Vector3(tamaño * signX, tamaño, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = sortingOrder;
        sr.flipX = false; // 🔑 flipX OFF — el flip lo hace localScale

        ApplySingleShader(go, sr, tamaño);

        Movement move = go.AddComponent<Movement>();
        move.limitePantalla = b.extents.x;
        move.limiteVertical = b.extents.y;
        move.centro = new Vector2(b.center.x, b.center.y);
        move.direccionInicial = desdeIzquierda ? 1 : -1;
        move.speed = Random.Range(1f, 4f);
    }

    void ApplySingleShader(GameObject go, SpriteRenderer sr, float tamaño)
    {
        if (shaderOndulacion == null)
        {
            Debug.LogWarning("FileWatcherLoaderAncho: shaderOndulacion sin asignar.");
            return;
        }

        Material mat = new Material(shaderOndulacion);

        float s = waveSpeed;
        float f = waveFreq;
        float a = waveAmp;

        if (waveRandomVariation > 0f)
        {
            s *= Mathf.Max(0.2f, 1f + Random.Range(-waveRandomVariation, waveRandomVariation));
            f *= Mathf.Max(0.2f, 1f + Random.Range(-waveRandomVariation, waveRandomVariation));
            a *= Mathf.Max(0.3f, 1f + Random.Range(-waveRandomVariation, waveRandomVariation));
        }

        // Compensar escala para que la amplitud en MUNDO sea constante
        float aCompensada = a / Mathf.Max(0.01f, tamaño);

        mat.SetFloat("_WaveSpeed", s);
        mat.SetFloat("_WaveFreq", f);
        mat.SetFloat("_WaveAmp", aCompensada);
        mat.SetFloat("_CutoffThreshold", cutoffThreshold);
        mat.SetFloat("_Feather", feather);

        sr.material = mat;
    }

    void OnDestroy()
    {
        watcher?.Dispose();
    }
}