using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Bindings;

public class Item : ItemEssential
{
    public virtual int ItemType () => 0;

    protected Effect[] effects;

    public static Item DeserializeItem (PacketBuffer buffer)
    {
        Item item = null;

        switch (buffer.ReadInteger())
        {
            case 0: item = new Item(buffer);
            break;
        }

        return item;
    }

    public Item (PacketBuffer buffer)
    {
        //This constructor reads the serialized item in the buffer to create this item.
        DesserializeIntoThisItem(buffer);
    }

    protected virtual void DesserializeIntoThisItem (PacketBuffer buffer)
    {

    }

    public Item (Effect[] item_effects)
    {
        effects = item_effects;
    }

    public void Use (Player p)
    {
        if (ProgramInfo.isServer) _Use(p);
    }

    public virtual void _Use (Player p)
    {

    }

    public virtual void GetItemSerialized (PacketBuffer buffer)
    {
        buffer.WriteInteger(ItemType());
    }
}
