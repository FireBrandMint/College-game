using Bindings;

public class CTCPTools
{
    
    public static bool EntityExists (int id, out OnlineEntity OEnt)
    {
        var entities = OnlineEntity.OEntities;
        if (entities.ContainsKey(id))
        {
            OEnt = entities[id];
            return true;
        }

        PacketBuffer buff = new PacketBuffer();
        buff.WriteInteger((int) ClientPackets.EntityUnloaded);
        buff.WriteInteger(id);
        ClientTCP.instance.SendData(buff.ToArray());

        OEnt = null;
        return false;
    }

}