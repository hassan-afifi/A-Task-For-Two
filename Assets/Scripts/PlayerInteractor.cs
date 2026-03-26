using System;
using UnityEngine;
[RequireComponent(typeof(PlayerInputHandler))]
[RequireComponent(typeof(PlayerMovement))]

// Performs interact raycasts for clickable world objects.
public class PlayerInteractor : MonoBehaviour
{
    private const float InteractDistance = 2.8f;
    private PlayerInputHandler inputHandler;
    private PlayerMovement movement;
    void Awake()
    {
        inputHandler = GetComponent<PlayerInputHandler>();
        movement = GetComponent<PlayerMovement>();
    }

    void Update()
    {
        if (!movement.IsOwner || !movement.IsSpawned)
        {
            return;
        }

        if (!inputHandler.ConsumeInteract())
        {
            return;
        }

        Camera currentCamera = movement.PlayerCamera;

        if (currentCamera == null)
        {
            throw new InvalidOperationException("PlayerInteractor state failed: player camera reference is missing.");
        }

        Ray ray = currentCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (!Physics.Raycast(ray, out RaycastHit hit, InteractDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
        {
            return;
        }

        DieClickAnimate die = hit.collider.GetComponentInParent<DieClickAnimate>();

        if (die == null)
        {
            return;
        }

        die.RollDie();
    }
}
