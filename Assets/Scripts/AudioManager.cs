using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    private static AudioManager Instance;

    [SerializeField] private AudioSource uiAudioSource;
    [SerializeField] private AudioClip buttonHoverClip;
    [SerializeField] private AudioClip buttonClickClip;
    [SerializeField] [Range(0f, 1f)] private float buttonHoverVolume = 1f;
    [SerializeField] [Range(0f, 1f)] private float buttonClickVolume = 1f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public static void PlayHover()
    {
        if (Instance == null)
        {
            return;
        }

        Instance.PlayOneShot(Instance.buttonHoverClip, Instance.buttonHoverVolume);
    }

    public static void PlayClick()
    {
        if (Instance == null)
        {
            return;
        }

        Instance.PlayOneShot(Instance.buttonClickClip, Instance.buttonClickVolume);
    }

    void PlayOneShot(AudioClip clip, float volume)
    {
        if (uiAudioSource == null || clip == null)
        {
            return;
        }

        uiAudioSource.PlayOneShot(clip, volume);
    }
}
