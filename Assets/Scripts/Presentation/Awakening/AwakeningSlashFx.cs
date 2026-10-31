using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 각성 중 공격하면 베기 궤적 뒤에 푸른 잔상이 함께 나온다.
///
///   잔상 1  원래 베기와 같은 순간, 조금 더 크게 뒤쪽에서 푸르게 빛남
///   잔상 2  조금 늦게(0.06초) 더 크게 퍼지며 사라짐 (칼바람이 한 번 더 지나가는 느낌)
///
/// 원래 베기 프리팹을 "그림만" 복사해서 쓴다 (판정·스크립트·충돌은 빼므로 데미지는 그대로).
/// PlayerAttack.AttackEffectCreated만 듣는다. 플레이어 프리팹·PlayerAttack은 고치지 않고, 씬이 열릴 때 스스로 붙는다.
/// </summary>
[DisallowMultipleComponent]
public class AwakeningSlashFx : MonoBehaviour
{
    [SerializeField] private Color echoColor = new Color(0.35f, 0.65f, 1f, 0.8f);
    [Tooltip("잔상 1 크기 (원래 베기 = 1)")]
    [SerializeField] private float echoScale = 1.15f;
    [Tooltip("잔상 2 크기")]
    [SerializeField] private float trailScale = 1.35f;
    [SerializeField] private float trailDelay = 0.06f;
    [Tooltip("잔상이 사라지는 시간 (초). 원래 베기 시간보다 짧으면 원래 시간을 따른다")]
    [SerializeField] private float minDuration = 0.25f;

    private Player player;
    private PlayerAwakening awakening;
    private PlayerAttack attack;
    private GameObject inactiveHolder;

    // ── 붙이기 ─────────────────────────────

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        TryAttach();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => TryAttach();

    private static void TryAttach()
    {
        Player p = FindAnyObjectByType<Player>();
        if (p != null && p.GetComponent<AwakeningSlashFx>() == null) p.gameObject.AddComponent<AwakeningSlashFx>();
    }

    private void Start()
    {
        player = GetComponent<Player>();
        awakening = GetComponent<PlayerAwakening>();
        attack = GetComponent<PlayerAttack>();
        if (awakening == null || attack == null) { enabled = false; return; }
        attack.AttackEffectCreated += OnAttackEffect;
    }

    private void OnDestroy()
    {
        if (attack != null) attack.AttackEffectCreated -= OnAttackEffect;
        if (inactiveHolder != null) Destroy(inactiveHolder);
    }

    private void OnAttackEffect(AttackPattern pattern, Vector3 pos, Quaternion rot, float dir)
    {
        if (!awakening.IsAwakened || pattern == null || pattern.attackEffectPrefab == null) return;
        float life = Mathf.Max(minDuration, pattern.effectDuration);
        Spawn(pattern, pos, rot, dir, echoScale, echoColor, life, pattern.followPlayer);
        StartCoroutine(SpawnLater(pattern, pos, rot, dir, life));
    }

    private IEnumerator SpawnLater(AttackPattern pattern, Vector3 pos, Quaternion rot, float dir, float life)
    {
        yield return new WaitForSeconds(trailDelay);
        Color c = echoColor;
        c.a *= 0.6f;
        Spawn(pattern, pos, rot, dir, trailScale, c, life * 1.2f, false);
    }

    // 베기 프리팹을 그림만 남겨 복사한다 (꺼진 상태에서 복사해서 스크립트가 한 번도 돌지 않게)
    private void Spawn(AttackPattern pattern, Vector3 pos, Quaternion rot, float dir, float scale, Color color, float life, bool follow)
    {
        if (inactiveHolder == null)
        {
            inactiveHolder = new GameObject("AwakeningSlashFx_Holder");
            inactiveHolder.SetActive(false);
            DontDestroyOnLoad(inactiveHolder);
        }

        GameObject copy = Instantiate(pattern.attackEffectPrefab, pos, rot, inactiveHolder.transform);
        copy.name = "AwakeningSlashEcho";
        StripToVisuals(copy);
        if (copy.GetComponentsInChildren<MonoBehaviour>(true).Length > 0) { Destroy(copy); return; }   // 못 지운 스크립트가 있으면 안전하게 포기

        List<SpriteRenderer> renderers = new List<SpriteRenderer>(copy.GetComponentsInChildren<SpriteRenderer>(true));
        if (renderers.Count == 0) { Destroy(copy); return; }
        renderers[0].flipX = dir < 0f;   // 원래 베기와 같게
        foreach (SpriteRenderer r in renderers) r.sortingOrder -= 1;   // 원래 베기 뒤
        foreach (Animator a in copy.GetComponentsInChildren<Animator>(true)) a.fireEvents = false;

        copy.transform.SetParent(follow && player != null ? player.transform : null, true);
        copy.transform.position = pos;
        copy.transform.rotation = rot;
        Vector3 baseScale = pattern.effectScale * scale;
        copy.transform.localScale = baseScale;
        if (follow && player != null)
        {
            Vector3 ps = player.transform.lossyScale;
            copy.transform.localScale = new Vector3(baseScale.x / Mathf.Max(0.0001f, Mathf.Abs(ps.x)),
                                                    baseScale.y / Mathf.Max(0.0001f, Mathf.Abs(ps.y)), 1f);
        }

        AwakeningSlashEcho echo = copy.AddComponent<AwakeningSlashEcho>();
        echo.Init(renderers, color, life);
    }

    private static void StripToVisuals(GameObject root)
    {
        // 스크립트 먼저 (서로 필요로 하는 경우가 있어서 두 번), 그다음 물리
        for (int pass = 0; pass < 3; pass++)
            foreach (MonoBehaviour m in root.GetComponentsInChildren<MonoBehaviour>(true))
                if (m != null) DestroyImmediate(m);
        foreach (Collider2D c in root.GetComponentsInChildren<Collider2D>(true)) DestroyImmediate(c);
        foreach (Rigidbody2D rb in root.GetComponentsInChildren<Rigidbody2D>(true)) DestroyImmediate(rb);
        foreach (AudioSource s in root.GetComponentsInChildren<AudioSource>(true)) DestroyImmediate(s);
    }
}

/// <summary>각성 베기 잔상: 푸르게 물들고, 조금 커지며 사라진다.</summary>
public class AwakeningSlashEcho : MonoBehaviour
{
    private List<SpriteRenderer> renderers;
    private Color color;
    private float life, t;
    private Vector3 startScale;

    public void Init(List<SpriteRenderer> rs, Color c, float duration)
    {
        renderers = rs;
        color = c;
        life = Mathf.Max(0.05f, duration);
        startScale = transform.localScale;
        Apply(0f);
    }

    private void Update()
    {
        t += Time.deltaTime;
        if (t >= life) { Destroy(gameObject); return; }
        Apply(t / life);
    }

    private void Apply(float k)
    {
        float a = color.a * (1f - k) * (1f - k);
        foreach (SpriteRenderer r in renderers)
        {
            if (r == null) continue;
            r.color = new Color(color.r, color.g, color.b, a);
        }
        transform.localScale = startScale * (1f + 0.12f * k);
    }
}
