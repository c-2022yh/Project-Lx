using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectLx.PlayerRuntime
{
public abstract class PlayerWeaponVisualStateComponent : MonoBehaviour
{
    public void NotifyCombatAction() => Invoke_NotifyCombatAction_0_Core();
    protected abstract void Invoke_NotifyCombatAction_0_Core();
}
}
