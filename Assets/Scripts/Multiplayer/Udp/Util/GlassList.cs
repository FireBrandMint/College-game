using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.Linq;

public class GlassList <T>
{
    Dictionary<string, KeyValuePair<string, T>[]> dictionary = new Dictionary<string, KeyValuePair<string, T>[]>();

    public T this[string s]
    {
        get
        {
            var a = dictionary[s];

            T result = default(T);
            
            for (int i = 0; i < a.Length; ++i)
            {
                var r = a[i];
                if (r.Key == s)
                {
                    result = r.Value;
                    break;
                }
            }

            return result;
        }

        set
        {
            var a = dictionary[s];
            
            for (int i = 0; i < a.Length; ++i)
            {
                if (a[i].Key == s)
                {
                    a[i] = new KeyValuePair<string, T>(s, value);
                    break;
                }
            }
        }
    }

    public string[] Keys
    {
        get => dictionary.Keys.ToArray();
    }

    public void Add (string key, T value)
    {
        if (dictionary.ContainsKey(key))
        {
            var lol = dictionary[key];

            bool oo = true;
            for(int i = 0; i< lol.Length; ++i)
            {
                //going crazy
                if (lol[i].Key == key)
                {
                    oo = false;
                    lol[i] = new KeyValuePair<string, T>(key, value);
                }
            }
            if (oo)
            {
                List<KeyValuePair<string, T>> j = new List<KeyValuePair<string, T>>();
                j.AddRange(lol);
                j.Add(new KeyValuePair<string, T>(key, value));
                dictionary[key] = j.ToArray();
            }
        }
        else
        {
            dictionary.Add(key, new KeyValuePair<string, T>[] {new KeyValuePair<string, T>(key, value)});
        }
    }

    public void Remove (string key)
    {
        KeyValuePair<string, T>[] u;
        if (dictionary.TryGetValue(key, out u))
        {
            var ul = u.ToList();
            for (int i = 0; i < ul.Count; ++i)
            {
                if (ul[i].Key == key)
                {
                    if (ul.Count == 1) dictionary.Remove(key);
                    else
                    {
                        ul.RemoveAt(i);
                        dictionary[key] = ul.ToArray();
                    }
                    break;
                }
            }
        }
    }

    public bool TryGetValue (string key, out T vessel)
    {

        if (dictionary.ContainsKey(key))
        {
            var a = dictionary[key];

            for (int i = 0; i< a.Length; ++i)
            {
                var b = a[i];
                if (b.Key == key)
                {
                    vessel = b.Value;
                    return true;
                }
            }
        }

        vessel = default(T);
        return false;
    }

    public bool ContainsKey (string key)
    {
        if (dictionary.ContainsKey(key))
        {
            bool contains = false;

            var obj = dictionary[key];
            for(int i = 0; i<obj.Length; ++i)
            {
                if (obj[i].Key == key)
                {
                    contains = true;
                    break;
                }
            }
            return contains;
        }
        return false;
    }
}
