using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectLx.Legacy
{
public partial class PlayerKillTracker
{
    protected override void Add_OnEnemyKilled_Core(Action<GameObject> handler) => OnEnemyKilled += handler;
    protected override void Remove_OnEnemyKilled_Core(Action<GameObject> handler) => OnEnemyKilled -= handler;
    protected override void Invoke_NotifyEnemyKilled_1_Core(GameObject killedEnemy) => NotifyEnemyKilled(killedEnemy);
}
}
