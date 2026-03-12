using UnityEngine;
using UnityEngine.SceneManagement;

public class BootstrapLoader : MonoBehaviour
{
    private const string initialSceneName = "MainMenu";

    void Start()
    {
        Scene activeScene = SceneManager.GetActiveScene();

        if (string.IsNullOrWhiteSpace(initialSceneName))
        {
            return;
        }

        if (activeScene.name == initialSceneName)
        {
            return;
        }

        SceneManager.LoadScene(initialSceneName, LoadSceneMode.Single);
    }
}
