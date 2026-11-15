using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectLx.PlayerRuntime
{
public abstract class PlayerRespawnComponent : MonoBehaviour
{
    public void SetRespawnPoint(Vector2 newRespawnPosition) => Invoke_SetRespawnPoint_0_Core(newRespawnPosition);
    protected abstract void Invoke_SetRespawnPoint_0_Core(Vector2 newRespawnPosition);
}
}
