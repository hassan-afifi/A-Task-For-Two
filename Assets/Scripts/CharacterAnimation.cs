using System;
using UnityEngine;
[RequireComponent(typeof(Animator))]

// Applies movement state to the character animator.
public class CharacterAnimation : MonoBehaviour
{
    private enum FloatParam
    {
        Vertical,
        Horizontal
    }

    private enum BoolParam
    {
        Crouching
    }

    private enum TriggerParam
    {
        Jump
    }

    private Animator animator;
    private PlayerMovement player;

    // Caches required animator and movement references.
    void Awake()
    {
        animator = GetComponent<Animator>();
        player = GetComponentInParent<PlayerMovement>();

        if (player == null)
        {
            throw new InvalidOperationException("CharacterAnimation setup failed: missing PlayerMovement in parent.");
        }
    }

    // Pushes movement values into animator parameters each frame.
    void Update()
    {
        if (!player.IsOwner)
        {
            return;
        }

        animator.SetFloat(FloatName(FloatParam.Vertical), player.FinalMove.z, 0.1f, Time.deltaTime);
        animator.SetFloat(FloatName(FloatParam.Horizontal), player.FinalMove.x, 0.1f, Time.deltaTime);
        animator.SetBool(BoolName(BoolParam.Crouching), player.IsCrouching);

        if (player.JumpTriggered)
        {
            animator.SetTrigger(TriggerName(TriggerParam.Jump));
        }
    }

    // Maps float parameter keys to animator parameter names.
    static string FloatName(FloatParam param)
    {
        switch (param)
        {
        case FloatParam.Vertical: return "Vertical";
        case FloatParam.Horizontal: return "Horizontal";
            default: throw new ArgumentOutOfRangeException(nameof(param), param, null);
        }
    }

    // Maps bool parameter keys to animator parameter names.
    static string BoolName(BoolParam param)
    {
        switch (param)
        {
        case BoolParam.Crouching: return "Crouching";
            default: throw new ArgumentOutOfRangeException(nameof(param), param, null);
        }
    }

    // Maps trigger parameter keys to animator parameter names.
    static string TriggerName(TriggerParam trigger)
    {
        switch (trigger)
        {
        case TriggerParam.Jump: return "Jump";
        default: throw new ArgumentOutOfRangeException(nameof(trigger), trigger, null);
        }
    }
}
