using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] private AudioSource uiAudioSource;
    [SerializeField] private AudioClip buttonHoverClip;
    [SerializeField] private AudioClip buttonClickClip;

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

        Instance.PlayOneShot(Instance.buttonHoverClip, 1f);
    }

    public static void PlayClick()
    {
        if (Instance == null)
        {
            return;
        }

        Instance.PlayOneShot(Instance.buttonClickClip, 1f);
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
