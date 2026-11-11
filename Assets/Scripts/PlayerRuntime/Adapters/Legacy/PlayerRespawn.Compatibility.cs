using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectLx.Legacy
{
public partial class PlayerRespawn
{
    protected override void Invoke_SetRespawnPoint_0_Core(Vector2 newRespawnPosition) => SetRespawnPoint(newRespawnPosition);
}
}
