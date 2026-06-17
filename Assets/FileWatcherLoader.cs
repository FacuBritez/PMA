using UnityEngine;
using System.IO;
public class FileWatcherLoader : MonoBehaviour
{
    public string watchFolder = "Assets/WatchedImages";
    private FileSystemWatcher watcher;
    private string pendingImagePath;
    public Shader fishShader;
    void Start()
    {
        Debug.Log("pantallas detectadas " + Display.displays.Length);
        for (int i = 1; i < Display.displays.Length; i++)
        {
            Display.displays[i].Activate();
        }
        Application.runInBackground = true;
        if (!Application.isEditor)
        {
            watchFolder = Path.Combine(Path.GetDirectoryName(Application.dataPath), "WatchedImages");
            Debug.Log("Juego exportado");
        }
        if (!Directory.Exists(watchFolder))
            Directory.CreateDirectory(watchFolder);
        watcher = new FileSystemWatcher(watchFolder);
        watcher.Filter = "*.*";
        watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite;
        watcher.Created += OnImageAdded;
        watcher.EnableRaisingEvents = true;
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
    byte[] bytes = File.ReadAllBytes(path);
    Texture2D tex = new Texture2D(2, 2);
    tex.LoadImage(bytes);

    GameObject go = GameObject.CreatePrimitive(PrimitiveType.Plane);
    go.name = Path.GetFileName(path);
    go.transform.position = new Vector3(Random.Range(-9f, 9f), Random.Range(-5f, 5f), -3.5f);
    go.transform.rotation = Quaternion.Euler(-90f, 0f, 90f);

    Material mat = new Material(fishShader);
    mat.mainTexture = tex;
    MeshRenderer mr = go.GetComponent<MeshRenderer>();
    mr.material = mat;

    // Normalizar tamaño según el ancho/alto deseado en unidades de mundo
    float targetSize = 0.3f; // tamaño deseado (ajustalo a tu escala de juego)
    float aspect = (float)tex.width / tex.height;

    if (aspect >= 1f)
    {
        // imagen más ancha que alta
        go.transform.localScale = new Vector3(targetSize, 1f, targetSize / aspect);
    }
    else
    {
        // imagen más alta que ancha
        go.transform.localScale = new Vector3(targetSize * aspect, 1f, targetSize);
    }

    Movement move = go.AddComponent<Movement>();
    move.speed = Random.Range(1f, 4f);
}
    void OnDestroy()
    {
        watcher?.Dispose();
    }
}