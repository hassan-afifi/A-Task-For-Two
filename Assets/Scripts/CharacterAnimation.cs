using UnityEngine;

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

    void Awake()
    {
        animator = GetComponent<Animator>();
        player = GetComponentInParent<PlayerMovement>();

        if (animator == null || player == null)
        {
            enabled = false;
        }
    }

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

    static string FloatName(FloatParam param)
    {
        switch (param)
        {
            case FloatParam.Vertical:
                return "Vertical";
            case FloatParam.Horizontal:
                return "Horizontal";
            default:
                throw new System.ArgumentOutOfRangeException(nameof(param), param, null);
        }
    }

    static string BoolName(BoolParam param)
    {
        switch (param)
        {
            case BoolParam.Crouching:
                return "Crouching";
            default:
                throw new System.ArgumentOutOfRangeException(nameof(param), param, null);
        }
    }

    static string TriggerName(TriggerParam trigger)
    {
        switch (trigger)
        {
            case TriggerParam.Jump:
                return "Jump";
            default:
                throw new System.ArgumentOutOfRangeException(nameof(trigger), trigger, null);
        }
    }
}
