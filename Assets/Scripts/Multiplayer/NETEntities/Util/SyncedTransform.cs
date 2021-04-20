using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SyncedTransform : MonoBehaviour
{
    public OnlineEntity OnlineEntityTracked = null;
    void Start()
    {
        if(!ProgramInfo.isServer)
        {
            Destroy(this);
            return;
        }

        if (OnlineEntityTracked == null)
        {
            OnlineEntityTracked = GetComponent<OnlineEntity>();
        }
    }

    Vector2 lastPos = new Vector2(float.MaxValue, float.MaxValue);
    float lastRot = float.MaxValue;
    Vector2 lastScale = new Vector2(float.MaxValue, float.MaxValue);
    void Update()
    {
        Vector2 pos = transform.position;
        float rot = transform.eulerAngles.z;
        Vector2 scale = transform.localScale;

        bool posb = false;
        bool rotb = false;
        bool scaleb = false;

        bool go = false;

        if (pos != lastPos)
        {
            lastPos = pos;
            posb = true;
            go = true;
        }

        if (rot != lastRot)
        {
            lastRot = rot;
            rotb = true;
            go = true;
        }

        if (scale != lastScale)
        {
            lastScale = scale;
            scaleb = true;
            go = true;
        }

        if (go) OnlineEntityTracked.Move_server(posb, rotb, scaleb);
    }
}