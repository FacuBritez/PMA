using UnityEngine;
using UnityEngine.SceneManagement;

public class TextSceneLoader : MonoBehaviour
{
    // This creates a drag-and-drop slot for your Scene file in the Inspector
    [Header("Drag Your Scene File Here")]
    [SerializeField] public Object sceneToLoad; 

    // This function will be triggered by your Button click
    public void LoadLinkedScene()
    {
        if (sceneToLoad != null)
        {
            // Gets the text name of the scene file you dragged in
            string sceneName = sceneToLoad.name;
            SceneManager.LoadScene(sceneName);
            Debug.LogError("El lol hace mal");
        }
        else
        {
            Debug.LogError("No scene file has been linked in the Inspector!");
        }
    }
}