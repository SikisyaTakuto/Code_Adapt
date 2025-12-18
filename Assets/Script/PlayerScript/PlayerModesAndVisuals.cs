using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

public class PlayerModesAndVisuals : MonoBehaviour
{
    public enum WeaponMode { Attack1, Attack2 } // 名前を Attack1/2 に変更
    public enum ArmorMode { Normal = 0, Buster = 1, Speed = 2 }

    [System.Serializable]
    public class ArmorStats
    {
        public string name;
        public float defenseMultiplier = 1.0f;
        public float moveSpeedMultiplier = 1.0f;
        public float energyRecoveryMultiplier = 1.0f;
    }

    [Header("Armor Settings")]
    public List<ArmorStats> armorConfigurations;

    [Header("Armor UI & Visuals")]
    public Image currentArmorIconImage;
    public Sprite[] armorSprites;
    public GameObject[] armorModels;

    [Header("Weapon UI")] // 変数名も Attack1 / Attack2 に統一
    public Image attack1Icon;
    public Text attack1Text;
    public Image attack2Icon;
    public Text attack2Text;
    public Color emphasizedColor = Color.white;
    public Color normalColor = new Color(0.5f, 0.5f, 0.5f);

    private ArmorStats _currentArmorStats = new ArmorStats();
    private WeaponMode _currentWeaponMode = WeaponMode.Attack1;

    public WeaponMode CurrentWeaponMode => _currentWeaponMode;
    public ArmorStats CurrentArmorStats => _currentArmorStats;

    private bool _isUsingSlot1 = false; // 現在スロット1を使っているか

    void Awake()
    {
        LoadAndApplyDualArmor();
    }

    void Start()
    {
        UpdateWeaponUIEmphasis();
    }

    // --- PlayerModesAndVisuals.cs 内の修正 ---

    public void LoadAndApplyDualArmor()
    {
        int slot0 = PlayerPrefs.GetInt("SelectedArmor_Slot0", 0);
        int slot1 = PlayerPrefs.GetInt("SelectedArmor_Slot1", -1);

        // ステータスは「常に両方の合計」にする
        _currentArmorStats.defenseMultiplier = 1.0f;
        _currentArmorStats.moveSpeedMultiplier = 1.0f;
        _currentArmorStats.energyRecoveryMultiplier = 1.0f;

        ApplyStatsOnly(slot0);
        if (slot1 != -1) ApplyStatsOnly(slot1);

        // 見た目はスロット0（1つ目）を初期表示
        UpdateArmorVisualAndIcon(slot0);
    }

    // ステータス計算だけを行うメソッド
    private void ApplyStatsOnly(int index)
    {
        if (index < 0 || index >= armorConfigurations.Count) return;
        _currentArmorStats.defenseMultiplier *= armorConfigurations[index].defenseMultiplier;
        _currentArmorStats.moveSpeedMultiplier *= armorConfigurations[index].moveSpeedMultiplier;
        _currentArmorStats.energyRecoveryMultiplier *= armorConfigurations[index].energyRecoveryMultiplier;
    }

    // 引数に bool showModel を追加
    private void ApplyStats(int index, bool showModel)
    {
        if (index < 0 || index >= armorConfigurations.Count) return;

        // ステータス倍率はどちらのスロットでも計算する
        _currentArmorStats.defenseMultiplier *= armorConfigurations[index].defenseMultiplier;
        _currentArmorStats.moveSpeedMultiplier *= armorConfigurations[index].moveSpeedMultiplier;
        _currentArmorStats.energyRecoveryMultiplier *= armorConfigurations[index].energyRecoveryMultiplier;

        // 見た目（モデル）の表示は showModel が true の時だけ行う
        if (showModel && index < armorModels.Length && armorModels[index] != null)
        {
            armorModels[index].SetActive(true);
        }
    }

    private void UpdateArmorIcon(int index)
    {
        if (currentArmorIconImage != null && armorSprites != null && index < armorSprites.Length)
        {
            currentArmorIconImage.sprite = armorSprites[index];
            currentArmorIconImage.enabled = true;
        }
    }

    public void SwitchWeapon()
    {
        // Attack1 と Attack2 を切り替え
        _currentWeaponMode = (_currentWeaponMode == WeaponMode.Attack1) ? WeaponMode.Attack2 : WeaponMode.Attack1;
        UpdateWeaponUIEmphasis();
    }

    private void UpdateWeaponUIEmphasis()
    {
        bool isAttack1 = (_currentWeaponMode == WeaponMode.Attack1);

        // UIの色の切り替え
        if (attack1Icon != null) attack1Icon.color = isAttack1 ? emphasizedColor : normalColor;
        if (attack2Icon != null) attack2Icon.color = isAttack1 ? normalColor : emphasizedColor;

        if (attack1Text != null)
        {
            attack1Text.text = "Attack 1";
            attack1Text.color = isAttack1 ? emphasizedColor : normalColor;
        }
        if (attack2Text != null)
        {
            attack2Text.text = "Attack 2";
            attack2Text.color = isAttack1 ? normalColor : emphasizedColor;
        }
    }

    public void SwitchArmor(ArmorMode newMode)
    {
        PlayerPrefs.SetInt("SelectedArmor_Slot0", (int)newMode);
        PlayerPrefs.SetInt("SelectedArmor_Slot1", -1);
        LoadAndApplyDualArmor();
    }

    // スロット番号(0か1)を指定してアーマーを切り替える
    public void ChangeArmorBySlot(int slotNumber)
    {
        string key = (slotNumber == 0) ? "SelectedArmor_Slot0" : "SelectedArmor_Slot1";
        int armorIndex = PlayerPrefs.GetInt(key, -1);

        // スロット1が未設定（-1）の場合は切り替えない
        if (armorIndex == -1) return;

        // 見た目とアイコンを更新
        UpdateArmorVisualAndIcon(armorIndex);
    }

    // 見た目とアイコンだけを更新（ステータス合算は維持）
    private void UpdateArmorVisualAndIcon(int activeIndex)
    {
        if (armorModels != null)
        {
            foreach (var m in armorModels) if (m != null) m.SetActive(false);
            if (activeIndex >= 0 && activeIndex < armorModels.Length && armorModels[activeIndex] != null)
                armorModels[activeIndex].SetActive(true);
        }

        if (currentArmorIconImage != null && activeIndex >= 0 && activeIndex < armorSprites.Length)
        {
            currentArmorIconImage.sprite = armorSprites[activeIndex];
        }
    }
}