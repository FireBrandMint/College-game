using System;
using System.Net.Sockets;
using System.Net;
using System.Collections;
using System.Collections.Generic;
using Bindings;
using UnityEngine;


public static class ClientHandleNetworkData
{
    static ClientHandleNetworkData ()
    {
        InitializeNetworkPackages();
    }
    private delegate void Packet_ (byte[] data);
    private delegate void NetGameEvent (PacketBuffer buffer);
    private static Dictionary<int, Packet_> Packets;
    private static Dictionary<int, NetGameEvent> NGEvents;

    public static void InitializeNetworkPackages ()
    {
        Console.WriteLine("Initializing network packeages.");
        Packets = new Dictionary <int, Packet_>
        {
            { (int) ServerPackets.SConnectionOK, HandleConnectionOK },
            { (int) ServerPackets.GameEvent, HandleGameNetEvents},
            { (int) ServerPackets.PingMeasure, PingMeasure},
            { (int) ClientPackets.NameRequest, NameSet },
            { (int) ServerPackets.PingConfirmation, PingConfirmation}
        };

        NGEvents = new Dictionary<int, NetGameEvent>
        {
            {(int) GameEvents.Teleport, TeleportEntity},
            {(int) GameEvents.Move, MoveEntity},
            {(int) GameEvents.OEntity_Initialized, InitializedEntity},
            {(int) GameEvents.OEntity_Variable, ChangedEntityVar},
            {(int) GameEvents.OEntity_Despawned, DespawnEntity},
            {(int) GameEvents.Set_Player, SetPlayer},
            {(int) GameEvents.Player_Changed_Areas, PlayerChangedAreas},
            {(int) GameEvents.Player_inventory, PlayerInventory},
            {(int) GameEvents.Player_invetory_set_item, PlayerSetInventoryItem},
            {(int) GameEvents.OEntity_SetAnimatorBool, EntitySetAnimatorBool},
            {(int) GameEvents.OEntity_SetAnimatorFloat, EntitySetAnimatorFloat},
            {(int) GameEvents.OEntity_SetAnimatorInteger, EntitySetAnimatorInteger},
            {(int) GameEvents.OEntity_SetAnimation, EntitySetAnimation},
            {(int) GameEvents.OEntity_Speed, EntitySetSpeed},
            {(int) GameEvents.Player_Input, PlayerInput}
        };
    }

    public static void HandleNetworkInformation (byte[] data)
    {
        PacketBuffer buffer = new PacketBuffer(data);
        int packetnum = buffer.ReadInteger();
        buffer.Dispose();
        if (Packets.TryGetValue(packetnum, out Packet_ Packet))
        {
            Packet.Invoke(data);
        }
    }

    private static void PingMeasure (byte[] data)
    {
        ClientTCP.instance.SendData(data);

        ClientTCP.instance.LastPing = 0;
    }
    private static void PingConfirmation (byte [] data)
    {
        using (PacketBuffer buffer = new PacketBuffer(data))
        {
            buffer.ReadInteger();
            var p = (Player) OnlineEntity.OEntities[buffer.ReadInteger()];
            p.ping = buffer.ReadInteger();
        }
    }

    private static void NameSet (byte[] data)
    {
        using (PacketBuffer buffer = new PacketBuffer(data))
        {
            buffer.ReadInteger();
            var p = (Player) OnlineEntity.OEntities[buffer.ReadInteger()];
            p.PlayerName = buffer.ReadString();
        }
    }

    private static void HandleGameNetEvents (byte [] data)
    {
        PacketBuffer buffer = new PacketBuffer(data);

        buffer.ReadInteger();

        while (buffer.GetReadPosition() < buffer.Length())
        {
            if (NGEvents.TryGetValue(buffer.ReadInteger(), out NetGameEvent g_event))
            {
                g_event.Invoke(buffer);
            }
        }
    }



    //GAME EVENTS

    private static void InitializedEntity (PacketBuffer buffer)
    {
        var prefab = Resources.Load(buffer.ReadString()) as GameObject;
        GameObject obj = MonoBehaviour.Instantiate(prefab);
        OnlineEntity oEnt = obj.GetComponent<OnlineEntity>();
        int id = buffer.ReadInteger();

        var me = ClientTCP.instance;

        if (OnlineEntity.OEntities.ContainsKey(id) && me.ControlledEntityHere && me.ControlledEntity == id) return;

        oEnt.Initialize(buffer.ReadString(), new Vector2(buffer.ReadFloat(), buffer.ReadFloat()),
        new Vector2(buffer.ReadFloat(), buffer.ReadFloat()), buffer.ReadFloat(), id);
    }

    public static void DespawnEntity (PacketBuffer buffer)
    {
        OnlineEntity.OEntities[buffer.ReadInteger()].Despawn();
    }

    private static void SetPlayer (PacketBuffer buffer)
    {
        ClientTCP.instance.ControlledEntity = buffer.ReadInteger();

        ClientTCP.instance.ControlledEntityHere = true;
    }

    private static void PlayerChangedAreas (PacketBuffer buffer)
    {
        Player p = OnlineEntity.OEntities[ClientTCP.instance.ControlledEntity] as Player;
        p.ChangeArea(buffer.ReadString());
    }
    
    private static void PlayerInput (PacketBuffer buffer)
    {
        Player p = OnlineEntity.OEntities[buffer.ReadInteger()] as Player;

        p.controls.PutSentActions(buffer);
    }

    private static void PlayerInventory (PacketBuffer buffer)
    {
        Player p = OnlineEntity.OEntities[ClientTCP.instance.ControlledEntity] as Player;

        p.inventory = new Inventory(ClientTCP.instance.ControlledEntity, buffer);
    }

    private static void PlayerSetInventoryItem (PacketBuffer buffer)
    {
        Player p = OnlineEntity.OEntities[ClientTCP.instance.ControlledEntity] as Player;

        if (buffer.ReadByte() == (byte)0)
        {
            p.inventory.Set_Item(buffer.ReadInteger(), null);
            return;
        }

        p.inventory.Set_Item(buffer.ReadInteger(), Item.DeserializeItem(buffer));
    }

    //HERE|

    private static void EntitySetSpeed (PacketBuffer buffer)
    {
        OnlineEntity OEnt;
        if (CTCPTools.EntityExists(buffer.ReadInteger(), out OEnt))
            OEnt.SetAnimationSpeed(buffer.ReadFloat());
    }

    private static void EntitySetAnimatorBool (PacketBuffer buffer)
    {
        OnlineEntity OEnt;
        if (CTCPTools.EntityExists(buffer.ReadInteger(), out OEnt))
            OEnt.SetAnimatorBool(buffer.ReadString(), buffer.ReadByte() == 1);
    }

    private static void EntitySetAnimatorInteger (PacketBuffer buffer)
    {
        OnlineEntity OEnt;
        if (CTCPTools.EntityExists(buffer.ReadInteger(), out OEnt))
            OEnt.SetAnimatorInteger(buffer.ReadString(), buffer.ReadInteger());
    }

    private static void EntitySetAnimatorFloat (PacketBuffer buffer)
    {
        OnlineEntity OEnt;
        if (CTCPTools.EntityExists(buffer.ReadInteger(), out OEnt))
            OEnt.SetAnimatorFloat(buffer.ReadString(), buffer.ReadFloat());
    }

    private static void EntitySetAnimation (PacketBuffer buffer)
    {
        OnlineEntity OEnt;
        if (CTCPTools.EntityExists(buffer.ReadInteger(), out OEnt))
        OEnt.SetAnimation(buffer.ReadString());
    }

    private static void ChangedEntityVar (PacketBuffer buffer)
    {
        OnlineEntity OEnt;
        if (CTCPTools.EntityExists(buffer.ReadInteger(), out OEnt))
            OEnt.variables.SetVar(buffer);
    }

    private static void TeleportEntity (PacketBuffer buffer)
    {
        OnlineEntity OEnt;
        if (CTCPTools.EntityExists(buffer.ReadInteger(), out OEnt))
            OEnt.Teleport_client(buffer);
    }

    private static void MoveEntity (PacketBuffer buffer)
    {
        OnlineEntity OEnt;
        if (CTCPTools.EntityExists(buffer.ReadInteger(), out OEnt))
            OEnt.Move_client(buffer);
    }

    //END

    private static void HandleConnectionOK (byte[] data)
    {
        PacketBuffer buffer = new PacketBuffer();
        buffer.WriteBytes(data);
        int packetnum = buffer.ReadInteger();
        string message = buffer.ReadString();
        buffer.Dispose();

        ClientTCP.instance.ThankYouServer();
    }
}