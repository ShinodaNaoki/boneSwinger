using UnityEngine;

public enum AvatarMotion
{
    idle, walk, run, knockback, attack, hijump, jumpdown, landing
}

public class AvatarAnimator : MonoBehaviour
{
    [SerializeField]
    private Animator animator;

    private void Start()
    {
    }

    public void Trigger(AvatarMotion motion)
    {
        var animationName = motion.ToString();
        animator.SetTrigger(animationName);
    }
}
