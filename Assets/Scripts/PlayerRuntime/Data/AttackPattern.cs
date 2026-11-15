using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


[System.Serializable]
public class AttackPattern //공격 패턴
{
    public string attackName;

    [Header("Audio")]
    public SoundCue attackSound = new SoundCue();
    public SoundCue impactSound = new SoundCue();
    public ImpactSoundMode impactMode = ImpactSoundMode.EveryHit;

    [Header("Timing")]
    public float startupTime = 0.05f;   //선딜
    public float activeTime = 0.1f;     //공격판정 시간
    public float recoveryTime = 0.1f;   //후딜

    [Min(0f), Tooltip("Allow horizontal movement during the final seconds of ground attack recovery. Clamped to recovery time. 0 keeps movement locked; jump and next attack remain locked until recovery ends.")]
    public float recoveryMovementTime = 0f;

    [Header("Combat")]
    public AttackDamageSpec damageSpec; //고유 데미지 값  

    [Header("Follow-up Hit")]
    [Tooltip("Each target touched by this trail receives a second hit, without needing to overlap the trail again.")]
    public bool enableFollowUpHit;
    [Min(0f)] public float followUpHitDelay = 0.06f;
    public AttackDamageSpec followUpDamageSpec;
    public SoundCue followUpImpactSound = new SoundCue();

    [Header("Effect")] //공격 이펙트 프리펩 설정
    public GameObject attackEffectPrefab;
    public Vector2 effectOffset = new Vector2(0.8f, 0f);
    public Vector3 effectScale = Vector3.one;
    public float effectRotationZ = 0f;
    public float effectDuration = 0.12f;

    [Header("Effect Follow")] //이펙트가 플레이어를 얼마나 따라올 지 (공중공격 한정)
    public bool followPlayer = false;
    public float followDuration = 0.1f;

}
