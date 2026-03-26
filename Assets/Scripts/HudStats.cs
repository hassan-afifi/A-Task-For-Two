using System;
using System.Globalization;
using TMPro;
using Unity.Netcode;
using UnityEngine;

// Updates FPS, ping, and clock widgets on the HUD.
public class HudStats : MonoBehaviour
{
    private const string FpsFormat = "{0} FPS";
    private const string PingUnavailableText = "-- ms";
    private const string PingFormat = "{0} ms";
    private const string ClockFormat = "h:mm tt";
    [SerializeField] private TMP_Text fpsText;
    [SerializeField] private TMP_Text pingText;
    [SerializeField] private TMP_Text clockText;
    [SerializeField] private RectTransform fpsWidget;
    [SerializeField] private RectTransform pingWidget;
    [SerializeField] private RectTransform clockWidget;
    private float fpsTimer;
    private int frameCount;
    private float pingTimer;
    private float clockTimer;
    private bool layoutInit;
    private bool lastFpsOn;
    private bool lastPingOn;
    private bool lastClockOn;
    private const float StatWidth = 100f;
    private const float StatHeight = 50f;
    void Awake()
    {
        EnsureSetup();
        Layout();
    }

    void Update()
    {
        UpdateFps();
        UpdatePing();
        UpdateClock();
        LayoutIfNeeded();
    }

    void UpdateFps()
    {
        frameCount++;
        fpsTimer += Time.unscaledDeltaTime;

        if (fpsTimer < 0.25f)
        {
            return;
        }

        float fps = frameCount / fpsTimer;
        fpsText.text = string.Format(FpsFormat, Mathf.RoundToInt(fps));
        frameCount = 0;
        fpsTimer = 0f;
    }

    void UpdatePing()
    {
        pingTimer += Time.unscaledDeltaTime;

        if (pingTimer < 0.25f)
        {
            return;
        }

        pingTimer = 0f;

        if (NetworkManager.Singleton == null)
        {
            pingText.text = PingUnavailableText;
            return;
        }

        if (!NetworkManager.Singleton.IsClient)
        {
            pingText.text = PingUnavailableText;
            return;
        }

        if (!TryGetPing(out ulong rttMs))
        {
            pingText.text = PingUnavailableText;
            return;
        }

        pingText.text = string.Format(PingFormat, rttMs);
    }

    void UpdateClock()
    {
        clockTimer += Time.unscaledDeltaTime;

        if (clockTimer < 1f)
        {
            return;
        }

        clockTimer = 0f;
        clockText.text = DateTime.Now.ToString(ClockFormat, CultureInfo.InvariantCulture);
    }

    void Layout()
    {
        int activeIndex = 0;
        PlaceWidget(fpsWidget, ref activeIndex);
        PlaceWidget(pingWidget, ref activeIndex);
        PlaceWidget(clockWidget, ref activeIndex);
        lastFpsOn = fpsWidget.gameObject.activeSelf;
        lastPingOn = pingWidget.gameObject.activeSelf;
        lastClockOn = clockWidget.gameObject.activeSelf;
        layoutInit = true;
    }

    void LayoutIfNeeded()
    {
        bool fpsOn = fpsWidget.gameObject.activeSelf;
        bool pingOn = pingWidget.gameObject.activeSelf;
        bool clockOn = clockWidget.gameObject.activeSelf;

        if (!layoutInit || fpsOn != lastFpsOn || pingOn != lastPingOn || clockOn != lastClockOn)
        {
            Layout();
        }
    }

    void PlaceWidget(RectTransform widget, ref int activeIndex)
    {
        widget.anchorMin = new Vector2(0f, 1f);
        widget.anchorMax = new Vector2(0f, 1f);
        widget.pivot = new Vector2(0f, 1f);
        widget.sizeDelta = new Vector2(StatWidth, StatHeight);

        if (!widget.gameObject.activeSelf)
        {
            return;
        }

        widget.anchoredPosition = new Vector2(activeIndex * StatWidth, 0f);
        activeIndex++;
    }

    bool TryGetPing(out ulong rttMs)
    {
        rttMs = 0;

        if (NetworkManager.Singleton == null)
        {
            return false;
        }

        if (NetworkManager.Singleton.IsHost)
        {
            foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
            {
                if (clientId == NetworkManager.Singleton.LocalClientId)
                {
                    continue;
                }

                rttMs = NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetCurrentRtt(clientId);
                return true;
            }

            return false;
        }

        rttMs = NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetCurrentRtt(NetworkManager.ServerClientId);
        return true;
    }

    void EnsureSetup()
    {
        if (fpsText == null)
        {
            throw new InvalidOperationException("HudStats setup failed: fpsText reference is missing.");
        }

        if (pingText == null)
        {
            throw new InvalidOperationException("HudStats setup failed: pingText reference is missing.");
        }

        if (clockText == null)
        {
            throw new InvalidOperationException("HudStats setup failed: clockText reference is missing.");
        }

        if (fpsWidget == null)
        {
            throw new InvalidOperationException("HudStats setup failed: fpsWidget reference is missing.");
        }

        if (pingWidget == null)
        {
            throw new InvalidOperationException("HudStats setup failed: pingWidget reference is missing.");
        }

        if (clockWidget == null)
        {
            throw new InvalidOperationException("HudStats setup failed: clockWidget reference is missing.");
        }
    }
}
