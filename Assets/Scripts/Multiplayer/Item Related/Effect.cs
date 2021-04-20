using Bindings;

public class Effect
{
    protected virtual int EffectType () => 0;
    protected int level;

    public Effect (int effect_level)
    {
        level = effect_level;
    }

    public void UseOn (OnlineEntity OEntity)
    {
        if (ProgramInfo.isServer) _UseOn(OEntity);
    }

    public virtual void _UseOn (OnlineEntity OEntity)
    {

    }

    public void SerializeEffect (PacketBuffer buffer)
    {
        buffer.WriteInteger(EffectType());
        buffer.WriteInteger(level);
    }

}