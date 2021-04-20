using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Bindings;

public class OnlineEntity : MonoBehaviour
{

    //static
    private static int entityNextIndex = int.MinValue + 1;
    public static List<int> available_indexes = new List<int>();
    public static Dictionary<int, OnlineEntity> OEntities = new Dictionary<int, OnlineEntity>();
    public static AreaRegistry Areas = new AreaRegistry();

    //end

    public bool InitializesUponSpawing = false;

    public Animator animator;

    public bool DespawnOnClient = false;

    public string PrefabPath;

    protected List<Vector2> past_positions = new List<Vector2>();

    public OEntityVariables variables;

    public string Current_Area = ".";

    public int entityID = int.MinValue;

    protected Action tick;
    protected void Start()
    {
        if (DespawnOnClient)
        {
            Destroy(gameObject);
            return;
        }

        //Code that registers entity id.

        if (ProgramInfo.isServer)
        {
            if (available_indexes.Count == 0)
            {
                entityID = entityNextIndex;
                ++entityNextIndex;
            }
            else
            {
                int lastAvailabeIndex = available_indexes.Count - 1;

                entityID = available_indexes[lastAvailabeIndex];
                available_indexes.RemoveAt(lastAvailabeIndex);
            }

            OEntities.Add(entityID, this);

            if (InitializesUponSpawing) Initialize(Current_Area, transform.position, transform.localScale, transform.eulerAngles.z);
        }

        //END

        tick = ProgramInfo.isServer ? new Action(ServerTick) : new Action(ClientTick);
    }

    private bool initialized = false;

    public void Initialize (string area, Vector2 position, Vector2 scale, float eureulerZ_rotation, int ID = -1)
    {
        initialized = true;

        if(ProgramInfo.isServer)
        {
            NotifyObjectSpawned(GetCurrentBuffer(), area, position, scale, eureulerZ_rotation);
        }
        else
        {
            entityID = ID;
            OEntities.Add(entityID, this);
        }

        Current_Area = area;

        Areas.RegisterEntity(Current_Area, this);

        transform.eulerAngles = new Vector3(0,0,eureulerZ_rotation);

        variables = new OEntityVariables(entityID);

        if (ProgramInfo.isServer) ServerInit();
        else ClientInit();
        CommonInit();
    }

    public void NotifyObjectSpawned (PacketBuffer buf, string area, Vector2 position, Vector2 scale, float eureulerZ_rotation)
    {
        buf.WriteInteger((int) GameEvents.OEntity_Initialized);
        buf.WriteString(PrefabPath);
        buf.WriteInteger(entityID);
        buf.WriteString(area);
        buf.WriteFloat(position.x);
        buf.WriteFloat(position.y);
        buf.WriteFloat(scale.x);
        buf.WriteFloat(scale.y);
        buf.WriteFloat(eureulerZ_rotation);
    }

    // Update is called once per frame
    protected void Update()
    {
        CommonTick();
        tick.Invoke();
    }

    protected virtual void ClientInit ()
    {

    }

    protected virtual void CommonInit ()
    {

    }

    protected virtual void ServerInit ()
    {

    }

    protected virtual void ClientTick ()
    {

    }
    protected virtual void ServerTick ()
    {
        //Necessary for prediction

        var ppCount = past_positions.Count;

        if (ppCount == 13)
        {
            for(int i = 0; i < ppCount - 1; ++i)
            {
                past_positions[i] = past_positions[i+1];
            }

            past_positions[ppCount - 1] = transform.position;
        }
        else past_positions.Add(transform.position);
    }

    protected virtual void CommonTick () {}

    public PacketBuffer GetCurrentBuffer () => Areas.GetAreaGameEventBuffer(Current_Area);

    public PacketBuffer GetCurrentUDPBuffer () => Areas.GetAreaUDPGameEventBuffer(Current_Area);

    public List<OnlineEntity> GetOEntitiesInCurrentArea () => Areas.GetAreaOEntities(Current_Area);

    //static voids

    public void AllServerPredictionActivate (int pingTicks)
    {
        int steps_back = pingTicks;

        foreach(OnlineEntity OEntity in GetOEntitiesInCurrentArea())
        {
            OEntity.ServerPredictionStepActivate(steps_back);
        }
    }

    public void AllServerPredictionEnd ()
    {
        foreach(OnlineEntity OEntity in GetOEntitiesInCurrentArea())
        {
            OEntity.ServerPredictionStepEnd();
        }
    }

    public void Teleport_server (Vector2 pos, bool tcp = true)
    {
        transform.position = pos;

        PacketBuffer GES;

        if (tcp) GES = GetCurrentBuffer();
        else GES = GetCurrentUDPBuffer();

        GES.WriteInteger((int) GameEvents.Teleport);
        GES.WriteInteger(entityID);
        GES.WriteFloat(pos.x);
        GES.WriteFloat(pos.x);
    }

    public void Teleport_client (PacketBuffer buffer)
    {
        transform.position = new Vector2(buffer.ReadFloat(), buffer.ReadFloat());
    }

    public virtual void Move_server (bool position, bool rotation, bool scale, bool tcp = false)
    {
        PacketBuffer GES;

        if (tcp) GES = GetCurrentBuffer();
        else GES = GetCurrentUDPBuffer();

        GES.WriteInteger((int) GameEvents.Move);
        GES.WriteInteger(entityID);
        if (position)
        {
            GES.WriteByte(1);
            var posi = transform.position;
            GES.WriteFloat(posi.x);
            GES.WriteFloat(posi.y);
        }else GES.WriteByte(0);

        if (rotation)
        {
            GES.WriteByte(1);
            GES.WriteFloat(transform.eulerAngles.z);
        }else GES.WriteByte(0);

        if (scale)
        {
            GES.WriteByte(1);
            var scle = transform.localScale;
            GES.WriteFloat(scle.x);
            GES.WriteFloat(scle.y);
        }
    }

    public virtual void Move_client (PacketBuffer buffer)
    {
        if (buffer.ReadByte() == 1)
        {
            transform.position = new Vector2(buffer.ReadFloat(), buffer.ReadFloat());
        }

        if (buffer.ReadByte() == 1)
        {
            transform.eulerAngles = new Vector3(0f,0f,buffer.ReadFloat());
        }

        if (buffer.ReadByte() == 1)
        {
            transform.localScale = new Vector2(buffer.ReadFloat(), buffer.ReadFloat());
        }
    }

    public void SetAnimation (string anim, bool modify = true)
    {
        if (animator == null) return;

        if (modify)
        {
            if (ProgramInfo.isServer)
            animator.Play(anim);
            else
            {
                if (this is Player p)
                {
                    if (p.isControlledPlayer)
                    {
                        var a = NCUtil.GetClip(animator, anim);
                        animator.Play(anim,0, Mathf.Clamp(((float)p.ping)*0.5f*ProgramInfo.FrameTime, 1f, a.length)/a.length);
                    }
                    else
                    {
                        var a = NCUtil.GetClip(animator, anim);
                        var cop = Player.controlledPlayer;
                        int copp = cop != null? cop.ping: 0;
                        animator.Play(anim,0, Mathf.Clamp((float)(p.ping + copp)*0.5f/60f, 1f, a.length)/a.length);
                    }
                }
                else
                {
                    var a = NCUtil.GetClip(animator, anim);
                    var cop = Player.controlledPlayer;
                    int copp = cop != null? cop.ping: 0;
                    animator.Play(anim,0, Mathf.Clamp((float)(copp)*0.5f/60f, 0f, a.length)* (1f/a.length));
                }
            }
        }

        if (!ProgramInfo.isServer) return;

        var buff = GetCurrentBuffer();

        buff.WriteInteger((int)GameEvents.OEntity_SetAnimation);
        buff.WriteInteger(entityID);
        buff.WriteString(anim);
    }

    public void SetAnimatorFloat (string vari, float fl, bool modify = true)
    {
        if (animator == null) return;

        if (modify) animator.SetFloat(vari, fl);

        if (!ProgramInfo.isServer) return;

        var buff = GetCurrentBuffer();

        buff.WriteInteger((int)GameEvents.OEntity_SetAnimatorFloat);
        buff.WriteInteger(entityID);
        buff.WriteString(vari);
        buff.WriteFloat(fl);
    }

    public void SetAnimatorInteger (string vari, int integer, bool modify = true)
    {
        if (animator == null) return;

        if (modify) animator.SetInteger(vari, integer);

        if (!ProgramInfo.isServer) return;

        var buff = GetCurrentBuffer();

        buff.WriteInteger((int)GameEvents.OEntity_SetAnimatorInteger);
        buff.WriteInteger(entityID);
        buff.WriteString(vari);
        buff.WriteInteger(integer);
    }

    public void SetAnimatorBool (string vari, bool thebool, bool modify = true)
    {
        if (animator == null) return;

        if (modify) animator.SetBool(vari, thebool);

        if (!ProgramInfo.isServer) return;

        var buff = GetCurrentBuffer();

        buff.WriteInteger((int)GameEvents.OEntity_SetAnimatorBool);
        buff.WriteInteger(entityID);
        buff.WriteString(vari);
        buff.WriteByte(thebool ? (byte)1 : (byte)0);
    }

    public void SetAnimationSpeed (float speed, bool modify = true, bool tcp = true)
    {
        if (modify) animator.speed = speed;

        if (!ProgramInfo.isServer) return;

        PacketBuffer buff;

        if (tcp) buff = GetCurrentBuffer();
        else buff = GetCurrentUDPBuffer();

        buff.WriteInteger((int)GameEvents.OEntity_Speed);
        buff.WriteInteger(entityID);
        buff.WriteFloat(speed);
    }

    //Code for prediction

    protected Vector2 GetbackPosition = Constants.DefaultVector;

    public void ServerPredictionStepActivate (int number)
    {
        var gbn = past_positions.Count - number;

        if (number == 0 || gbn < 0) return;

        GetbackPosition = transform.position;

        transform.position = past_positions[gbn];
    }

    public void ServerPredictionStepEnd ()
    {
        if (GetbackPosition == Constants.DefaultVector) return;

        transform.position = GetbackPosition;

        GetbackPosition = Constants.DefaultVector;
    }

    public void Despawn ()
    {
        Destroy(gameObject);
    }

    protected void OnDestroy ()
    {
        tick = null;

        if (!initialized) return;

        if (initialized && ProgramInfo.isServer && Areas.AreaExists(Current_Area))
        {
            OnDespawn_server();

            GetCurrentBuffer().WriteInteger((int) GameEvents.OEntity_Despawned);
            GetCurrentBuffer().WriteInteger(entityID);
        }
        else if (Areas.AreaExists(Current_Area)) OnDespawn_client();

        OnDespawn_common();

        if (OEntities.ContainsKey(entityID)) OEntities.Remove(entityID);

        Areas.UnregisterEntity(Current_Area, this);

        if (ProgramInfo.isServer) available_indexes.Add(entityID);
    }

    protected virtual void OnDespawn_server ()
    {

    }

    protected virtual void OnDespawn_client ()
    {

    }

    protected virtual void OnDespawn_common () {}
}
