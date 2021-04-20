using Bindings;
using System.Collections.Generic;

public class Inventory
{
    private int player_entity_id;

    public Item[] Items;

    public Inventory(int _player_entity_id, PacketBuffer buffer)
    {
        player_entity_id = _player_entity_id;

        if (buffer!=null)
        {
            List<Item> _Items = new List<Item>();

            int item_count = buffer.ReadInteger();

            for (int i = 0; i < item_count; ++i)
            {
                if (buffer.ReadByte() == 0)
                {
                    _Items.Add(null);
                    continue;
                }

                _Items.Add(Item.DeserializeItem(buffer));
            }

            Items = _Items.ToArray();
        }

        if (ProgramInfo.isServer) SendInventoryToClient();
    }

    public Inventory(int _player_entity_id, int InvSpace)
    {
        player_entity_id = _player_entity_id;

        Items = new Item[InvSpace];

        if (ProgramInfo.isServer) SendInventoryToClient();
    }

    public void SendInventoryToClient ()
    {
        Player p = (Player) OnlineEntity.OEntities[player_entity_id];

        var Player_ID = p.playerID;

        PacketBuffer buff = new PacketBuffer();

        buff.WriteInteger((int) ServerPackets.GameEvent);
        buff.WriteInteger((int) GameEvents.Player_inventory);
        SerializeInventoryToBuffer(buff);

        ServerTCP.instance.SendDataTo(Player_ID, buff.ToArray());
    }

    private void SerializeInventoryToBuffer (PacketBuffer buff)
    {
        buff.WriteInteger(Items.Length);
        for (int i = 0; i < Items.Length; ++i)
        {
            Item item = Items[i];

            bool invalid = item == null;

            buff.WriteByte(invalid ? (byte)0 : (byte)1);

            if (!invalid) item.GetItemSerialized(buff);
        }
    }

    public void Set_Item (int slot, Item item)
    {
        Items[slot] = item;

        if (!ProgramInfo.isServer) return;
        Player p = (Player) OnlineEntity.OEntities[player_entity_id];

        var Player_ID = p.playerID;

        PacketBuffer buff = new PacketBuffer();

        buff.WriteInteger((int) ServerPackets.GameEvent);
        buff.WriteInteger((int) GameEvents.Player_invetory_set_item);
        bool invalid = item == null;
        buff.WriteByte(invalid ? (byte)0 : (byte)1);
        buff.WriteInteger(slot);
        if (!invalid) item.GetItemSerialized(buff);

        ServerTCP.instance.SendDataTo(Player_ID, buff.ToArray());
    }

}