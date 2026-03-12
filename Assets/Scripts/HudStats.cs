using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class HudStats : MonoBehaviour
{
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
    private const float StatWidth = 100f;
    private const float StatHeight = 50f;

    void Update()
    {
        UpdateFps();
        UpdatePing();
        UpdateClock();
    }

    void LateUpdate()
    {
        Layout();
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
            fpsText.text = $"{Mathf.RoundToInt(fps)} FPS";
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
            pingText.text = "-- ms";
            return;
        }

        if (!TryGetPing(out ulong rttMs))
        {
            pingText.text = "-- ms";
            return;
        }

        pingText.text = $"{rttMs} ms";
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
            clockText.text = DateTime.Now.ToString("h:mm tt");
        }
    }

    void Layout()
    {
        int activeIndex = 0;
        PlaceWidget(fpsWidget, ref activeIndex);
        PlaceWidget(pingWidget, ref activeIndex);
        PlaceWidget(clockWidget, ref activeIndex);
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
