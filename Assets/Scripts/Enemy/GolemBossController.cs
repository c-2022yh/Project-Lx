using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class GolemBossAttack
{
    [Min(0.1f)] public float warningTime = 1.1f;
    [Min(0.02f)] public float activeTime = 0.15f;
    [Min(0.1f)] public float recoveryTime = 1.4f;
    [Min(0f)] public float cooldown = 3f;
    [Tooltip("World units. This exact rectangle is displayed and queried for damage.")]
    public Vector2 size = new Vector2(4f, 1.4f);
    public AttackDamageSpec damage = new AttackDamageSpec { damageMultiplier = 2f };
    public GolemBossMotion motion = new GolemBossMotion();
    public GameObject impactPrefab;
    public SoundCue warningSound = new SoundCue();
    public SoundCue impactSound = new SoundCue();
}

// Stationary ground boss. Enemy/EnemyHealth remain the sole owners of incoming damage,
// rewards and death. This component owns only encounter and pattern scheduling.
[RequireComponent(typeof(Enemy), typeof(EnemyHealth), typeof(EnemyStats))]
[RequireComponent(typeof(Rigidbody2D), typeof(GolemBossVisual))]
[RequireComponent(typeof(CapsuleCollider2D))]
public class GolemBossController : MonoBehaviour
{
    private enum State { Waiting, Prepare, Active, Recovery, Summon, PhaseChange }
    private enum Pattern { Slam, Crush, Rockfall }

    [Header("Encounter")]
    [SerializeField] private bool autoBegin = true;
    [SerializeField, Min(1f)] private float activationRange = 18f;
    [SerializeField, Min(0f)] private float firstAttackDelay = 1f;
    [SerializeField, Min(0.1f)] private float patternInterval = 0.7f;
    [Tooltip("Horizontal limits relative to the boss spawn X. Keep the arena mostly flat.")]
    [SerializeField] private Vector2 arenaX = new Vector2(-10f, 10f);
    [SerializeField] private LayerMask groundLayers = 1 << 6;
    [SerializeField] private LayerMask targetLayers = 1 << 7;

    [Header("Front slam")]
    [SerializeField] private GolemBossAttack slam = new GolemBossAttack();
    [SerializeField, Min(0f)] private float slamRange = 5f;
    [SerializeField, Min(0f)] private float slamForwardOffset = 3.2f;
    [Header("Wide ground crush")]
    [SerializeField] private GolemBossAttack crush = new GolemBossAttack
    {
        warningTime = 1.5f, recoveryTime = 1.8f, cooldown = 6f,
        size = new Vector2(9f, 1.3f), damage = new AttackDamageSpec { damageMultiplier = 2.5f }
    };
    [Header("Sequential rockfall")]
    [SerializeField] private GolemBossAttack rockfall = new GolemBossAttack
    {
        warningTime = 1.2f, recoveryTime = 1.6f, cooldown = 5f,
        size = new Vector2(2.2f, 5f)
    };
    [SerializeField, Range(1, 6)] private int rockCount = 3;
    [SerializeField, Min(0.05f)] private float rockInterval = 0.45f;
    [SerializeField] private Sprite rockSprite;
    [SerializeField, Min(1f)] private float rockDropHeight = 7f;

    [Header("Phase 2: full warnings, two-pattern chains")]
    [SerializeField, Range(0.05f, 0.95f)] private float phaseTwoHealth = 0.5f;
    [SerializeField, Min(0.1f)] private float phaseChangeTime = 1.6f;
    [SerializeField, Min(0.1f)] private float chainRecovery = 0.45f;

    [Header("Summons: once per health threshold")]
    [SerializeField] private GameObject[] minionPrefabs;
    [SerializeField] private float[] summonHealthThresholds = { 0.7f, 0.35f };
    [SerializeField, Min(1)] private int minionsPerWave = 2;
    [SerializeField, Min(1)] private int maxLivingMinions = 2;
    [SerializeField, Min(0.1f)] private float summonWarningTime = 1.8f;
    [SerializeField, Min(0.1f)] private float afterSummonRest = 2f;
    [SerializeField] private Vector2[] summonOffsets = { new Vector2(-6f, 0f), new Vector2(6f, 0f) };
    [Tooltip("Optional world-space anchors. Empty uses the offsets above.")]
    [SerializeField] private Transform[] summonPoints;
    [SerializeField, Min(0f)] private float summonLift = 0.08f;
    [SerializeField] private bool despawnMinionsOnEncounterEnd = true;
    public UnityEvent onEncounterStarted = new UnityEvent();
    public UnityEvent onEncounterEnded = new UnityEvent();
    public UnityEvent onPhaseTwo = new UnityEvent();

    [Header("Warning art (empty = solid rectangle with border)")]
    [SerializeField] private Sprite indicatorFill;
    [SerializeField] private Sprite indicatorBorder;
    [SerializeField] private Color attackWarningColor = new Color(1f, 0.08f, 0.04f, 0.3f);
    [SerializeField] private Color summonWarningColor = new Color(0.65f, 0.2f, 1f, 0.3f);

    private readonly List<GolemBossArea> warnings = new List<GolemBossArea>();
    private readonly List<float> warningDueTimes = new List<float>();
    private readonly List<GameObject> minions = new List<GameObject>();
    private readonly List<GameObject> summonChoices = new List<GameObject>();
    private readonly RaycastHit2D[] groundHits = new RaycastHit2D[16];
    private EnemyHealth health;
    private Collider2D bodyCollider;
    private EnemyStats stats;
    private GolemBossVisual visual;
    private PlayerHealth player;
    private State state;
    private Pattern pattern, lastPattern;
    private GolemBossAttack current;
    private bool engaged, armed, phaseTwo, hasLastPattern;
    private bool[] usedSummons;
    private float elapsed, nextActionTime, nextSearchTime, nextRockTime, spawnX;
    private readonly float[] readyTimes = new float[3];
    private int facing = 1, rocksMarked, chainRemaining;

    public bool IsEngaged => engaged;
    public bool IsPhaseTwo => phaseTwo;
    public float PhaseTwoHealth => phaseTwoHealth;
    public int LivingMinionCount
    {
        get
        {
            minions.RemoveAll(item => item == null);
            int count = 0;
            foreach (GameObject item in minions)
                if (item.activeInHierarchy && item.GetComponent<EnemyHealth>() != null && !item.GetComponent<EnemyHealth>().IsDead) count++;
            return count;
        }
    }

    private void Awake()
    {
        health = GetComponent<EnemyHealth>();
        bodyCollider = GetComponent<Collider2D>();
        stats = GetComponent<EnemyStats>();
        visual = GetComponent<GolemBossVisual>();
    }

    private void OnEnable()
    {
        state = State.Waiting;
        engaged = phaseTwo = hasLastPattern = false;
        armed = autoBegin;
        chainRemaining = 0;
        spawnX = transform.position.x;
        nextSearchTime = 0f;
        usedSummons = new bool[summonHealthThresholds.Length];
        Array.Clear(readyTimes, 0, readyTimes.Length);
        visual.SetPose(GolemBossPose.Idle);
    }

    private void OnDisable() => StopEncounter();

    public void BeginEncounter()
    {
        if (!isActiveAndEnabled || health.IsDead || engaged) return;
        armed = engaged = true;
        nextActionTime = Time.time + firstAttackDelay;
        onEncounterStarted.Invoke();
    }

    public void StopEncounter()
    {
        bool wasEngaged = engaged;
        engaged = armed = false;
        CancelWarnings();
        chainRemaining = 0;
        state = State.Waiting;
        visual.SetPose(GolemBossPose.Idle);
        if (despawnMinionsOnEncounterEnd)
        {
            foreach (GameObject minion in minions) if (minion != null) Destroy(minion);
            minions.Clear();
        }
        if (wasEngaged) onEncounterEnded.Invoke();
    }

    private void FixedUpdate()
    {
        if (health.IsDead) { if (engaged) StopEncounter(); return; }
        FindPlayer();
        if (player == null || player.IsDead || !player.isActiveAndEnabled)
        {
            if (engaged)
            {
                CancelWarnings();
                state = State.Waiting;
                chainRemaining = 0;
                nextActionTime = Time.time + firstAttackDelay;
                visual.SetPose(GolemBossPose.Idle);
            }
            return;
        }
        if (!engaged)
        {
            if (armed && Vector2.Distance(player.transform.position, transform.position) <= activationRange)
                BeginEncounter();
            return;
        }

        elapsed += Time.fixedDeltaTime;
        switch (state)
        {
            case State.Waiting:
                visual.SetPose(GolemBossPose.Idle, 0f, facing);
                if (Time.time >= nextActionTime) ChooseNextAction();
                break;
            case State.Prepare:
                TickAttack();
                break;
            case State.Active:
                visual.SetPose(GolemBossPose.Strike, elapsed / current.activeTime, facing, current.motion.strike, pattern == Pattern.Slam);
                if (elapsed >= current.activeTime) Enter(State.Recovery);
                break;
            case State.Recovery:
                float duration = chainRemaining > 0 ? chainRecovery : current.recoveryTime;
                visual.SetPose(GolemBossPose.Recovery, elapsed / Mathf.Max(0.1f, duration), facing, current.motion.recovery, pattern == Pattern.Slam);
                if (elapsed >= duration)
                {
                    Enter(State.Waiting);
                    nextActionTime = Time.time + (chainRemaining > 0 ? 0f : patternInterval);
                }
                break;
            case State.Summon:
                TickSummon();
                break;
            case State.PhaseChange:
                visual.SetPose(GolemBossPose.PhaseChange, elapsed / Mathf.Max(0.1f, phaseChangeTime), facing);
                if (elapsed >= phaseChangeTime) { Enter(State.Waiting); nextActionTime = Time.time + patternInterval; }
                break;
        }
    }

    private void FindPlayer()
    {
        if (player != null && player.isActiveAndEnabled && !player.IsDead) return;
        if (Time.time < nextSearchTime) return;
        nextSearchTime = Time.time + 0.5f;
        player = FindAnyObjectByType<PlayerHealth>();
    }

    private void ChooseNextAction()
    {
        float hp = health.MaxHp > 0f ? health.CurrentHp / health.MaxHp : 0f;
        if (!phaseTwo && hp <= phaseTwoHealth)
        {
            phaseTwo = true;
            chainRemaining = 0;
            Enter(State.PhaseChange);
            onPhaseTwo.Invoke();
            return;
        }
        for (int i = 0; i < usedSummons.Length; i++)
        {
            if (!usedSummons[i] && hp <= summonHealthThresholds[i] && LivingMinionCount < maxLivingMinions)
            {
                if (StartSummon()) { usedSummons[i] = true; chainRemaining = 0; return; }
            }
        }

        float distance = Mathf.Abs(player.transform.position.x - transform.position.x);
        var choices = new List<Pattern>();
        for (int i = 0; i < 3; i++)
        {
            if (Time.time < readyTimes[i]) continue;
            if (i == (int)Pattern.Slam && distance > slamRange) continue;
            if (i == (int)Pattern.Crush && distance > crush.size.x * 0.5f + 0.5f) continue;
            choices.Add((Pattern)i);
        }
        if (choices.Count == 0) { nextActionTime = Time.time + 0.2f; return; }
        if (hasLastPattern && choices.Count > 1) choices.Remove(lastPattern);
        Pattern next = choices[UnityEngine.Random.Range(0, choices.Count)];
        if (chainRemaining > 0) chainRemaining--;
        else if (phaseTwo) chainRemaining = 1;
        StartPattern(next);
    }

    private void StartPattern(Pattern next)
    {
        CancelWarnings();
        pattern = lastPattern = next;
        hasLastPattern = true;
        current = next == Pattern.Slam ? slam : next == Pattern.Crush ? crush : rockfall;
        facing = player.transform.position.x >= transform.position.x ? 1 : -1;
        rocksMarked = 0;
        nextRockTime = 0f;
        Enter(State.Prepare);
        GameAudio.Play(current.warningSound, transform.position, transform);
        if (next != Pattern.Rockfall)
        {
            float x = transform.position.x + (next == Pattern.Slam ? facing * slamForwardOffset : 0f);
            if (TryGround(x, FeetY + 1f, out Vector2 ground))
                AddWarning(ground, current.size, current.warningTime, attackWarningColor);
        }
        else MarkRock();
    }

    private float FeetY => bodyCollider.bounds.min.y;

    private void TickAttack()
    {
        visual.SetPose(GolemBossPose.Prepare, elapsed / Mathf.Max(0.1f, current.warningTime), facing, current.motion.prepare, pattern == Pattern.Slam);
        if (pattern == Pattern.Rockfall && rocksMarked < rockCount && elapsed >= nextRockTime) MarkRock();
        for (int i = 0; i < warnings.Count; i++)
        {
            GolemBossArea area = warnings[i];
            if (area == null || area.IsCommitted) continue;
            area.SetProgress(1f - (warningDueTimes[i] - elapsed) / Mathf.Max(0.1f, current.warningTime));
            if (elapsed >= warningDueTimes[i])
            {
                area.Commit(DamageInfo.Create(stats.Offense, current.damage, gameObject), targetLayers,
                    current.activeTime, current.impactPrefab);
                GameAudio.Play(current.impactSound, area.Center);
            }
        }
        float finishTime = current.warningTime;
        foreach (float due in warningDueTimes) finishTime = Mathf.Max(finishTime, due);
        if (elapsed >= finishTime && (pattern != Pattern.Rockfall || rocksMarked >= rockCount))
        {
            readyTimes[(int)pattern] = Time.time + current.activeTime + current.recoveryTime + Mathf.Max(0f, current.cooldown);
            Enter(State.Active);
            visual.SetPose(GolemBossPose.Strike, 0f, facing, current.motion.strike, pattern == Pattern.Slam);
        }
    }

    private void MarkRock()
    {
        float x = Mathf.Clamp(player.transform.position.x, spawnX + arenaX.x + rockfall.size.x / 2f,
            spawnX + arenaX.y - rockfall.size.x / 2f);
        Collider2D playerBody = player.GetComponent<Collider2D>();
        float top = playerBody != null ? playerBody.bounds.min.y + 0.25f : player.transform.position.y;
        if (TryGround(x, top, out Vector2 ground))
        {
            AddWarning(ground, current.size, elapsed + current.warningTime, attackWarningColor);
            warnings[warnings.Count - 1].SetFallingRock(rockSprite, rockDropHeight);
        }
        rocksMarked++;
        nextRockTime = elapsed + Mathf.Max(0.05f, rockInterval);
    }

    private void AddWarning(Vector2 ground, Vector2 size, float due, Color color)
    {
        warnings.Add(GolemBossArea.Create(ground + Vector2.up * size.y * 0.5f, size, color, indicatorFill, indicatorBorder));
        warningDueTimes.Add(due);
    }

    private bool TryGround(float x, float top, out Vector2 point)
    {
        var filter = new ContactFilter2D();
        filter.SetLayerMask(groundLayers);
        filter.useTriggers = false;
        int count = Physics2D.Raycast(new Vector2(x, top), Vector2.down, filter, groundHits, 15f);
        for (int i = 0; i < count; i++)
        {
            if (groundHits[i].normal.y < 0.6f) continue;
            point = groundHits[i].point;
            return true;
        }
        point = default;
        return false;
    }

    private bool StartSummon()
    {
        if (minionPrefabs == null || minionPrefabs.Length == 0) return false;
        var valid = new List<GameObject>();
        foreach (GameObject prefab in minionPrefabs)
            if (prefab != null && prefab.GetComponent<EnemyHealth>() != null && prefab.GetComponent<GolemBossController>() == null)
                valid.Add(prefab);
        if (valid.Count == 0) return false;
        CancelWarnings();
        summonChoices.Clear();
        int count = Mathf.Min(minionsPerWave, maxLivingMinions - LivingMinionCount);
        int pointCount = summonPoints != null && summonPoints.Length > 0 ? summonPoints.Length : summonOffsets.Length;
        for (int i = 0; i < Mathf.Min(count, pointCount); i++)
        {
            Vector2 position;
            if (summonPoints != null && summonPoints.Length > 0)
            {
                if (summonPoints[i] == null) continue;
                position = summonPoints[i].position;
            }
            else position = (Vector2)transform.position + summonOffsets[i];
            position.x = Mathf.Clamp(position.x, spawnX + arenaX.x, spawnX + arenaX.y);
            if (!TryGround(position.x, position.y + 3f, out Vector2 ground)) continue;
            GameObject prefab = valid[i % valid.Count];
            Vector2 size = MinionSize(prefab);
            // Reject walls or low ceilings before showing a spawn marker.
            if (Physics2D.OverlapBox(ground + Vector2.up * (size.y / 2f + 0.05f), size * 0.9f, 0f, groundLayers)) continue;
            AddWarning(ground, new Vector2(size.x, 0.2f), summonWarningTime, summonWarningColor);
            summonChoices.Add(prefab);
        }
        if (warnings.Count == 0) return false;
        Enter(State.Summon);
        return true;
    }

    private static Vector2 MinionSize(GameObject prefab)
    {
        Collider2D collider = prefab.GetComponent<Collider2D>();
        Vector2 size = collider is CapsuleCollider2D capsule ? capsule.size :
            collider is BoxCollider2D box ? box.size : Vector2.one * 2f;
        return Vector2.Scale(size, new Vector2(Mathf.Abs(prefab.transform.localScale.x), Mathf.Abs(prefab.transform.localScale.y)));
    }

    private void TickSummon()
    {
        visual.SetPose(GolemBossPose.Summon, elapsed / Mathf.Max(0.1f, summonWarningTime), facing);
        foreach (var marker in warnings) if (marker != null) marker.SetProgress(elapsed / Mathf.Max(0.1f, summonWarningTime));
        if (elapsed < summonWarningTime) return;
        for (int i = 0; i < warnings.Count && LivingMinionCount < maxLivingMinions; i++)
        {
            if (warnings[i] == null || summonChoices[i] == null) continue;
            GameObject prefab = summonChoices[i];
            Collider2D collider = prefab.GetComponent<Collider2D>();
            float offset = collider != null ? collider.offset.y * prefab.transform.localScale.y : 0f;
            Vector2 ground = warnings[i].Center - Vector2.up * warnings[i].Size.y / 2f;
            Vector2 position = ground + Vector2.up * (MinionSize(prefab).y / 2f - offset + summonLift);
            minions.Add(Instantiate(prefab, position, prefab.transform.rotation));
        }
        CancelWarnings();
        Enter(State.Waiting);
        nextActionTime = Time.time + afterSummonRest;
    }

    private void CancelWarnings()
    {
        foreach (var area in warnings) if (area != null) area.CancelWarning();
        warnings.Clear();
        warningDueTimes.Clear();
        summonChoices.Clear();
    }

    private void Enter(State next) { state = next; elapsed = 0f; }

    private void OnValidate()
    {
        arenaX.y = Mathf.Max(arenaX.x + 2f, arenaX.y);
        if (summonHealthThresholds == null) summonHealthThresholds = Array.Empty<float>();
        for (int i = 0; i < summonHealthThresholds.Length; i++) summonHealthThresholds[i] = Mathf.Clamp01(summonHealthThresholds[i]);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 origin = transform.position;
        Gizmos.DrawLine(origin + Vector3.right * arenaX.x, origin + Vector3.right * arenaX.y);
    }
}
