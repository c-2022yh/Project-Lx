using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

//플레이어의 체력과 각성 상태에 맞춰 화면 비네팅과 가장자리 왜곡을 관리하는 스크립트
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerHealth), typeof(PlayerAwakening))]
public sealed class PlayerScreenEffects : MonoBehaviour
{
    //비네팅 초기 설정을 가져올 프로필
    [SerializeField] private VolumeProfile volumeTemplate;
    //상태별 비네팅 강도와 저체력 경고 설정
    [Header("Vignette")]
    [Range(0f, 1f)] public float normalVignette = 0.12f;
    [Range(0f, 1f)] public float bossVignette = 0.23f;
    [Range(0f, 1f)] public float awakenedVignette = 0.25f;
    [Range(0.01f, 1f)] public float lowHealthThreshold = 0.3f;
    [Range(0f, 1f)] public float lowHealthVignette = 0.36f;
    public Color lowHealthColor = new Color(0.55f, 0.015f, 0.02f);
    //저체력일 때 반복되는 맥박의 진폭과 초당 반복 횟수
    [Range(0f, 0.1f)] public float pulseAmplitude = 0.015f;
    [Min(0f)] public float pulseFrequency = 0.8f;
    //피격 시 잠깐 추가할 강도와 지속 시간
    [Range(0f, 1f)] public float hitBoost = 0.14f;
    [Min(0.01f)] public float hitDuration = 0.18f;
    [Min(0.01f)] public float transitionSpeed = 8f;
    //각성 진입, 유지, 종료 시 화면 가장자리 왜곡 설정
    [Header("Edge distortion (UV displacement)")]
    [Range(0f, 0.02f)] public float entryDistortion = 0.008f;
    [Range(0f, 0.02f)] public float sustainedDistortion = 0.0015f;
    [Min(0.01f)] public float entryRise = 0.1f;
    [Min(0.01f)] public float entrySettle = 0.45f;
    [Min(0.01f)] public float exitDuration = 0.25f;
    //왜곡 전체 배율이며 0으로 설정하면 왜곡만 해제
    [Range(0f, 1f)] public float distortionScale = 1f;

    //화면 효과를 제어하는 단일 플레이어와 참조 컴포넌트
    private static PlayerScreenEffects owner;
    private PlayerHealth health;
    private PlayerAwakening awakening;
    private Volume volume;
    private VolumeProfile runtimeProfile;
    private Vignette vignette;
    //피격 잔여 시간, 각성 진행 시간, 왜곡과 종료 보간에 필요한 값
    private float hitRemaining, awakeningTime, distortion, exitStart, exitTime, baseIntensity;
    //이전 프레임의 각성 여부와 외부에서 전달받은 보스전 여부
    private bool wasAwakened, bossBattle;
    //반복해서 사용하는 셰이더 속성 이름을 ID로 보관
    private static readonly int StrengthId = Shader.PropertyToID("_LxScreenEdgeStrength");
    private static readonly int ClockId = Shader.PropertyToID("_LxScreenEffectTime");

    //플레이 시작 시 이전 실행의 정적 참조와 셰이더 값을 초기화
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        owner = null;
        Shader.SetGlobalFloat(StrengthId, 0f);
    }

    //플레이어 참조와 실행 중 사용할 비네팅 Volume 생성
    private void Awake()
    {
        health = GetComponent<PlayerHealth>();
        awakening = GetComponent<PlayerAwakening>();
        //플레이어와 함께 유지되고 제거되는 자식 오브젝트 생성
        var holder = new GameObject("Player Screen Vignette (Runtime)");
        holder.transform.SetParent(transform, false);
        holder.layer = 0; //카메라의 Volume Mask에 포함되는 Default 레이어 사용
        volume = holder.AddComponent<Volume>();
        //플레이어 위치와 관계없이 적용하며 우선순위는 100으로 설정
        volume.isGlobal = true;
        volume.priority = 100f;
        volume.sharedProfile = volumeTemplate;
        runtimeProfile = volume.profile; //원본 에셋 대신 실행 중에 사용할 프로필 복사본 사용
        //프로필에 비네팅이 없으면 추가하고 제어할 항목의 Override 활성화
        if (!runtimeProfile.TryGet(out vignette)) vignette = runtimeProfile.Add<Vignette>(true);
        vignette.active = true;
        vignette.intensity.Override(normalVignette);
        vignette.color.Override(Color.black);
        vignette.center.Override(new Vector2(0.5f, 0.5f));
        vignette.smoothness.Override(0.45f);
        vignette.rounded.Override(false);
        volume.enabled = false;
        baseIntensity = normalVignette;
    }

    //피격, 사망, 카메라 렌더링 이벤트 구독
    private void OnEnable()
    {
        //씬 전환 중 중복 생성된 플레이어가 같은 화면 효과를 제어하지 않도록 방지
        if (owner != null && owner != this) { enabled = false; return; }
        owner = this;
        health.OnDamaged += OnDamaged;
        health.OnDied += ResetEffects;
        RenderPipelineManager.beginCameraRendering += BeginCamera;
        RenderPipelineManager.endCameraRendering += EndCamera;
    }

    //이벤트 구독 해제와 남아 있는 화면 효과 정리
    private void OnDisable()
    {
        if (owner != this) return;
        health.OnDamaged -= OnDamaged;
        health.OnDied -= ResetEffects;
        RenderPipelineManager.beginCameraRendering -= BeginCamera;
        RenderPipelineManager.endCameraRendering -= EndCamera;
        ResetEffects();
        volume.enabled = false;
        Shader.SetGlobalFloat(StrengthId, 0f);
        owner = null;
    }

    //직접 생성한 런타임 프로필과 Volume 오브젝트 제거
    private void OnDestroy()
    {
        if (runtimeProfile != null)
        {
            foreach (var component in runtimeProfile.components)
                if (component != null) Destroy(component);
            Destroy(runtimeProfile);
        }
        if (volume != null) Destroy(volume.gameObject);
    }

    //실제 피해가 발생했을 때 피격 연출 시간 갱신
    private void OnDamaged(float amount) => hitRemaining = hitDuration; //연속 피격 시 강도를 누적하지 않고 남은 시간만 갱신
    //보스전 관리 코드에서 전투 시작과 종료 상태 전달
    public void SetBossBattle(bool active) => bossBattle = active;

    //사망 또는 컴포넌트 비활성화 시 평상시 화면으로 초기화
    private void ResetEffects()
    {
        hitRemaining = awakeningTime = distortion = exitStart = exitTime = 0f;
        wasAwakened = bossBattle = false;
        baseIntensity = normalVignette;
        vignette.intensity.value = normalVignette;
        vignette.color.value = Color.black;
        Shader.SetGlobalFloat(StrengthId, 0f);
    }

    //플레이어 상태 처리가 끝난 뒤 이번 프레임의 화면 효과 계산
    private void LateUpdate()
    {
        float dt = Time.deltaTime; //일시정지 중에는 효과의 시간 진행도 정지
        //프레임 시간에 맞춰 부드러운 전환 비율 계산
        float blend = 1f - Mathf.Exp(-Mathf.Max(0.01f, transitionSpeed) * dt);
        //살아 있고 각성 준비 중이거나 각성 유지 중이면 각성 효과 적용
        bool empowered = !health.IsDead && (awakening.IsAwakening || awakening.IsAwakened);
        //각성 진입 시 타이머 초기화, 종료 시 현재 왜곡 강도 저장
        if (empowered && !wasAwakened) awakeningTime = 0f;
        if (!empowered && wasAwakened) { exitStart = distortion; exitTime = 0f; }
        if (empowered)
        {
            //시간을 더하기 전에 계산하여 각성 진입 첫 프레임의 왜곡을 0에서 시작
            float rise = Mathf.Max(0.01f, entryRise);
            //진입 구간에서는 0에서 최대로 증가하고 이후 유지 강도로 감소
            distortion = awakeningTime < rise
                ? Mathf.Lerp(0f, entryDistortion, awakeningTime / rise)
                : Mathf.Lerp(entryDistortion, sustainedDistortion,
                    Mathf.Clamp01((awakeningTime - rise) / Mathf.Max(0.01f, entrySettle)));
            awakeningTime += dt;
        }
        else
        {
            //정상 종료 시 저장한 강도에서 0까지 부드럽게 감소
            exitTime += dt;
            distortion = Mathf.Lerp(exitStart, 0f, Mathf.Clamp01(exitTime / Mathf.Max(0.01f, exitDuration)));
        }
        //사망 시 종료 보간을 기다리지 않고 왜곡 제거
        if (health.IsDead) distortion = 0f;
        wasAwakened = empowered;

        //살아 있는 플레이어의 현재 체력 비율로 저체력 판정
        bool low = !health.IsDead && health.CurrentHealth / Mathf.Max(1f, health.MaxHealth) <= lowHealthThreshold;
        //겹치는 지속 상태는 강도를 합산하지 않고 가장 강한 값 선택
        float target = normalVignette;
        if (bossBattle) target = Mathf.Max(target, bossVignette);
        if (empowered) target = Mathf.Max(target, awakenedVignette);
        //저체력 강도에 작은 사인파를 더하여 맥박 표현
        if (low) target = Mathf.Max(target, lowHealthVignette
            + Mathf.Sin(Time.time * Mathf.PI * 2f * pulseFrequency) * pulseAmplitude);
        baseIntensity = Mathf.Lerp(baseIntensity, target, blend);
        //지속 상태는 부드럽게 바꾸고, 짧은 피격 반응은 즉시 더해서 표시
        float hit = hitBoost * Mathf.Clamp01(hitRemaining / Mathf.Max(0.01f, hitDuration));
        //지속 강도와 피격 강도를 합친 뒤 유효 범위로 제한
        vignette.intensity.value = Mathf.Clamp01(baseIntensity + hit);
        //저체력일 때 붉은색으로, 그 외에는 검정색으로 전환
        vignette.color.value = Color.Lerp(vignette.color.value, low ? lowHealthColor : Color.black, blend);
        hitRemaining = Mathf.Max(0f, hitRemaining - dt);
    }

    //카메라마다 화면 효과 적용 여부와 셰이더 값을 전달
    private void BeginCamera(ScriptableRenderContext context, Camera camera)
    {
        var marker = camera.GetComponent<ScreenEffectsCamera>();
        //화면 효과 마커가 활성화된 게임 카메라만 적용
        bool applies = camera.cameraType == CameraType.Game && marker != null && marker.isActiveAndEnabled;
        //URP가 현재 카메라의 Volume을 계산하기 전에 적용 여부 설정
        volume.enabled = applies;
        Shader.SetGlobalFloat(StrengthId, applies ? distortion * Mathf.Clamp01(distortionScale) : 0f);
        Shader.SetGlobalFloat(ClockId, Time.time);
    }

    //렌더링 종료 후 다른 카메라에 효과가 남지 않도록 정리
    private void EndCamera(ScriptableRenderContext context, Camera camera)
    {
        volume.enabled = false;
        Shader.SetGlobalFloat(StrengthId, 0f);
    }
}
