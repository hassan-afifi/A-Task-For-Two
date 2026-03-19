using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;

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

        if (fpsText != null)
        {
            fpsText.text = string.Format(FpsFormat, Mathf.RoundToInt(fps));
        }

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

        if (pingText == null)
        {
            return;
        }

        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsClient)
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

        if (clockText != null)
        {
            clockText.text = DateTime.Now.ToString(ClockFormat, System.Globalization.CultureInfo.InvariantCulture);
        }
    }

    void Layout()
    {
        int activeIndex = 0;
        PlaceWidget(fpsWidget, ref activeIndex);
        PlaceWidget(pingWidget, ref activeIndex);
        PlaceWidget(clockWidget, ref activeIndex);
        lastFpsOn = fpsWidget != null && fpsWidget.gameObject.activeSelf;
        lastPingOn = pingWidget != null && pingWidget.gameObject.activeSelf;
        lastClockOn = clockWidget != null && clockWidget.gameObject.activeSelf;
        layoutInit = true;
    }

    void LayoutIfNeeded()
    {
        bool fpsOn = fpsWidget != null && fpsWidget.gameObject.activeSelf;
        bool pingOn = pingWidget != null && pingWidget.gameObject.activeSelf;
        bool clockOn = clockWidget != null && clockWidget.gameObject.activeSelf;

        if (!layoutInit || fpsOn != lastFpsOn || pingOn != lastPingOn || clockOn != lastClockOn)
        {
            Layout();
        }
    }

    void PlaceWidget(RectTransform widget, ref int activeIndex)
    {
        if (widget == null)
        {
            return;
        }

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
}
