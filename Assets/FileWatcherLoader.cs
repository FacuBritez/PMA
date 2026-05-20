using UnityEngine;
using System.IO;

public class FileWatcherLoader : MonoBehaviour
{
    public string watchFolder = "Assets/WatchedImages";
    private FileSystemWatcher watcher;
    private string pendingImagePath;

    void Start()
    {
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
    }

    void OnDestroy()
    {
        watcher?.Dispose();
    }
}