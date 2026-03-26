using UnityEngine;
using UnityEngine.SceneManagement;

// Loads the main menu scene from the bootstrap scene.
public class BootstrapLoader : MonoBehaviour
{
    private const string InitialSceneName = "MainMenu";
    void Start()
    {
        Scene activeScene = SceneManager.GetActiveScene();

        if (activeScene.name == InitialSceneName)
        {
            return;
        }

        SceneManager.LoadScene(InitialSceneName, LoadSceneMode.Single);
    }
}
