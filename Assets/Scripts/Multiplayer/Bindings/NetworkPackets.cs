namespace Bindings
{
    //get sent from server to client
    public enum ServerPackets
    {
        SConnectionOK = 1001,
        GameEvent = 1002,
        PingMeasure = 1003,
        PingConfirmation = 1004
    }

    public enum GameEvents
    {
        Teleport = 1,
        Move = 2,
        Anim = 3,
        OEntity_Initialized = 4,
        OEntity_Despawned = 5,
        OEntity_Variable = 6,
        Set_Player = 7,
        Player_inventory = 9,
        Player_invetory_set_item = 10,
        Player_Changed_Areas = 8, 
        OEntity_SetAnimatorBool = 11,
        OEntity_SetAnimatorInteger = 12,
        OEntity_SetAnimatorFloat = 13,
        OEntity_SetAnimation = 14,
        OEntity_Speed = 15,
        Player_Input = 16
    }

    //get sent from client to server
    public enum ClientPackets
    {
        CThankYou = 2001,
        ControlInputs = 2002,
        EntityUnloaded = 2003,
        NameRequest = 2004,
        NameItRequest = 2005
    }
}