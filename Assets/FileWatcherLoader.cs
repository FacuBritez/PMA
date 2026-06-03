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




        // Si estás en el editor, usa una carpeta en la raíz del proyecto.
        // Si es el juego exportado, creará la carpeta al lado del archivo .exe
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

        Sprite sprite = Sprite.Create(tex,
            new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f));


        GameObject go = new GameObject(Path.GetFileName(path));
        go.transform.position = new Vector3(Random.Range(-9, 9), Random.Range(-5, 5), 0);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;

        // añadimos el shader
        // Creamos un material con el shader
        Material mat = new Material(fishShader);
        sr.material = mat;

        Movement move = go.AddComponent<Movement>();
        move.speed = Random.Range(1f, 4f);
    }

    void OnDestroy()
    {
        watcher?.Dispose();
    }
}