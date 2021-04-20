using UnityEngine;
using System;
using System.Collections.Generic;
using Bindings;

public class PlayerControls
{
    public static List<string> Actions = new List<string>()
    {
        "Right", "Left", "Up", "Down"
    };

    HashSet<string> ActionPressed = new HashSet<string>();

    HashSet<string> ActionJustPressed = new HashSet<string>();

    HashSet<string> ActionJustReleased = new HashSet<string>();

    public bool IsMain = false;

    bool justReceived = false;
    
    public void tick ()
    {
        var ac = Actions.Count;

        if (IsMain)
        {
            for (int i = 0; i < ac; ++i)
            {
                var action = Actions[i];

                ActionJustPressed.Clear();
                ActionJustReleased.Clear();
                //ActionPressed.Clear();

                if (Input.GetButtonDown(action))
                {
                    ActionJustPressed.Add(action);
                    ActionPressed.Add(action);
                }
                if (Input.GetButtonUp(action))
                {
                    ActionJustReleased.Add(action);
                    ActionPressed.Remove(action);
                }
            }

            if (!ProgramInfo.isServer && ActionJustPressed.Count!=0 && ActionJustReleased.Count!=0) ClientTCP.instance.SendData(GetInputsData());
        }
        else
        {
            if (justReceived) justReceived = false;
            else
            {
                ActionJustPressed.Clear();
                ActionJustReleased.Clear();
            }
        }
    }

    public byte[] GetInputsData ()
    {
        using (PacketBuffer buffer = new PacketBuffer())
        {
            buffer.WriteInteger((int)ClientPackets.ControlInputs);

            buffer.WriteInteger(ActionJustPressed.Count);
            foreach (string act in ActionJustPressed) buffer.WriteString(act);

            buffer.WriteInteger(ActionJustReleased.Count);
            foreach (string act in ActionJustReleased) buffer.WriteString(act);

            return buffer.ToArray();
        }
    }

    public bool IsActionPressed (string action) => ActionPressed.Contains(action);

    public bool IsActionJustPressed (string action) => ActionJustPressed.Contains(action);

    public bool IsActionJustReleased (string action) => ActionJustReleased.Contains(action);

    public Vector2 ActionsLocation;

    public void PutSentActions (PacketBuffer buffer)
    {
        try
        {
            ActionJustPressed.Clear();
            ActionJustReleased.Clear();
            //ActionPressed.Clear();

            int loopJustPressed = buffer.ReadInteger();
            for (int i = 0; i < loopJustPressed; ++i)
            {
                var action = buffer.ReadString();
                
                if (!ActionJustPressed.Contains(action)) ActionJustPressed.Add(action);
                if (!ActionPressed.Contains(action)) ActionPressed.Add(action);
            }

            int loopJustReleased = buffer.ReadInteger();
            for (int i = 0; i < loopJustReleased; ++i)
            {
                var action = buffer.ReadString();

                if (!ActionJustReleased.Contains(action)) ActionJustReleased.Add(action);
                if (ActionPressed.Contains(action)) ActionPressed.Remove(action);
            }

            if (!ProgramInfo.isServer) ActionsLocation = new Vector2(buffer.ReadFloat(), buffer.ReadFloat());

            justReceived = true;

            if (OnInputsReceived!=null) OnInputsReceived.Invoke();

            /*int loopPressed = buffer.ReadInteger();
            for (int i = 0; i < loopPressed; ++i)
            {
                ActionPressed.Add(buffer.ReadString());
            }
            */
            

        }
        catch
        {

        }
    }

    public Action OnInputsReceived = null;
}