using UnityEngine;

public class NCUtil
{

    public static AnimationClip GetClip (Animator animator, string anim)
    {
        RuntimeAnimatorController ac = animator.runtimeAnimatorController;
                
        foreach (AnimationClip a in ac.animationClips) 
        if (a.name == anim)
        {
            return a;
        }

        return null;
    }

}