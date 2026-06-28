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
    // [SerializeField] private MapPanel mapPanel;
    [SerializeField] private PausePanel pausePanel;
    [SerializeField] private GameOverPanel gameOverPanel;
    // [SerializeField] private NotificationPanel notificationPanel;

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

    private bool isInventoryOpen, isMapOpen, isPaused;
    public bool IsAnyPopupOpen => isInventoryOpen || isMapOpen || isPaused;

   
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
        DontDestroyOnLoad(gameObject);
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
        if (Keyboard.current.escapeKey.wasPressedThisFrame) TogglePause();
        if (Keyboard.current.iKey.wasPressedThisFrame) ToggleInventory();
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

    public void ToggleInventory()
    {
        if (isPaused) return;
        isInventoryOpen = !isInventoryOpen;
        Debug.Log($"[UI] Inventory: {(isInventoryOpen ? "Open" : "Close")}");
        inventoryPanel.SetVisible(isInventoryOpen);
        // [SFX_HOOK] AudioManager.Play(isInventoryOpen ? openSfx : closeSfx);
    }

    public void ToggleMap()
    {
        if (isPaused || isInventoryOpen) return;
        isMapOpen = !isMapOpen;
        Debug.Log($"[UI] Map: {(isMapOpen ? "Open" : "Close")}");
        // [TODO] mapPanel.SetVisible(isMapOpen);
        // [SFX_HOOK]
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;
        Debug.Log($"[UI] Pause: {isPaused}");
        pausePanel.SetVisible(isPaused);
        // [PLAYER_HOOK] 플레이어 입력 막기: PlayerInput.actions.FindActionMap("Player").Disable();
        // [SFX_HOOK]
    }

    public void ShowGameOver()
    {
        Time.timeScale = 0f;
        Debug.Log("[UI] Game Over");
        gameOverPanel.SetVisible(true);
        // [PLAYER_HOOK] 플레이어 사망 처리
    }

    
}