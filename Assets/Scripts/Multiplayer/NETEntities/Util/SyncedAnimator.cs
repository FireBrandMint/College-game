using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SyncedAnimator : MonoBehaviour
{
    public bool TrackSpecificAnimationPlaying = true;

    public bool TrackAnimatorParameters = false;

    public bool TrackAnimatorSpeed = true;

    public bool UDPTrackAnimatorSpeed = false;

    OnlineEntity OEntity = null;

    KeyValuePair<string, AnimatorControllerParameterType>[] parametersToWatch;

    object[] lastObjects;

    string lastName = "??";

    void Start()
    {
        if (!ProgramInfo.isServer)
        {
            Destroy(this);
            return;
        }

        if (OEntity == null) OEntity = GetComponent<OnlineEntity>();



        var anParams = OEntity.animator.parameters;

        List <KeyValuePair<string, AnimatorControllerParameterType>> aLot = new List<KeyValuePair<string, AnimatorControllerParameterType>>(anParams.Length);

        if (TrackAnimatorParameters) lastObjects = new object[anParams.Length];

        if (TrackAnimatorParameters) for (int i = 0; i<anParams.Length; ++i)
        {
            var param = anParams[i];
            aLot.Add(new KeyValuePair<string, AnimatorControllerParameterType>(param.name, param.type));
            switch (param.type)
            {
                case AnimatorControllerParameterType.Bool:
                lastObjects[i] = param.defaultBool;
                break;
                case AnimatorControllerParameterType.Float:
                lastObjects[i] = param.defaultFloat;
                break;
                case AnimatorControllerParameterType.Int:
                lastObjects[i] = param.defaultInt;
                break;
            }
        }

        parametersToWatch = aLot.ToArray();
    }

    float lastSpeed = 1f;

    void Update()
    {
        var animator = OEntity.animator;
        if (TrackAnimatorParameters) for (int i = 0 ; i < parametersToWatch.Length; ++i)
        {
            var ptw = parametersToWatch[i];
            switch (ptw.Value)
            {
                case AnimatorControllerParameterType.Bool:
                bool p = animator.GetBool(ptw.Key);
                if (p != (bool)lastObjects[i])
                {
                    lastObjects[i] = p;
                    OEntity.SetAnimatorBool(ptw.Key, p, false);
                }
                break;
                case AnimatorControllerParameterType.Float:
                float e = animator.GetFloat(ptw.Key);
                if (e != (float)lastObjects[i])
                {
                    lastObjects[i] = e;
                    OEntity.SetAnimatorFloat(ptw.Key, e, false);
                }
                break;
                case AnimatorControllerParameterType.Int:
                int c = animator.GetInteger(ptw.Key);
                if (c != (int)lastObjects[i])
                {
                    lastObjects[i] = c;
                    OEntity.SetAnimatorInteger(ptw.Key, c, false);
                }
                break;
            }
        }

        if (TrackSpecificAnimationPlaying)
        {
            string name = animator.GetCurrentAnimatorClipInfo(0)[0].clip.name;
            if (name != lastName)
            {
                lastName = name;
                OEntity.SetAnimation(name, false);
                //YEEEEEEEEEEEEEEEEEEEEEEES I FINALLY DID IT
            }
        }

        if (TrackAnimatorSpeed)
        {
            var speed = animator.speed;

            if (UDPTrackAnimatorSpeed) OEntity.SetAnimationSpeed(speed, false, false);
            else if (speed != lastSpeed)
            {
                OEntity.SetAnimationSpeed(speed, false);
                lastSpeed = speed;
            }
        }
    }
}
