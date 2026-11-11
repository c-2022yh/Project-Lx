using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectLx.PlayerRuntime
{
public abstract class PlayerKillTrackerComponent : MonoBehaviour
{
    public event Action<GameObject> OnEnemyKilled { add => Add_OnEnemyKilled_Core(value); remove => Remove_OnEnemyKilled_Core(value); }
    protected abstract void Add_OnEnemyKilled_Core(Action<GameObject> handler);
    protected abstract void Remove_OnEnemyKilled_Core(Action<GameObject> handler);
    public void NotifyEnemyKilled(GameObject killedEnemy) => Invoke_NotifyEnemyKilled_1_Core(killedEnemy);
    protected abstract void Invoke_NotifyEnemyKilled_1_Core(GameObject killedEnemy);
}
}
