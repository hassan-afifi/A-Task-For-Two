using System;
using UnityEngine;
[RequireComponent(typeof(AudioSource))]

// Centralizes menu hover and click sounds.
public class AudioManager : MonoBehaviour
{
    // Stores the global audio manager instance.
    public static AudioManager Instance { get; private set; }
    [SerializeField] private AudioSource uiAudioSource;
    [SerializeField] private AudioClip buttonHoverClip;
    [SerializeField] private AudioClip buttonClickClip;

    // Enforces singleton lifetime and keeps it across scene loads.
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        uiAudioSource ??= GetComponent<AudioSource>();
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Clears singleton reference when this instance is destroyed.
    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // Plays the hover sound effect.
    public static void PlayHover()
    {
        if (Instance == null)
        {
            throw new InvalidOperationException("AudioManager.PlayHover failed: AudioManager instance is missing.");
        }

        Instance.PlayOneShot(Instance.buttonHoverClip, 1f);
    }

    // Plays the click sound effect.
    public static void PlayClick()
    {
        if (Instance == null)
        {
            throw new InvalidOperationException("AudioManager.PlayClick failed: AudioManager instance is missing.");
        }

        Instance.PlayOneShot(Instance.buttonClickClip, 1f);
    }

    // Plays one UI clip through the shared audio source.
    void PlayOneShot(AudioClip clip, float volume)
    {
        if (clip == null)
        {
            throw new ArgumentNullException(nameof(clip), "AudioManager.PlayOneShot failed: clip is missing.");
        }

        uiAudioSource.PlayOneShot(clip, volume);
    }
}
