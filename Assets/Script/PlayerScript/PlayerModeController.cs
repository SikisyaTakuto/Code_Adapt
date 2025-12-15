using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;

/// <summary>
/// プレイヤーのアーマーモード、武器モード、エネルギー管理、およびUIの制御を行います。
/// Input Systemからの入力を受け付けます。
/// </summary>
public class PlayerModeController : MonoBehaviour
{
    // === Enum Definitions ===
    public enum WeaponMode { Melee, Beam }
    public enum ArmorMode { Normal = 0, Buster = 1, Speed = 2 }
    private const string SelectedArmorKey = "SelectedArmorIndex";

    [System.Serializable]
    public class ArmorStats
    {
        public string name;
        [Tooltip("ダメージ軽減率 (例: 1.0 = 変更なし, 0.5 = ダメージ半減)")]
        public float defenseMultiplier = 1.0f;
        [Tooltip("移動速度補正 (例: 1.5 = 1.5倍速)")]
        public float moveSpeedMultiplier = 1.0f;
        [Tooltip("エネルギー回復補正")]
        public float energyRecoveryMultiplier = 1.0f;
    }

    // 依存関係をパブリックプロパティまたはSerializeFieldで受け取る
    [Header("Dependencies")]
    public PlayerMovementAndCombat movementAndCombat; // ★必須：MovementAndCombatへの参照
    public PlayerInput playerInput; // Input Systemの参照

    [Header("Armor UI & Visuals")]
    public Image currentArmorIconImage;
    public Sprite[] armorSprites;
    public GameObject[] armorModels;

    [Header("Weapon UI")]
    public Image meleeWeaponIcon;
    public Text meleeWeaponText;
    public Image beamWeaponIcon;
    public Text beamWeaponText;
    public Color emphasizedColor = Color.white;
    public Color normalColor = new Color(0.5f, 0.5f, 0.5f);

    [Header("Energy Gauge Settings")]
    public float maxEnergy = 1000.0f;
    public float energyRecoveryRate = 10.0f;
    public float recoveryDelay = 1.0f;
    public Slider energySlider;
    public float energyConsumptionRate = 15.0f; // 移動/飛行時のエネルギー消費率

    [Header("HP UI Settings")]
    public Slider hPSlider;
    public Text hPText;

    [Header("Attack Energy Cost")]
    public float beamAttackEnergyCost = 30.0f; // ビーム攻撃コスト

    [Header("Armor Configurations")]
    // このリストにNormal, Buster, Speedの3つの設定を入れる
    public List<ArmorStats> armorConfigurations = new List<ArmorStats>
    {
        new ArmorStats { name = "Normal", defenseMultiplier = 1.0f, moveSpeedMultiplier = 1.0f, energyRecoveryMultiplier = 1.0f },
        new ArmorStats { name = "Buster Mode", defenseMultiplier = 1.5f, moveSpeedMultiplier = 0.8f, energyRecoveryMultiplier = 0.8f },
        new ArmorStats { name = "Speed Mode", defenseMultiplier = 0.75f, moveSpeedMultiplier = 1.5f, energyRecoveryMultiplier = 1.2f }
    };

    // === プライベート/キャッシュ変数 ===
    private ArmorMode _currentArmorMode = ArmorMode.Normal;
    private ArmorStats _currentArmorStats;
    private float _currentEnergy;
    private float _lastEnergyConsumptionTime;
    private WeaponMode _currentWeaponMode = WeaponMode.Melee;

    // パブリックプロパティ
    public ArmorMode currentArmorMode => _currentArmorMode;
    public WeaponMode currentWeaponMode => _currentWeaponMode;
    public ArmorStats currentArmorStats => _currentArmorStats;
    public float currentEnergy { get => _currentEnergy; private set => _currentEnergy = value; }

    // =======================================================
    // Unity Lifecycle Methods
    // =======================================================

    void Awake()
    {
        if (movementAndCombat == null)
        {
            Debug.LogError("PlayerMovementAndCombat が設定されていません。");
            enabled = false;
        }
    }

    void Start()
    {
        currentEnergy = maxEnergy;

        LoadAndSwitchArmor();
        UpdateUI();
    }

    void Update()
    {
        HandleEnergy();

        // キーボード/マウスによる入力処理 (Input Systemの代替/並行)
        HandleArmorSwitchInputKeyboard();
        HandleWeaponSwitchInputKeyboard();
        HandleAttackInputsMouse();
    }

    // =======================================================
    // Mode Switching Logic
    // =======================================================

    private void LoadAndSwitchArmor()
    {
        int selectedIndex = PlayerPrefs.GetInt(SelectedArmorKey, (int)ArmorMode.Normal);

        if (Enum.IsDefined(typeof(ArmorMode), selectedIndex) && selectedIndex < armorConfigurations.Count)
        {
            SwitchArmor((ArmorMode)selectedIndex, false);
        }
        else
        {
            SwitchArmor(ArmorMode.Normal, false);
        }
    }

    private void HandleArmorSwitchInputKeyboard()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchArmor(ArmorMode.Normal);
        else if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchArmor(ArmorMode.Buster);
        else if (Input.GetKeyDown(KeyCode.Alpha3)) SwitchArmor(ArmorMode.Speed);
    }

    private void HandleWeaponSwitchInputKeyboard()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            SwitchWeapon();
        }
    }

    public void SwitchArmor(ArmorMode newMode, bool shouldLog = true)
    {
        int index = (int)newMode;
        if (index < 0 || index >= armorConfigurations.Count)
        {
            Debug.LogError($"アーマーモード {newMode} の設定が見つかりません。");
            return;
        }

        if (_currentArmorMode == newMode && _currentArmorStats != null)
        {
            if (shouldLog) Debug.Log($"アーマーは既に **{newMode}** です。");
            return;
        }

        _currentArmorMode = newMode;
        _currentArmorStats = armorConfigurations[index];

        PlayerPrefs.SetInt(SelectedArmorKey, index);
        PlayerPrefs.Save();

        UpdateArmorVisuals(index);

        if (shouldLog) Debug.Log($"アーマーを切り替えました: **{_currentArmorStats.name}** ");
    }

    public void SwitchWeapon()
    {
        _currentWeaponMode = (_currentWeaponMode == WeaponMode.Melee) ? WeaponMode.Beam : WeaponMode.Melee;
        UpdateWeaponUIEmphasis();
        Debug.Log($"武器を切り替えました: **{_currentWeaponMode}**");
    }

    // =======================================================
    // Energy Management
    // =======================================================

    private void HandleEnergy()
    {
        if (Time.time >= _lastEnergyConsumptionTime + recoveryDelay)
        {
            float recoveryMultiplier = _currentArmorStats != null ? _currentArmorStats.energyRecoveryMultiplier : 1.0f;
            float recoveryRate = energyRecoveryRate * recoveryMultiplier;
            currentEnergy += recoveryRate * Time.deltaTime;
        }

        currentEnergy = Mathf.Clamp(currentEnergy, 0, maxEnergy);
        UpdateEnergyUI();
    }

    public void ConsumeEnergy(float amount)
    {
        currentEnergy -= amount;
        currentEnergy = Mathf.Clamp(currentEnergy, 0, maxEnergy);
        _lastEnergyConsumptionTime = Time.time;
    }

    public void ResetEnergyRecoveryTimer()
    {
        _lastEnergyConsumptionTime = Time.time;
    }

    // =======================================================
    // Attack Input (Attackの実行自体はMovementAndCombatに委任)
    // =======================================================

    private void HandleAttackInputsMouse()
    {
        if (Input.GetMouseButtonDown(0))
        {
            movementAndCombat.Attack();
        }
    }

    // =======================================================
    // UI Updates
    // =======================================================

    private void UpdateUI()
    {
        UpdateHPUI();
        UpdateEnergyUI();
        UpdateWeaponUIEmphasis();
    }

    private void UpdateArmorVisuals(int index)
    {
        if (currentArmorIconImage != null && armorSprites != null && index < armorSprites.Length)
        {
            currentArmorIconImage.sprite = armorSprites[index];
            currentArmorIconImage.enabled = true;
        }

        if (armorModels != null)
        {
            for (int i = 0; i < armorModels.Length; i++)
            {
                if (armorModels[i] != null)
                {
                    armorModels[i].SetActive(i == index);
                }
            }
        }
    }

    private void UpdateWeaponUIEmphasis()
    {
        bool isMelee = (_currentWeaponMode == WeaponMode.Melee);

        if (meleeWeaponIcon != null) meleeWeaponIcon.color = isMelee ? emphasizedColor : normalColor;
        if (beamWeaponIcon != null) beamWeaponIcon.color = isMelee ? normalColor : emphasizedColor;

        if (meleeWeaponText != null)
        {
            meleeWeaponText.text = "Melee";
            meleeWeaponText.color = isMelee ? emphasizedColor : normalColor;
        }
        if (beamWeaponText != null)
        {
            beamWeaponText.text = "Beam";
            beamWeaponText.color = isMelee ? normalColor : emphasizedColor;
        }
    }

    public void UpdateEnergyUI()
    {
        if (energySlider != null)
        {
            energySlider.value = currentEnergy / maxEnergy;
        }
    }

    public void UpdateHPUI()
    {
        if (hPSlider != null)
        {
            // HP値はMovementAndCombatから取得
            hPSlider.value = movementAndCombat.currentHP / movementAndCombat.maxHP;
        }

        if (hPText != null)
        {
            int currentHPInt = Mathf.CeilToInt(movementAndCombat.currentHP);
            int maxHPInt = Mathf.CeilToInt(movementAndCombat.maxHP);

            hPText.text = $"{currentHPInt} / {maxHPInt}";
        }
    }

    // =======================================================
    // Input System Event Handlers 【コントローラー入力の処理】
    // =======================================================

    public void OnFlyUp(InputAction.CallbackContext context)
    {
        if (context.performed) movementAndCombat.verticalInput = 1f;
        else if (context.canceled) movementAndCombat.verticalInput = 0f;
    }

    public void OnFlyDown(InputAction.CallbackContext context)
    {
        if (context.performed) movementAndCombat.verticalInput = -1f;
        else if (context.canceled) movementAndCombat.verticalInput = 0f;
    }

    public void OnBoost(InputAction.CallbackContext context)
    {
        if (context.performed) movementAndCombat.isBoosting = true;
        else if (context.canceled) movementAndCombat.isBoosting = false;
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            movementAndCombat.Attack();
        }
    }

    public void OnWeaponSwitch(InputAction.CallbackContext context)
    {
        if (context.started) SwitchWeapon();
    }

    public void OnDPad(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            Vector2 input = context.ReadValue<Vector2>();

            if (input.x > 0.5f) SwitchArmor(ArmorMode.Buster);
            else if (input.y > 0.5f) SwitchArmor(ArmorMode.Normal);
            else if (input.x < -0.5f) SwitchArmor(ArmorMode.Speed);
        }
    }

    public void OnMenu(InputAction.CallbackContext context)
    {
        if (context.started) Debug.Log("メニューボタンが押されました: 設定画面を開く");
    }
}