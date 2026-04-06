using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
[RequireComponent(typeof(NetworkObject))]

// Triggers and displays the shared victory end screen.
public class EndScreen : NetworkBehaviour
{
    private const float Duration = 0.82f;
    [SerializeField] private Collider triggerZone;
    [SerializeField] private GameObject panel;
    [SerializeField] private Canvas hudCanvas;
    [SerializeField] private AudioSource endScreenStartAudioSource;
    [SerializeField] private AudioClip endScreenStartClip;
    private readonly Collider[] triggerHits = new Collider[16];
    private bool triggered;
    private bool localShown;
    private Image fadeImage;
    private Coroutine fadeRoutine;

    // Tracks whether the victory screen is currently shown.
    public static bool IsShown { get; private set; }

    // Validates setup and starts with the panel hidden.
    void Awake()
    {
        EnsureSetup();
        IsShown = false;
        panel.SetActive(false);
    }

    // Clears shared end-screen state when the object is destroyed.
    public override void OnDestroy()
    {
        base.OnDestroy();
    }

    // Monitors the trigger zone and starts the shared end screen.
    void Update()
    {
        if (triggered)
        {
            return;
        }

        if (IsSpawned && !IsServer)
        {
            // Only the server decides trigger activation in networked sessions.
            return;
        }

        if (!IsPlayerInTriggerZone())
        {
            return;
        }

        triggered = true;

        if (!IsSpawned)
        {
            // Offline fallback for local testing without a spawned NetworkObject.
            ShowLocal();
            return;
        }

        ShowEndRpc();
    }

    [Rpc(SendTo.Everyone)]
    void ShowEndRpc()
    {
        ShowLocal();
    }

    // Returns all players to the main menu.
    public void ReturnToMainMenu()
    {
        MenuActions.ReturnToMainMenu();
    }

    // Exits play mode or the built application.
    public void ExitGame()
    {
        MenuActions.ExitGame();
    }

    // Applies local end-screen state and starts the fade sequence.
    void ShowLocal()
    {
        if (localShown)
        {
            return;
        }

        localShown = true;
        IsShown = true;
        OptionsMenu.StopGameMusic();
        PauseMenu.ForceClose();
        panel.SetActive(false);
        hudCanvas.enabled = false;
        DisableLocalPlayerControls();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
        }

        PlayEndScreenStartSfx();
        fadeRoutine = StartCoroutine(FadeToEndScreen());
    }

    // Disables local player control scripts during the end screen.
    void DisableLocalPlayerControls()
    {
        // Disable only owner-side controls so remote proxies stay untouched.
        PlayerInputHandler[] inputs = FindObjectsByType<PlayerInputHandler>(FindObjectsSortMode.None);

        for (int i = 0; i < inputs.Length; i++)
        {
            if (inputs[i].IsOwner)
            {
                inputs[i].enabled = false;
            }
        }

        PlayerMovement[] movements = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);

        for (int i = 0; i < movements.Length; i++)
        {
            if (movements[i].IsOwner)
            {
                movements[i].enabled = false;
            }
        }

        PlayerInteractor[] interactors = FindObjectsByType<PlayerInteractor>(FindObjectsSortMode.None);

        for (int i = 0; i < interactors.Length; i++)
        {
            PlayerMovement movement = interactors[i].GetComponent<PlayerMovement>();

            if (movement != null && movement.IsOwner)
            {
                interactors[i].enabled = false;
            }
        }
    }

    // Checks whether the collider belongs to a player.
    bool IsPlayer(Collider other)
    {
        PlayerMovement movement = other.GetComponentInParent<PlayerMovement>();
        return movement != null;
    }

    // Checks whether any player is currently inside the trigger zone.
    bool IsPlayerInTriggerZone()
    {
        // Box query is cheaper than allocating overlap calls each frame.
        int count = Physics.OverlapBoxNonAlloc(triggerZone.bounds.center, triggerZone.bounds.extents, triggerHits, triggerZone.transform.rotation, ~0, QueryTriggerInteraction.Collide);

        for (int i = 0; i < count; i++)
        {
            if (triggerHits[i] != null && IsPlayer(triggerHits[i]))
            {
                return true;
            }
        }

        return false;
    }

    // Fades to white, shows the panel, then fades back to clear.
    IEnumerator FadeToEndScreen()
    {
        EnsureFadeOverlay();
        fadeImage.raycastTarget = true;
        float elapsed = 0f;

        // First half: fade from clear to white.
        while (elapsed < Duration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetFadeAlpha(elapsed / Duration);
            yield return null;
        }

        SetFadeAlpha(1f);
        yield return new WaitForSecondsRealtime(Duration);
        panel.SetActive(true);
        elapsed = 0f;

        // Second half: reveal end-screen panel by fading white back out.
        while (elapsed < Duration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetFadeAlpha(1f - (elapsed / Duration));
            yield return null;
        }

        SetFadeAlpha(0f);
        fadeImage.raycastTarget = false;
    }

    // Creates the fullscreen white fade overlay on first use.
    void EnsureFadeOverlay()
    {
        if (fadeImage != null)
        {
            return;
        }

        GameObject canvasObj = new GameObject("EndFadeCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;
        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        GameObject imageObj = new GameObject("EndFadeImage", typeof(RectTransform), typeof(Image));
        RectTransform rect = imageObj.GetComponent<RectTransform>();
        imageObj.transform.SetParent(canvasObj.transform, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        fadeImage = imageObj.GetComponent<Image>();
        fadeImage.color = new Color(1f, 1f, 1f, 0f);
        fadeImage.raycastTarget = true;
    }

    // Sets the current overlay alpha value.
    void SetFadeAlpha(float alpha)
    {
        Color color = fadeImage.color;
        color.a = Mathf.Clamp01(alpha);
        fadeImage.color = color;
    }

    // Plays the start sound when the end screen begins.
    void PlayEndScreenStartSfx()
    {
        endScreenStartAudioSource.PlayOneShot(endScreenStartClip, 1f);
    }

    // Validates all required end-screen references.
    void EnsureSetup()
    {
        if (triggerZone == null)
        {
            throw new InvalidOperationException("EndScreen setup failed: triggerZone reference is missing.");
        }

        if (!triggerZone.isTrigger)
        {
            throw new InvalidOperationException("EndScreen setup failed: triggerZone collider must be configured as trigger.");
        }

        if (hudCanvas == null)
        {
            throw new InvalidOperationException("EndScreen setup failed: hudCanvas reference is missing.");
        }

        if (panel == null)
        {
            throw new InvalidOperationException("EndScreen setup failed: panel reference is missing.");
        }

        if (endScreenStartAudioSource == null)
        {
            throw new InvalidOperationException("EndScreen setup failed: endScreenStartAudioSource reference is missing.");
        }

        if (endScreenStartClip == null)
        {
            throw new InvalidOperationException("EndScreen setup failed: endScreenStartClip reference is missing.");
        }
    }
}
