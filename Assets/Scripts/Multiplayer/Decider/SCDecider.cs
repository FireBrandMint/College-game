using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SCDecider : MonoBehaviour
{
    
    public GameObject server;

    public GameObject client;

    void Start()
    {
        if (ProgramInfo.isServer) Instantiate(server, new Vector2(), Quaternion.identity);
        else Instantiate(client, new Vector2(), Quaternion.identity);

        Destroy(gameObject);
    }
}
