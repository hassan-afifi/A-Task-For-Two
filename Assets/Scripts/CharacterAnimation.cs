using UnityEngine;

[RequireComponent(typeof(Animator))]
public class CharacterAnimation : MonoBehaviour
{
    private Animator animator;
    private PlayerController player;

    void Awake()
    {
        animator = GetComponent<Animator>();
        player = GetComponentInParent<PlayerController>();
    }

    void Update()
    {
        if (animator == null || player == null || !player.IsOwner) 
        {
            return;
        }

        Vector3 move = player.FinalMove;
        animator.SetFloat("Vertical", move.z, 0.05f, Time.deltaTime);
        animator.SetFloat("Horizontal", move.x, 0.05f, Time.deltaTime);
        animator.SetBool("Crouching", player.IsCrouching);

        if (player.JumpTriggered)
        {
            animator.SetTrigger("Jump");
        }
    }
}
