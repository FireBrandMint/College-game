using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dummy : MonoBehaviour
{
    public static Dummy instance;

    void Start()
    {
        instance = this;
    }

    public GameObject DInstantiate (string path)
    {
        var prefab = Resources.Load(path) as GameObject;
        return Instantiate(prefab) as GameObject;
    }
}
