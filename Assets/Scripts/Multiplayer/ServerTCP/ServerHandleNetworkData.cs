using System;
using System.Collections.Generic;
using Bindings;
using UnityEngine;
public static class ServerHandleNetworkData
{

    private delegate void Packet_ (int index, PacketBuffer buffer);
    private static Dictionary<int, Packet_> Packets;

    static ServerHandleNetworkData ()
    {
        InitializeNetworkPackages();
    }

    public static void InitializeNetworkPackages ()
    {
        Console.WriteLine("Initializing network packeages.");
        Packets = new Dictionary <int, Packet_>
        {
            { (int) ClientPackets.CThankYou, HandleThankYou },
            { (int) ClientPackets.ControlInputs, ClientInputRequest },
            { (int) ServerPackets.PingMeasure, ClientPing },
            { (int) ClientPackets.EntityUnloaded, EntityUnloaded },
            { (int) ClientPackets.NameRequest, NameRequest },
            { (int) ClientPackets.NameItRequest, NameItRequest }
        };
    }

    public static void HandleNetworkInformation (int index, byte[] data)
    {
        int packetnum; PacketBuffer buffer = new PacketBuffer();
        buffer.WriteBytes(data);
        packetnum = buffer.ReadInteger();
        buffer.Dispose();
        if (Packets.TryGetValue(packetnum, out Packet_ Packet))
        {
            Packet.Invoke(index, buffer);
        }
    }

    private static void ClientPing (int index, PacketBuffer buffer)
    {
        var client = ServerTCP.instance.clients[index];

        if (client != null)
        {
            client.StartedWaitingPing = false;
        }
    }

    private static void ClientInputRequest (int index, PacketBuffer buffer)
    {
        try
        {
            var client = ServerTCP.instance.clients[index];

            var ec = client.EntityControlled;

            if (ec != null)
            {
                
                ec.controls.PutSentActions(buffer);

                var cb = ec.GetCurrentBuffer();

                cb.WriteInteger((int)GameEvents.Player_Input);

                cb.WriteInteger(ec.entityID);

                cb.WriteBytes(ec.controls.GetInputsData());

                Vector2 pos = ec.controls.ActionsLocation;

                cb.WriteFloat(pos.x);
                cb.WriteFloat(pos.y);
            }
        }
        catch
        {
            
        }
    }

    private static void EntityUnloaded (int index, PacketBuffer buffer)
    {
        OnlineEntity OEnt;
        if (OnlineEntity.OEntities.TryGetValue(index, out OEnt))
        {
            PacketBuffer buff = new PacketBuffer();
            buff.WriteInteger((int) ServerPackets.GameEvent);
            OEnt.NotifyObjectSpawned(buff, OEnt.Current_Area, OEnt.transform.position, OEnt.transform.localScale, OEnt.transform.rotation.eulerAngles.z);
            ServerTCP.instance.SendDataTo(index, buff.ToArray());
        }
    }

    private static void NameRequest (int index, PacketBuffer buffer)
    {
        string newname = buffer.ReadString();
        ServerTCP.instance.clients[index].EntityControlled.PlayerName = newname;
        var pp = ServerTCP.instance.clients[index].EntityControlled;
        using (PacketBuffer buff = new PacketBuffer())
        {
            buff.WriteInteger((int)ClientPackets.NameRequest);
            buff.WriteInteger(pp.entityID);
            buff.WriteString(newname);

            var toSend = buff.ToArray();

            foreach (Player p in Player.players)
            {
                ServerTCP.instance.SendDataTo(p.playerID, toSend);
            }
        }
    }

    private static void NameItRequest (int index, PacketBuffer buffer)
    {
        OnlineEntity e;
        if (OnlineEntity.OEntities.TryGetValue(buffer.ReadInteger(), out e))
        {
            var p = e as Player;
            if (e != null)
            {
                using (PacketBuffer buff = new PacketBuffer())
                {
                    buff.WriteInteger((int)ClientPackets.NameRequest);
                    buff.WriteInteger(p.entityID);
                    buff.WriteString(p.PlayerName);

                    ServerTCP.instance.SendDataTo(index, buff.ToArray());
                }
            }
        }
    }

    public static void HandleThankYou (int index, PacketBuffer buffer)
    {
        try
        {
            string message = buffer.ReadString();
            buffer.Dispose();

            Console.WriteLine(message);
        }
        catch {}
    }

    private static void HandleConnectionOK (int index, PacketBuffer buffer)
    {
        try
        {
            string message = buffer.ReadString();
            buffer.Dispose();
        }
        catch
        {

        }
    }
}