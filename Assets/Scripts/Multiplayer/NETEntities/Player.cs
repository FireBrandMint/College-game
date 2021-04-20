using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Bindings;

public class Player : OnlineEntity, ConnectedToPlayer
{

    public static List<Player> players = new List<Player>();

    public int ping = 1;

    public static Player controlledPlayer = null;

    private bool _isControlledPlayer = false;

    public bool isControlledPlayer {get => _isControlledPlayer;
    set
    {
        _isControlledPlayer = value;
        if (value == true)
        {
            controls.IsMain = true;
        }



        controlledPlayer = this;
    }
    }

    string _name = "";

    public string PlayerName {
        get{return _name;}
        set
        {
            _name  = value;
            if (!ProgramInfo.isServer) return;
            using (PacketBuffer buff = new PacketBuffer())
            {
                buff.WriteInteger((int)ClientPackets.NameRequest);
                buff.WriteInteger(entityID);
                buff.WriteString(value);

                var toSend = buff.ToArray();

                foreach (Player p in Player.players)
                {
                    ServerTCP.instance.SendDataTo(p.playerID, toSend);
                }
            }
        }
    }

    float serverClientMaxDistance = 70f;
    Vector2 serverPosition = new Vector2(float.MinValue, float.MinValue);

    public PlayerControls controls = new PlayerControls();

    public Inventory inventory;

    public int playerID = -1;

    public void InitializePlayer (int playerID, string PlayerName)
    {
        players.Add(this);

        if (ProgramInfo.isServer)
        {
            //Shows player the entities before calling the rest
            ServerTCP.instance.SendDataTo(playerID, Areas.GetPacketAllCurrentlySpawned(Current_Area));

            this.playerID = playerID;

            var client = ServerTCP.instance.clients[playerID];
            client.EntityControlled = this;

            using (PacketBuffer buff = new PacketBuffer())
            {
                buff.WriteInteger((int)ServerPackets.GameEvent);

                buff.WriteInteger((int)GameEvents.Set_Player);

                buff.WriteInteger(entityID);

                ServerTCP.instance.SendDataTo(playerID, buff.ToArray());
            }

            ServerTCP.instance.clients[playerID].OnClientDisconnected.Add(OnDisconnected);
        }
        else
        {
            
        }
    }

    public void OnPlayerDisconnect () => Despawn();

    protected override void ClientInit()
    {
        base.ClientInit();

        if (PlayerName == "")
        {
            using (PacketBuffer buffer = new PacketBuffer())
            {
                buffer.WriteInteger((int)ClientPackets.NameItRequest);
                buffer.WriteInteger(entityID);
                ClientTCP.instance.SendData(buffer.ToArray());
            }
        }
    }

    protected override void CommonInit()
    {
        base.CommonInit();
        controls.OnInputsReceived = OnInputsReceived;
    }

    float serverRotation = 0f;

    bool ClientInterpolate = true;
    bool ClientCorrectDistance = true;

    protected override void ClientTick()
    {
        base.ClientTick();

        if (serverPosition.x != float.MinValue)
        {
            Vector2 pos = transform.position;
            if (ClientInterpolate)
            {
                pos = MathUtil.LerpVector(pos, serverPosition, 0.1f);

                transform.position = pos;

                transform.eulerAngles = new Vector3(0f,0f,Mathf.Lerp(transform.eulerAngles.z, serverRotation, 0.8f));
            }

            if (ClientCorrectDistance && Vector2.Distance(pos, serverPosition) > serverClientMaxDistance * ping)
            pos = serverPosition;
        }
    }

    protected override void ServerTick()
    {
        base.ServerTick();
    }

    protected override void CommonTick()
    {
        base.CommonTick();
        controls.tick();

        
    }

    protected virtual void OnInputsReceived ()
    {
        var pping = 0;
        if (ProgramInfo.isServer) pping = (int)(ping * 0.5) - 1;
        else pping = (int)((controlledPlayer == null ? 0 : controlledPlayer.ping) * 0.5) - 1;

        if (isControlledPlayer || pping < 1) return;

        if (ProgramInfo.isServer)
        {
            var ppc = past_positions.Count > pping ? pping : past_positions.Count - 1;
            var pp = past_positions[ppc];
            transform.position = pp;
            past_positions.Clear();
            controls.ActionsLocation = pp;
        }else transform.position = controls.ActionsLocation;

        ClientCorrectDistance = false;
        ClientInterpolate = false;

        for (int i = 0; i < pping; ++i)
        {
            tick.Invoke();
            
            CommonTick();
        }

        ClientCorrectDistance = true;
        ClientInterpolate = true;
    }

    public void ChangeArea (in string area)
    {
        if (ProgramInfo.isServer)
        {
            Area.instances[Current_Area].NotifyPlayerLeft();

            var curBuf = GetCurrentBuffer();
            curBuf.WriteInteger((int)GameEvents.OEntity_Despawned);
            curBuf.WriteInteger(entityID);

            Areas.UnregisterEntity(Current_Area, this);
            Areas.RegisterEntity(area, this);
            Current_Area = area;

            PacketBuffer buff = new PacketBuffer();

            buff.WriteInteger((int)ServerPackets.GameEvent);
            buff.WriteInteger((int) GameEvents.Player_Changed_Areas);
            buff.WriteString(area);

            ServerTCP.instance.SendDataTo(playerID, buff.ToArray());

            ServerTCP.instance.SendDataTo(playerID, Areas.GetPacketAllCurrentlySpawned(area));

            buff.Dispose();
        }
        else
        {
            Areas.UnregisterEntity(Current_Area, this);
            foreach (OnlineEntity o in GetOEntitiesInCurrentArea())
            {
                o.Despawn();
            }
            var ars = Area.instances;
            if (ars.ContainsKey(Current_Area)) ars[Current_Area].Client_DespawnArea();


            ars[area].Client_SpawnArea();
            Areas.RegisterEntity(area, this);

            Current_Area = area;
        }
    }

    public override void Move_client (PacketBuffer buffer)
    {
        if (buffer.ReadByte() == 1)
        {
            serverPosition = new Vector2(buffer.ReadFloat(), buffer.ReadFloat());
        }

        if (buffer.ReadByte() == 1)
        {
            serverRotation = buffer.ReadFloat();
        }

        if (buffer.ReadByte() == 1)
        {
            transform.localScale = new Vector2(buffer.ReadFloat(), buffer.ReadFloat());
        }
    }

    public void ActivatePrediction (int ping) => AllServerPredictionActivate(ping);

    public void EndPrediction () => AllServerPredictionEnd();

    protected virtual void OnDisconnected ()
    {
        Destroy(gameObject);

        var ev = ServerTCP.instance.clients[playerID].OnClientDisconnected;
        if (ev.Contains(OnDisconnected)) ev.Remove(OnDisconnected);
    }

    protected override void OnDespawn_common()
    {
        players.Remove(this);
        if (controls.OnInputsReceived!= null) controls.OnInputsReceived = null;
    }

    protected override void OnDespawn_server()
    {
        base.OnDespawn_server();
        var ev = ServerTCP.instance.clients[playerID].OnClientDisconnected;
        if (ev.Contains(OnDisconnected)) ev.Remove(OnDisconnected);
    }
}
