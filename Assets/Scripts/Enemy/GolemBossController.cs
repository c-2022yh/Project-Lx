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

// Enemy/EnemyHealth remain the sole owners of incoming damage,
// rewards and death. This component owns only encounter and pattern scheduling.
[RequireComponent(typeof(Enemy), typeof(EnemyHealth), typeof(EnemyStats))]
[RequireComponent(typeof(Rigidbody2D), typeof(GolemBossVisual))]
[RequireComponent(typeof(CapsuleCollider2D))]
public class GolemBossController : MonoBehaviour
{
    private enum State { Waiting, Prepare, Active, Recovery, Summon, PhaseChange, Split }
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

    [Header("Phase 2: split into two independent golems")]
    [SerializeField] private bool splitEnabled = true;
    [SerializeField, Range(0.05f, 0.95f)] private float phaseTwoHealth = 0.5f;
    [SerializeField, Min(0.1f)] private float phaseChangeTime = 1.6f;
    [SerializeField, Range(0.2f, 0.95f)] private float fragmentScale = 0.65f;
    [SerializeField, Min(0.5f)] private float splitDistance = 2.5f;
    [SerializeField, Min(0f)] private float fragmentMoveSpeed = 1f;
    [SerializeField, Min(0.1f)] private float fragmentStopDistance = 2.2f;
    [SerializeField, HideInInspector] private bool isFragment;
    [Tooltip("Used only when Split Enabled is off (legacy phase).")]
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
    private readonly List<GolemBossController> fragments = new List<GolemBossController>();
    private GolemBossController fragmentOwner, attackingFragment;
    private int nextFragment;
    private Rigidbody2D body;
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
    public bool IsFragment => isFragment;
    public bool HasSplit => state == State.Split;
    public bool IsDefeated { get; private set; }
    public IReadOnlyList<GolemBossController> Fragments => fragments;
    public int LivingFragmentCount => fragments.FindAll(item => item != null && item.gameObject.activeInHierarchy && !item.health.IsDead).Count;
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
        body = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
        stats = GetComponent<EnemyStats>();
        visual = GetComponent<GolemBossVisual>();
    }

    private void OnEnable()
    {
        state = State.Waiting;
        IsDefeated = false;
        engaged = phaseTwo = hasLastPattern = false;
        armed = autoBegin;
        chainRemaining = 0;
        spawnX = fragmentOwner != null ? fragmentOwner.spawnX : transform.position.x;
        nextSearchTime = 0f;
        usedSummons = new bool[summonHealthThresholds.Length];
        Array.Clear(readyTimes, 0, readyTimes.Length);
        visual.SetPose(GolemBossPose.Idle);
        visual.SetPresentationVisible(true);
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
        ReleaseFragmentTurn();
        bool wasSplit = state == State.Split;
        bool wasEngaged = engaged;
        engaged = armed = false;
        CancelWarnings();
        chainRemaining = 0;
        state = State.Waiting;
        visual.SetPose(GolemBossPose.Idle);
        StopHorizontalMovement();
        foreach (var fragment in fragments)
            if (fragment != null) { fragment.gameObject.SetActive(false); Destroy(fragment.gameObject); }
        fragments.Clear();
        attackingFragment = null;
        if (wasSplit && !health.IsDead && gameObject.activeInHierarchy && !IsDefeated)
        {
            phaseTwo = false;
            health.IsDamageSuppressed = false;
            visual.SetPresentationVisible(true);
            bodyCollider.enabled = true;
            body.simulated = true;
        }
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
        if (state == State.Split)
        {
            // Wait for each EnemyHealth death animation to finish before ending the encounter.
            if (fragments.TrueForAll(item => item == null || !item.gameObject.activeInHierarchy))
            {
                IsDefeated = true;
                gameObject.SetActive(false);
            }
            return;
        }
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
                StopHorizontalMovement();
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
                MoveFragment();
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
                    ReleaseFragmentTurn();
                    Enter(State.Waiting);
                    nextActionTime = Time.time + (chainRemaining > 0 ? 0f : patternInterval);
                }
                break;
            case State.Summon:
                TickSummon();
                break;
            case State.PhaseChange:
                visual.SetPose(GolemBossPose.PhaseChange, elapsed / Mathf.Max(0.1f, phaseChangeTime), facing);
                if (elapsed >= phaseChangeTime)
                {
                    if (splitEnabled && !isFragment)
                    {
                        if (!CreateFragments()) elapsed = 0f; // No invisible spawns in walls or over gaps.
                    }
                    else { Enter(State.Waiting); nextActionTime = Time.time + patternInterval; }
                }
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

    private bool CreateFragments()
    {
        var capsule = (CapsuleCollider2D)bodyCollider;
        Vector3 scale = transform.lossyScale * fragmentScale;
        Vector2 size = Vector2.Scale(capsule.size, new Vector2(Mathf.Abs(scale.x), Mathf.Abs(scale.y)));
        var positions = new Vector2[2];
        for (int i = 0; i < 2; i++)
        {
            float x = Mathf.Clamp(transform.position.x + (i == 0 ? -splitDistance : splitDistance),
                spawnX + arenaX.x + size.x / 2f, spawnX + arenaX.y - size.x / 2f);
            if (!TryGround(x, FeetY + 3f, out Vector2 ground) ||
                Physics2D.OverlapBox(ground + Vector2.up * (size.y / 2f + 0.06f), size * 0.9f, 0f, groundLayers)) return false;
            positions[i] = ground + Vector2.up * (size.y / 2f - capsule.offset.y * scale.y + 0.06f);
        }
        if (Mathf.Abs(positions[0].x - positions[1].x) < size.x + 0.1f) return false;

        // Configure clones while inactive: no full-HP frame, recursive split or copied encounter callbacks.
        var staging = new GameObject("Golem Split Staging");
        staging.SetActive(false);
        try
        {
            for (int i = 0; i < 2; i++)
            {
                GameObject clone = Instantiate(gameObject, positions[i], transform.rotation, staging.transform);
                clone.SetActive(false);
                clone.name = name + (i == 0 ? " Left Fragment" : " Right Fragment");
                clone.transform.localScale = scale;
                var fragment = clone.GetComponent<GolemBossController>();
                fragment.isFragment = true;
                fragment.autoBegin = false;
                fragment.fragmentOwner = this;
                fragment.onEncounterStarted = new UnityEvent();
                fragment.onEncounterEnded = new UnityEvent();
                fragment.onPhaseTwo = new UnityEvent();
                clone.GetComponent<EnemyHealth>().ConfigureSpawnHealth(Mathf.Max(1f, health.CurrentHp * 0.5f), health.EnergyReward * 0.5f);
                var fragmentBody = clone.GetComponent<Rigidbody2D>();
                fragmentBody.constraints &= ~RigidbodyConstraints2D.FreezePositionX;
                fragmentBody.linearVelocity = Vector2.zero;
                clone.transform.SetParent(null, true);
                fragments.Add(fragment);
            }
            nextFragment = 0;
            foreach (var fragment in fragments)
            {
                fragment.gameObject.SetActive(true);
                fragment.BeginEncounter();
            }
        }
        finally { Destroy(staging); }

        CancelWarnings();
        // Phase-one summons end here; fragments retain the same three attack patterns only.
        foreach (GameObject minion in minions) if (minion != null) Destroy(minion);
        minions.Clear();
        StopHorizontalMovement();
        bodyCollider.enabled = false;
        body.simulated = false;
        health.IsDamageSuppressed = true;
        visual.SetPresentationVisible(false);
        Enter(State.Split);
        return true;
    }

    private bool TryAcquireFragmentTurn(GolemBossController candidate)
    {
        if (attackingFragment != null && attackingFragment.gameObject.activeInHierarchy && !attackingFragment.health.IsDead)
            return attackingFragment == candidate;
        attackingFragment = null;
        for (int i = 0; i < fragments.Count; i++)
        {
            int index = (nextFragment + i) % fragments.Count;
            var next = fragments[index];
            if (next == null || !next.gameObject.activeInHierarchy || next.health.IsDead) continue;
            if (next != candidate) return false;
            attackingFragment = candidate;
            return true;
        }
        return false;
    }

    private void ReleaseFragmentTurn()
    {
        if (fragmentOwner == null || fragmentOwner.attackingFragment != this) return;
        int index = fragmentOwner.fragments.IndexOf(this);
        fragmentOwner.nextFragment = fragmentOwner.fragments.Count > 0 ? (index + 1) % fragmentOwner.fragments.Count : 0;
        fragmentOwner.attackingFragment = null;
    }

    private void StopHorizontalMovement()
    {
        if (body != null && body.simulated) body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
    }

    private void MoveFragment()
    {
        if (!isFragment || player == null || fragmentMoveSpeed <= 0f) { StopHorizontalMovement(); return; }
        float dx = player.transform.position.x - transform.position.x;
        int direction = dx >= 0f ? 1 : -1;
        facing = direction;
        Bounds bounds = bodyCollider.bounds;
        float step = fragmentMoveSpeed * Time.fixedDeltaTime;
        float edge = bounds.center.x + direction * (bounds.extents.x + step + 0.05f);
        if (Mathf.Abs(dx) <= fragmentStopDistance || edge < spawnX + arenaX.x || edge > spawnX + arenaX.y ||
            !TryGround(edge, bounds.min.y + 0.15f, out Vector2 ground) || bounds.min.y - ground.y > 0.4f)
        { StopHorizontalMovement(); return; }
        if (fragmentOwner != null)
            foreach (var other in fragmentOwner.fragments)
                if (other != null && other != this && other.gameObject.activeInHierarchy && !other.health.IsDead &&
                    (other.transform.position.x - transform.position.x) * direction > 0f &&
                    Mathf.Abs(other.transform.position.x - transform.position.x) < bounds.extents.x + other.bodyCollider.bounds.extents.x + 0.15f)
                { StopHorizontalMovement(); return; }
        var filter = new ContactFilter2D();
        filter.SetLayerMask(groundLayers);
        filter.useTriggers = false;
        int count = body.Cast(Vector2.right * direction, filter, groundHits, step + 0.05f);
        for (int i = 0; i < count; i++)
            if (groundHits[i].normal.x * direction < -0.1f) { StopHorizontalMovement(); return; }
        body.linearVelocity = new Vector2(direction * fragmentMoveSpeed, body.linearVelocity.y);
    }

    private void ChooseNextAction()
    {
        float hp = health.MaxHp > 0f ? health.CurrentHp / health.MaxHp : 0f;
        if (!isFragment && !phaseTwo && hp <= phaseTwoHealth)
        {
            phaseTwo = true;
            chainRemaining = 0;
            Enter(State.PhaseChange);
            onPhaseTwo.Invoke();
            return;
        }
        for (int i = 0; !isFragment && i < usedSummons.Length; i++)
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
        if (isFragment && fragmentOwner != null && !fragmentOwner.TryAcquireFragmentTurn(this))
        { nextActionTime = Time.time + 0.1f; return; }
        if (hasLastPattern && choices.Count > 1) choices.Remove(lastPattern);
        Pattern next = choices[UnityEngine.Random.Range(0, choices.Count)];
        if (chainRemaining > 0) chainRemaining--;
        else if (phaseTwo && !isFragment && !splitEnabled) chainRemaining = 1;
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

    private void Enter(State next)
    {
        state = next;
        elapsed = 0f;
        if (next != State.Waiting) StopHorizontalMovement();
    }

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
