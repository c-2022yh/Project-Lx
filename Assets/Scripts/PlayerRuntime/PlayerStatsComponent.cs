using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectLx.PlayerRuntime
{
public abstract class PlayerStatsComponent : MonoBehaviour
{
    public float RollDamageMultiplier() => Invoke_RollDamageMultiplier_0_Core();
    protected abstract float Invoke_RollDamageMultiplier_0_Core();
    public OffensiveStats Offense { get => Read_Offense_Core(); }
    protected abstract OffensiveStats Read_Offense_Core();
    public DefensiveStats Defense { get => Read_Defense_Core(); }
    protected abstract DefensiveStats Read_Defense_Core();
}
}
