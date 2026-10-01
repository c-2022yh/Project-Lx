using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

   
    //  패널 참조 (Inspector에서 드래그로 연결)
  
    [Header("Panels")]
    [SerializeField] private HUDPanel hudPanel;
    // [TODO] 아래 패널들은 만들면서 하나씩 활성화
    [SerializeField] private InventoryPanel inventoryPanel;

    // 새 유물 인벤토리(카테고리별 장착칸 + 보관함).
    // 연결되어 있으면 I키가 이쪽을 연다. 비어 있으면 기존 inventoryPanel이 그대로 열린다.
    [SerializeField] private RelicInventoryPanel relicInventoryPanel;
    [SerializeField] private MapPanel mapPanel;
    [SerializeField] private SkillPanel skillPanel;
    [SerializeField] private PausePanel pausePanel;
    [SerializeField] private GameOverPanel gameOverPanel;
    // [SerializeField] private NotificationPanel notificationPanel;
    [SerializeField] private ControlGuidePanel controlGuidePanel;
    [SerializeField] private TutorialTooltip tutorialTooltip;

    // ─────────────────────────────────────────
    //  더미 스탯 (UI 테스트용)
    // ─────────────────────────────────────────
    // [PLAYER_HOOK] 플레이어 붙으면 이 영역 통째로 제거.
    // 대신 PlayerStats 같은 컴포넌트가 동일 시그니처 이벤트 발행.
    [Header("Dummy Stats (UI 테스트용)")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int maxSoul = 5;  // 5칸 게이지
    private int currentHealth;
    private int currentSoul;

    public event Action<int, int> OnHealthChanged;  // (current, max)
    public event Action<int, int> OnSoulChanged;

    
    //  팝업 상태

    private bool isInventoryOpen, isSkillOpen, isMapOpen, isPaused;

    /// <summary>플레이어 입력을 껐다 켜려고 들고 있는다. 씬이 바뀌면 다시 찾는다.</summary>
    private PlayerInput playerInput;
    public bool IsAnyPopupOpen => isInventoryOpen || isSkillOpen || isMapOpen || isPaused;

    /// <summary>
    /// 화면을 점유하는 "창"이 하나라도 떠 있는지. 맵을 막는 기준이다.
    /// IsAnyPopupOpen과 달리 맵은 세지 않는다. 맵은 창이 아니라
    /// 누르고 있는 동안만 겹쳐 보이는 오버레이라서 서로 막을 이유가 없다.
    /// </summary>
    private bool IsAnyWindowOpen =>
        isInventoryOpen || isSkillOpen || isPaused ||
        (controlGuidePanel != null && controlGuidePanel.gameObject.activeSelf) ||
        (gameOverPanel != null && gameOverPanel.gameObject.activeSelf);

   
    //  Input System 연결
  
    //private UIInputActions uiInput;

    void Awake()
    {
        Debug.Log($"[UIManager] Awake 호출됨 - GameObject: {gameObject.name}");

        if (Instance != null && Instance != this)
        {
            Debug.Log($"[UIManager] 중복 발견! {gameObject.name} 파괴함");
            Destroy(gameObject);
            return;
        }
        Instance = this;
       // DontDestroyOnLoad(gameObject);  UI_root로 관리한다고 해서 오류날까봐 주석처리함. 대신 씬마다 새로 넣을 것
       //얘의 원래 기능은 씬 넘어가도 안 죽는거임
        /*
        uiInput = new UIInputActions();
        uiInput.UI.ToggleInventory.performed += ctx => ToggleInventory();
        uiInput.UI.ToggleMap.performed += ctx => ToggleMap();
        uiInput.UI.TogglePause.performed += ctx => TogglePause();
        */

        currentHealth = maxHealth;
        currentSoul = maxSoul;
    }

    void OnEnable()
    {
        //uiInput?.UI.Enable();
    }

    void OnDisable()
    {
        //uiInput?.UI.Disable();
    }

    void Start()
    {
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnSoulChanged?.Invoke(currentSoul, maxSoul);
    }

    void Update()

    {
      

        // [DEBUG_ONLY] 플레이어 붙으면 제거
        if (Keyboard.current.digit1Key.wasPressedThisFrame) ModifyHealth(-15);
        if (Keyboard.current.digit2Key.wasPressedThisFrame) ModifyHealth(+20);
        if (Keyboard.current.digit3Key.wasPressedThisFrame) ModifySoul(-1);
        if (Keyboard.current.digit4Key.wasPressedThisFrame) ModifySoul(+1);
        if (Keyboard.current.digit0Key.wasPressedThisFrame) ShowGameOver();
        if (Keyboard.current.escapeKey.wasPressedThisFrame) HandleCancelKey();
        if (Keyboard.current.iKey.wasPressedThisFrame) ShowWindowTab(UITabBar.RelicTab);
        if (Keyboard.current.kKey.wasPressedThisFrame) ShowWindowTab(UITabBar.SkillTab);

        // Q/E로 유물 ↔ 스킬 탭 이동. 창이 떠 있을 때만 본다.
        // Q는 게임에서 각성 키라, 창이 닫혀 있을 때 가로채면 각성이 안 나간다.
        if (isInventoryOpen || isSkillOpen)
        {
            if (Keyboard.current.qKey.wasPressedThisFrame ||
                Keyboard.current.eKey.wasPressedThisFrame)
            {
                ShowWindowTab(isInventoryOpen ? UITabBar.SkillTab : UITabBar.RelicTab);
            }
        }

        // 맵은 탭을 누르고 있는 동안만 보인다.
        // 손을 떼면 조건 없이 내려가므로 켜진 채로 남는 상태가 생기지 않는다.
        if (Keyboard.current.tabKey.wasPressedThisFrame) SetMapVisible(true);
        if (Keyboard.current.tabKey.wasReleasedThisFrame) SetMapVisible(false);

        //이거는 나중에 처음 스킬 발동하면 나오게 튜토리얼 구현할건데 테스트키
        if (Keyboard.current.tKey.wasPressedThisFrame)
            tutorialTooltip.Show("Shift", "대시로 회피하세요");  
    }

    //  스탯 조작

    public void ModifyHealth(int delta)
    {
        currentHealth = Mathf.Clamp(currentHealth + delta, 0, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        if (currentHealth == 0) ShowGameOver();
    }

    public void ModifySoul(int delta)
    {
        currentSoul = Mathf.Clamp(currentSoul + delta, 0, maxSoul);
        OnSoulChanged?.Invoke(currentSoul, maxSoul);
    }

    public bool TryUseSoul(int amount)
    {
        if (currentSoul < amount) return false;
        ModifySoul(-amount);
        return true;
    }

    //  팝업 토글

    /// <summary>
    /// ESC: 지금 열려 있는 창을 닫는다. 닫을 게 없을 때만 일시정지를 연다.
    ///
    /// 창을 열어둔 채 습관적으로 ESC를 누르기 때문에, 그때 일시정지가 뜨면
    /// 창이 두 겹으로 쌓인다. 위에 떠 있는 것부터 하나씩 닫는다.
    /// </summary>
    private void HandleCancelKey()
    {
        // 조작법은 일시정지 위에 떠 있으므로 가장 먼저 본다.
        if (controlGuidePanel != null && controlGuidePanel.gameObject.activeSelf)
        {
            controlGuidePanel.SetVisible(false);
            ShowPause();
            return;
        }

        if (isInventoryOpen)
        {
            ToggleInventory();
            return;
        }

        if (isSkillOpen)
        {
            ToggleSkill();
            return;
        }

        if (isMapOpen)
        {
            ToggleMap();
            return;
        }

        TogglePause();
    }

    /// <summary>
    /// 유물/스킬 탭을 연다. 같은 탭을 다시 부르면 닫는다.
    /// I·K 키와 탭 버튼, Q/E가 모두 이 함수를 지난다.
    /// </summary>
    public void ShowWindowTab(int tab)
    {
        bool wantRelic = tab == UITabBar.RelicTab;

        // 이미 그 탭이 떠 있으면 토글로 닫는다. I를 두 번 누르면 닫히던 동작 유지.
        if (wantRelic ? isInventoryOpen : isSkillOpen)
        {
            if (wantRelic) ToggleInventory();
            else ToggleSkill();
            return;
        }

        if (wantRelic) ToggleInventory();
        else ToggleSkill();
    }

    /// <summary>
    /// 창이 떠 있는 동안 플레이어 조작을 막는다.
    ///
    /// 막지 않으면 Q로 탭을 넘길 때 각성이 같이 나간다.
    /// 창을 열면 조작이 멈춰야 한다는 건 기획에서도 정해진 사항이다.
    /// PlayerInput의 "Player" 액션맵만 끄므로 UI 입력은 그대로 살아 있다.
    /// </summary>
    private void RefreshPlayerControl()
    {
        if (playerInput == null) playerInput = FindAnyObjectByType<PlayerInput>();
        if (playerInput == null || playerInput.actions == null) return;

        InputActionMap map = playerInput.actions.FindActionMap("Player", false);

        if (map == null) return;

        // 창이 하나도 없을 때만 켠다. 어떤 경로로 닫혔든 여기서 되살아난다.
        if (IsAnyWindowOpen) map.Disable();
        else map.Enable();
    }

    public void ToggleInventory()
    {
        // 여는 것만 막는다. 이미 열려 있으면 언제든 닫을 수 있어야 한다.
        if (!isInventoryOpen && isPaused) return;

        // 창이 뜨면 맵은 내린다. (겹침 규칙 (2))
        SetMapVisible(false);

        // 스킬 창과는 한 번에 하나만 뜬다.
        if (!isInventoryOpen && isSkillOpen) ToggleSkill();

        isInventoryOpen = !isInventoryOpen;
        Debug.Log($"[UI] Inventory: {(isInventoryOpen ? "Open" : "Close")}");

        if (relicInventoryPanel != null)
            relicInventoryPanel.SetVisible(isInventoryOpen);
        else if (inventoryPanel != null)
            inventoryPanel.SetVisible(isInventoryOpen);

        RefreshPlayerControl();
        // [SFX_HOOK] AudioManager.Play(isInventoryOpen ? openSfx : closeSfx);
    }

    /// <summary>
    /// 스킬 창. 유물창과 같은 창의 두 탭이라 한 번에 하나만 뜬다.
    /// 여는 쪽은 ShowWindowTab을 거치고, 여기는 실제로 켜고 끄는 일만 한다.
    /// </summary>
    public void ToggleSkill()
    {
        // 여는 것만 막는다. 이미 열려 있으면 언제든 닫을 수 있어야 한다.
        if (!isSkillOpen && isPaused) return;

        SetMapVisible(false);

        // 인벤토리가 떠 있으면 먼저 닫는다.
        if (!isSkillOpen && isInventoryOpen) ToggleInventory();

        isSkillOpen = !isSkillOpen;
        Debug.Log($"[UI] Skill: {(isSkillOpen ? "Open" : "Close")}");

        if (skillPanel != null) skillPanel.SetVisible(isSkillOpen);

        RefreshPlayerControl();
        // [SFX_HOOK]
    }

    public void ToggleMap()
    {
        SetMapVisible(!isMapOpen);
    }

    /// <summary>
    /// 맵을 켜고 끈다.
    ///
    /// 맵은 탭을 누르고 있는 동안만 뜨는 오버레이라 창과 겹칠 수 있다.
    /// 규칙은 두 개뿐이다.
    ///   (1) 창이 떠 있으면 켜지 않는다  - 여기서
    ///   (2) 창이 열리면 내린다          - ToggleInventory / TogglePause에서
    /// 끄는 쪽은 아무 조건도 보지 않는다. 손을 떼면 무조건 내려가야
    /// 맵이 켜진 채 남는 상태가 생기지 않는다.
    /// </summary>
    public void SetMapVisible(bool visible)
    {
        if (visible && IsAnyWindowOpen) return;
        if (isMapOpen == visible) return;

        isMapOpen = visible;
        Debug.Log($"[UI] Map: {(isMapOpen ? "Open" : "Close")}");

        if (mapPanel != null) mapPanel.SetVisible(isMapOpen);
        // [SFX_HOOK]
    }

    public void TogglePause()
    {
        SetMapVisible(false);

        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;
        Debug.Log($"[UI] Pause: {isPaused}");
        pausePanel.SetVisible(isPaused);

        RefreshPlayerControl();
        // [SFX_HOOK]
    }


    public void ShowGameOver()
    {
        Time.timeScale = 0f;
        Debug.Log("[UI] Game Over");
        gameOverPanel.SetVisible(true);
        // [PLAYER_HOOK] 플레이어 사망 처리
    }

    //가이드
    
    public void ShowControlGuide()
    {
        pausePanel.SetVisible(false);      // 일시정지 끄기
        controlGuidePanel.SetVisible(true); // 조작법 켜기
    }
    //되돌아가기
    public void ShowPause()
    {
        pausePanel.SetVisible(true);
    }
}