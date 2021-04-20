using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Bindings;

public class AreaRegistry
{
    private Dictionary<string, List<OnlineEntity>> area_entities = new Dictionary<string, List<OnlineEntity>>();

    private Dictionary<string, PacketBuffer> area_buffers = new Dictionary<string, PacketBuffer>();

    private Dictionary<string, PacketBuffer> area_udp_buffers = new Dictionary<string, PacketBuffer>();

    private Dictionary<string, List<ConnectedToArea>> area_connected = new Dictionary<string, List<ConnectedToArea>>();

    public void RegisterEntity (in string area, OnlineEntity entity)
    {
        if (!area_entities.ContainsKey(area))
        {
            area_entities.Add(area, new List<OnlineEntity>());

            var buff = new PacketBuffer();
            buff.WriteInteger((int)ServerPackets.GameEvent);

            var bufff = new PacketBuffer();
            bufff.WriteInteger((int)ServerPackets.GameEvent);

            area_buffers.Add(area, buff);
            area_udp_buffers.Add(area, bufff);
        }

        area_entities[area].Add(entity);
    }

    public void ConnectToAreaEvents (in string area, ConnectedToArea c)
    {
        if (!area_connected.ContainsKey(area)) area_connected.Add(area, new List<ConnectedToArea>());

        area_connected[area].Add(c);
    }

    public PacketBuffer GetAreaGameEventBuffer (in string area)
    {
        if (!area_buffers.ContainsKey(area)) return new PacketBuffer();
        return area_buffers[area];
    }

    public PacketBuffer GetAreaUDPGameEventBuffer (in string area)
    {
        if (!area_udp_buffers.ContainsKey(area)) return new PacketBuffer();
        return area_udp_buffers[area];
    }

    public List<OnlineEntity> GetAreaOEntities (in string area)
    {
        return area_entities[area];
    }

    public byte[] GetPacketAllCurrentlySpawned (in string area)
    {
        List<OnlineEntity> a_entities;

        if (area_entities.TryGetValue(area, out a_entities))
        {
            var buff = new PacketBuffer();

            buff.WriteInteger((int)ServerPackets.GameEvent);

            for (int i = 0; i < a_entities.Count; ++i)
            {
                var OEnt = a_entities[i];

                var position = OEnt.transform.position;

                var scale = OEnt.transform.localScale;

                OEnt.NotifyObjectSpawned(buff, area, position, scale, OEnt.transform.eulerAngles.z);

                OEnt.variables.GetVariablesPacketsOnBuffer(buff);
            }

            var toReturn = buff.ToArray();

            buff.Dispose();
            return toReturn;
        }
        
        return null;
    }

    public byte[] GetAndClearAreaBuffer (in string area)
    {
        var buff = area_buffers[area];
        byte[] data = buff.ToArray();

        buff.Clear();

        buff.WriteInteger((int) ServerPackets.GameEvent);

        return data;
    }

    public void ClearAreaBuffers (in string area)
    {
        var buff = area_buffers[area];

        buff.Clear();

        buff.WriteInteger((int) ServerPackets.GameEvent);

        var bufff = area_udp_buffers[area];

        bufff.Clear();

        bufff.WriteInteger((int) ServerPackets.GameEvent);
    }

    public void ClearAllAreaBuffers ()
    {
        foreach (KeyValuePair<string, PacketBuffer> kv in area_buffers)
        {
            kv.Value.Clear();
            kv.Value.WriteInteger((int) ServerPackets.GameEvent);
        }

        foreach (KeyValuePair<string, PacketBuffer> kv in area_udp_buffers)
        {
            kv.Value.Clear();
            kv.Value.WriteInteger((int) ServerPackets.GameEvent);
        }
    }

    public void UnregisterEntity (in string area, OnlineEntity entity)
    {
        var a_entities = area_entities[area];

        if (a_entities.Contains(entity)) a_entities.Remove(entity);

        if (a_entities.Count > 0) return;

        DespawnArea(area);
    }

    public void SpawnArea (in string area)
    {
        area_entities.Add(area, new List<OnlineEntity>());

        var buff = new PacketBuffer();
        buff.WriteInteger((int)ServerPackets.GameEvent);

        var bufff = new PacketBuffer();
        bufff.WriteInteger((int)ServerPackets.GameEvent);

        area_buffers.Add(area, buff);
        area_udp_buffers.Add(area, bufff);
    }

    public void DespawnArea (in string area)
    {
        foreach (OnlineEntity oentity in area_entities[area])
        {
            oentity.Despawn();
        }

        area_entities.Remove(area);
        area_buffers.Remove(area);
        area_udp_buffers.Remove(area);

        if (area_connected.ContainsKey(area))
        {
            var ac = area_connected[area];

            for (int i = 0; i< ac.Count; ++i)
            {
                ConnectedToArea cta = ac[i];

                cta.OnAreaDespawn();
            }

            area_connected.Remove(area);
        }
    }

    public int AreaCount (in string area)
    {
        List<OnlineEntity> u = null;

        if (area_entities.TryGetValue(area, out u)) return u.Count;

        return 0;
    }

    public bool AreaExists (in string area) => area_entities.ContainsKey(area);

    public void ClearRegistry ()
    {
        area_entities.Clear();
        area_buffers.Clear();
        area_connected.Clear();
    }
}
