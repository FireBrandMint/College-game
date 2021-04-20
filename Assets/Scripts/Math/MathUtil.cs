using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MathUtil
{
    public static Vector2 LerpVector (Vector2 a, Vector2 b, float t)
    {
        return new Vector2(Mathf.Lerp(a.x, b.x, t), Mathf.Lerp(a.y, b.y, t));
    }
}
