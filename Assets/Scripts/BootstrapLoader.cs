using UnityEngine;
using UnityEngine.SceneManagement;

public class BootstrapLoader : MonoBehaviour
{
    private const string DefaultInitialScene = "MainMenu";
    [SerializeField] private string initialSceneName = DefaultInitialScene;

    void Start()
    {
        Scene activeScene = SceneManager.GetActiveScene();

        if (string.IsNullOrWhiteSpace(initialSceneName))
        {
            throw new System.InvalidOperationException("BootstrapLoader setup failed: initialSceneName is empty.");
        }

        if (activeScene.name == initialSceneName)
        {
            return;
        }

        SceneManager.LoadScene(initialSceneName, LoadSceneMode.Single);
    }
}
