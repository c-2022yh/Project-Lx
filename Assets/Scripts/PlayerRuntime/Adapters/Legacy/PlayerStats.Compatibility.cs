using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectLx.Legacy
{
public partial class PlayerStats
{
    protected override float Invoke_RollDamageMultiplier_0_Core() => RollDamageMultiplier();
    protected override OffensiveStats Read_Offense_Core() => Offense;
    protected override DefensiveStats Read_Defense_Core() => Defense;
}
}
