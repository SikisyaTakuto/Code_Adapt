using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

/// <summary>
/// プレイヤーのアーマー/武器モードの設定、およびビジュアル/アイコンの切り替えを制御します。
/// HP/エネルギー管理ロジックは含まれません。
/// </summary>
public class PlayerModesAndVisuals : MonoBehaviour
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

    [Header("Armor Settings")]
    public List<ArmorStats> armorConfigurations;

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

    // === プライベート/キャッシュ変数 ===
    private ArmorMode _currentArmorMode = ArmorMode.Normal;
    private ArmorStats _currentArmorStats;
    private WeaponMode _currentWeaponMode = WeaponMode.Melee;

    // === Public Properties (他のコンポーネントからアクセス用) ===
    public ArmorMode CurrentArmorMode => _currentArmorMode;
    public WeaponMode CurrentWeaponMode => _currentWeaponMode;
    public ArmorStats CurrentArmorStats => _currentArmorStats;

    // =======================================================
    // Unity Lifecycle
    // =======================================================

    void Awake()
    {
        // 依存コンポーネントがStartで参照できるように、ここでアーマーの初期化を行う
        LoadAndSwitchArmor(false);
    }

    void Start()
    {
        UpdateWeaponUIEmphasis();
    }

    // =======================================================
    // Initialization
    // =======================================================

    private void LoadAndSwitchArmor(bool shouldLog)
    {
        int selectedIndex = PlayerPrefs.GetInt(SelectedArmorKey, (int)ArmorMode.Normal);

        if (Enum.IsDefined(typeof(ArmorMode), selectedIndex) && selectedIndex < armorConfigurations.Count)
        {
            SwitchArmor((ArmorMode)selectedIndex, shouldLog);
        }
        else
        {
            SwitchArmor(ArmorMode.Normal, shouldLog);
            if (shouldLog) Debug.LogWarning($"不正なアーマーインデックス({selectedIndex})が検出されました。Normalモードを適用します。");
        }
    }

    // =======================================================
    // Armor and Weapon Switching
    // =======================================================

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

        if (shouldLog)
        {
            Debug.Log($"アーマーを切り替えました: **{_currentArmorStats.name}** ");
        }
    }

    public void SwitchWeapon()
    {
        _currentWeaponMode = (_currentWeaponMode == WeaponMode.Melee) ? WeaponMode.Beam : WeaponMode.Melee;
        UpdateWeaponUIEmphasis();
        Debug.Log($"武器を切り替えました: **{_currentWeaponMode}**");
    }

    // =======================================================
    // Visual Updates
    // =======================================================

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
}