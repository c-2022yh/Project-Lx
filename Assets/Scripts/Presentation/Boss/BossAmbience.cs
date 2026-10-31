using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 보스방 분위기: 하늘 위에서 비스듬히 내려오는 옅은 빛줄기 + 그 안을 천천히 떠내려오는 먼지.
/// 보스방(BossRoom)이 시작할 때 만든다. 캐릭터 뒤·배경 앞에 그린다. 실제 시간으로 움직인다.
/// </summary>
public class BossAmbience : MonoBehaviour
{
    private static readonly Color ShaftColor = new Color(0.82f, 0.9f, 1f, 1f);
    private static readonly Color DustColor = new Color(0.9f, 0.94f, 1f, 1f);

    private class Shaft { public SpriteRenderer sr; public float baseAlpha, phase; }
    private class Mote { public SpriteRenderer sr; public Vector2 vel; public float phase, size; }

    private readonly List<Shaft> shafts = new List<Shaft>();
    private readonly List<Mote> motes = new List<Mote>();
    private Bounds area;
    private float strength = 1f, target = 1f;

    public static BossAmbience Create(Bounds room)
    {
        GameObject go = new GameObject("BossAmbience");
        BossAmbience a = go.AddComponent<BossAmbience>();
        a.Build(room);
        return a;
    }

    /// <summary>분위기 세기 (0 = 꺼짐, 1 = 기본). 천천히 바뀐다.</summary>
    public void SetStrength(float value) => target = Mathf.Max(0f, value);

    private void Build(Bounds room)
    {
        area = room;
        area.Expand(new Vector3(8f, 0f, 0f));
        float top = room.max.y + 9f;               // 화면 위 바깥에서 시작
        float height = top - room.min.y + 1f;

        // 빛줄기: 폭·기울기·밝기가 제각각
        // 빛줄기는 보스방 위(천장이 뚫린 하늘 쪽)에만
        int count = Mathf.Clamp(Mathf.RoundToInt(room.size.x / 6f), 3, 6);
        for (int i = 0; i < count; i++)
        {
            float x = Mathf.Lerp(room.min.x + 4.5f, room.max.x - 4.5f, (i + Random.Range(0.2f, 0.8f)) / count);
            GameObject s = new GameObject("LightShaft");
            s.transform.SetParent(transform, false);
            s.transform.position = new Vector3(x, top, 0f);
            s.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(-10f, -4f));   // 하늘 위에서 살짝 비스듬히
            SpriteRenderer sr = s.AddComponent<SpriteRenderer>();
            sr.sprite = BossFxArt.Beam;   // 피벗이 위쪽
            sr.sharedMaterial = BossFxArt.Unlit;
            sr.sortingLayerName = "Default";
            sr.sortingOrder = 42;          // 배경 앞, 땅·캐릭터 뒤 (바닥 안개 뒤 안개(40)보다 앞)
            Vector2 size = sr.sprite.bounds.size;
            float w = Random.Range(1.8f, 4f);
            s.transform.localScale = new Vector3(w / size.x, height / size.y, 1f);
            shafts.Add(new Shaft { sr = sr, baseAlpha = Random.Range(0.07f, 0.14f), phase = Random.value * 10f });
        }

        // 떠내려오는 먼지
        int dust = Mathf.Clamp(Mathf.RoundToInt(area.size.x * 1.2f), 20, 60);
        for (int i = 0; i < dust; i++)
        {
            GameObject d = new GameObject("Dust");
            d.transform.SetParent(transform, false);
            SpriteRenderer sr = d.AddComponent<SpriteRenderer>();
            sr.sprite = BossFxArt.SoftDot;
            sr.sharedMaterial = BossFxArt.Unlit;
            sr.sortingLayerName = i % 4 == 0 ? "GroundCover" : "Default";   // 몇 개는 캐릭터 앞
            sr.sortingOrder = i % 4 == 0 ? 4 : 43;
            Mote m = new Mote { sr = sr, phase = Random.value * 10f, size = Random.Range(0.05f, 0.14f) };
            Respawn(m, true);
            motes.Add(m);
        }
        Update();
    }

    private void Respawn(Mote m, bool anywhere)
    {
        float y = anywhere ? Random.Range(area.min.y, area.max.y + 6f) : area.max.y + 6f;
        m.sr.transform.position = new Vector3(Random.Range(area.min.x, area.max.x), y, 0f);
        m.vel = new Vector2(Random.Range(-0.15f, 0.1f), -Random.Range(0.15f, 0.45f));
        float k = m.size / m.sr.sprite.bounds.size.x;
        m.sr.transform.localScale = new Vector3(k, k, 1f);
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        float time = Time.unscaledTime;
        strength = Mathf.MoveTowards(strength, target, dt * 0.5f);

        foreach (Shaft s in shafts)
        {
            float breathe = 0.75f + 0.25f * Mathf.Sin(time * 0.35f + s.phase);   // 구름이 지나가듯 천천히 밝아졌다 어두워짐
            s.sr.color = new Color(ShaftColor.r, ShaftColor.g, ShaftColor.b, s.baseAlpha * breathe * strength);
        }

        foreach (Mote m in motes)
        {
            Transform t = m.sr.transform;
            Vector3 p = t.position;
            p.x += (m.vel.x + 0.12f * Mathf.Sin(time * 0.8f + m.phase)) * dt;   // 살랑살랑
            p.y += m.vel.y * dt;
            t.position = p;
            if (p.y < area.min.y - 0.5f) { Respawn(m, false); continue; }
            float twinkle = 0.5f + 0.5f * Mathf.Sin(time * 2f + m.phase * 3f);
            m.sr.color = new Color(DustColor.r, DustColor.g, DustColor.b, (0.25f + 0.35f * twinkle) * strength);
        }
    }
}
