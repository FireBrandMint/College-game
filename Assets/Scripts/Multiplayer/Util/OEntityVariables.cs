using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Bindings;

public class OEntityVariables
{
    private Dictionary<string, object> variables = new Dictionary<string, object>();

    HashSet<string> variablesChanged = new HashSet<string>();

    private int OEntity_ID;

    public OEntityVariables (int _OEntity_ID, Dictionary<string, float> _variables = null)
    {
        OEntity_ID = _OEntity_ID;

        if (_variables == null) return;
        
        foreach (KeyValuePair<string, float> kv in _variables)
        {
            SetVar(kv.Key, kv.Value);
        }
    }

    public object GetVar (String variableName)
    {
        if (!HasVar(variableName)) return float.MinValue;
        return variables[variableName];
    }

    public object UnsafeGetVar (String variableName)
    {
        return variables[variableName];
    }

    public bool HasVar (String variableName)
    {
        return variables.ContainsKey(variableName);
    }

    public bool VarChanged (string variableName)
    {
        return variablesChanged.Contains(variableName);
    }

    public bool TryGetVarChanged (string variableName, out object l)
    {
        if (variablesChanged.Contains(variableName))
        {
            variablesChanged.Remove(variableName);
            l = variables[variableName];
            return true;
        }
        else l = null;
        return false;
    }

    //Can only be set from serverside.
    public void SetVar (String variableName, object value)
    {
        if (variables.ContainsKey(variableName)) variables[variableName] = value;
        else variables.Add(variableName, value);

        variablesChanged.Add(variableName);

        PacketBuffer buff = OnlineEntity.OEntities[OEntity_ID].GetCurrentBuffer();

        buff.WriteInteger((int)GameEvents.OEntity_Variable);
        buff.WriteInteger(OEntity_ID);
        buff.WriteString(variableName);
        GetVarIntoBuffer(buff, value);
    }

    public void SetVar (PacketBuffer buffer)
    {
        var variableName = buffer.ReadString();

        object value = GetVarFromBuffer(buffer);

        if (variables.ContainsKey(variableName)) variables[variableName] = value;
        else variables.Add(variableName, value);

        variablesChanged.Add(variableName);
    }

    public void GetVariablesPacketsOnBuffer (PacketBuffer buff)
    {
        foreach (KeyValuePair<string, object> kv in variables)
        {
            buff.WriteInteger((int)GameEvents.OEntity_Variable);
            buff.WriteInteger(OEntity_ID);
            buff.WriteString(kv.Key);
            GetVarIntoBuffer(buff, kv.Value);
        }
    }

    private void GetVarIntoBuffer (PacketBuffer buffer, object obj)
    {
        switch (obj)
        {
            case int i: buffer.WriteByte(0); buffer.WriteInteger(i);
            break;
            case float f: buffer.WriteByte(1); buffer.WriteFloat(f);
            break;
            case string s: buffer.WriteByte(2); buffer.WriteString(s);
            break;
            case int[] ir: buffer.WriteByte(3); int size = ir.Length;
            buffer.WriteInteger(size);
            for (int i = 0; i<size; ++i) buffer.WriteInteger(ir[i]);
            break;
            case float[] ir: buffer.WriteByte(4); int sizee = ir.Length;
            buffer.WriteInteger(sizee);
            for (int i = 0; i<sizee; ++i) buffer.WriteFloat(ir[i]);
            break;
            case bool b: buffer.WriteByte(5); buffer.WriteByte(b? (byte)1 : (byte)0);
            break;
        }
    }

    private object GetVarFromBuffer (PacketBuffer buffer)
    {
        switch (buffer.ReadByte())
        {
            case 0: return buffer.ReadInteger();
            case 1: return buffer.ReadFloat();
            case 2: return buffer.ReadString();
            case 3: var f = new int[buffer.ReadInteger()]; for (int i = 0; i< f.Length; ++i) f[i] = buffer.ReadInteger();
            return f;
            case 4: var o = new float[buffer.ReadInteger()]; for (int i = 0; i< o.Length; ++i) o[i] = buffer.ReadFloat();
            return o;
            case 5: return buffer.ReadByte() == 1? true : false;
        }
        return null;
    }

}