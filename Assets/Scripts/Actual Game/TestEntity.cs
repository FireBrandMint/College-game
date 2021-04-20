using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestEntity : Player
{
    protected override void ServerTick()
    {
        base.ServerTick();

        Vector2 pos = transform.position;
        if (controls.IsActionPressed("Right")) pos.x += 10f;
        if (controls.IsActionPressed("Left")) pos.x -= 10f;

        transform.position = pos;
    }
}
