using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClientCamera : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        if(ProgramInfo.isServer)
        {
            Destroy(this);
            return;
        }
    }

    // Update is called once per frame
    void Update()
    {
        var ctcp = ClientTCP.instance;
        if (ctcp != null && ctcp.ControlledEntityHere)
        {
            OnlineEntity e;
            if (OnlineEntity.OEntities.TryGetValue(ctcp.ControlledEntity, out e))
            {
                transform.position = e.transform.position;
            }
        }
    }
}
