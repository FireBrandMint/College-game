using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Area : MonoBehaviour
{

    public static Dictionary<string, Area> instances = new Dictionary<string, Area>();

    public string area;

    public GameObject area_prefab;

    private GameObject area_instance;
    
    public Vector2 way_1;

    public Vector2 way_2;

    public Vector2 way_3;

    public Vector2 way_4;

    private int Player_Count = 0;

    void Start ()
    {
        instances.Add(area, this);
    }

    public void TeleportIntoWay (int wayNum, Player player)
    {
        if (!ProgramInfo.isServer) return;

        OnPlayerEntered();

        switch (wayNum)
        {
            case 1: player.Teleport_server(way_1);
            break;
            case 2: player.Teleport_server(way_2);
            break;
            case 3: player.Teleport_server(way_3);
            break;
            case 4: player.Teleport_server(way_4);
            break;
        }

        player.ChangeArea(area);

        ++Player_Count;
    }

    public void TeleportIntoHere (Vector2 where, Player player)
    {
        if (!ProgramInfo.isServer) return;
        OnPlayerEntered();

        player.Teleport_server(where);
        player.ChangeArea(area);

        ++Player_Count;
    }

    public void EnterHere (Player player)
    {
        if (!ProgramInfo.isServer) return;
        OnPlayerEntered();

        player.ChangeArea(area);

        ++Player_Count;
    }

    public void NotifyPlayerLeft ()
    {
        --Player_Count;

        if (Player_Count == 0)
        {
            OnlineEntity.Areas.DespawnArea(area);
            Destroy(area_instance);
            area_instance = null;
        }
    }

    public void OnPlayerEntered ()
    {
        if (!OnlineEntity.Areas.AreaExists(area)) OnlineEntity.Areas.SpawnArea(area);

        if (area_instance==null)
        {
            area_instance = Instantiate(area_prefab, transform.position, transform.rotation);
        }
    }

    public void Client_SpawnArea ()
    {
        area_instance = Instantiate(area_prefab);
    }

    public void Client_DespawnArea ()
    {
        if (area_instance != null)
        {
            Destroy(area_instance);
            area_instance=null;
        }
    }

    void OnDestroy ()
    {
        instances.Remove(area);
    }

}
