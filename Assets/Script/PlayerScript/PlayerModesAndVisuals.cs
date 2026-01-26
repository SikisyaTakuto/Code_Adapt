using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
        public string attack1Name = "Attack 1";
        public string attack2Name = "Attack 2";
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

    [Header("Switch Effects")]
    public GameObject switchEffectObject; // フェードさせる演出用オブジェクト
    public float effectDuration = 1.0f;   // 表示時間
    public Vector3 rotationSpeed = new Vector3(0, 0, 360); // 1秒間に回転する角度

    private Coroutine _effectCoroutine;

    // キャッシュ用に現在の表示アーマーのインデックスを保持
    private int _currentVisibleArmorIndex = 0;

    private ArmorStats _currentArmorStats = new ArmorStats();
    private WeaponMode _currentWeaponMode = WeaponMode.Attack1;

    public WeaponMode CurrentWeaponMode => _currentWeaponMode;
    public ArmorStats CurrentArmorStats => _currentArmorStats;

    private bool _isUsingSlot1 = false; // 現在スロット1を使っているか

    void Awake()
    {
        // 1. まずステータスを合算計算する
        RefreshTotalStats();

        // 2. 初期表示するアーマーを決定（Slot0に設定されているものを表示）
        int defaultArmor = PlayerPrefs.GetInt("SelectedArmor_Slot0", 0);

        // 3. 重要：見た目と技名UIを、計算結果に基づいて強制更新する
        UpdateArmorVisualAndIcon(defaultArmor);
    }

    void Start()
    {
        UpdateWeaponUIEmphasis();
    }

    public void RefreshTotalStats()
    {
        // 1. PlayerPrefsから現在選ばれているスロットの状態を取得
        int slot0 = PlayerPrefs.GetInt("SelectedArmor_Slot0", 0);
        int slot1 = PlayerPrefs.GetInt("SelectedArmor_Slot1", 1);

        // 2. 現在の「見た目（表示中）」のアーマーのインデックスを取得
        // ChangeArmorBySlotなどで _currentVisibleArmorIndex が更新されている前提です
        int activeIndex = _currentVisibleArmorIndex;

        // 3. ステータスの適用（合算ではなく、表示中のもののみを抽出）
        ApplyActiveStats(activeIndex);

        // 4. デバッグログを表示
        LogCurrentStatus(slot0, slot1);
    }

    /// <summary>
    /// 指定されたインデックスのアーマーステータスを現在のステータスとして完全に上書きします。
    /// </summary>
    private void ApplyActiveStats(int index)
    {
        if (index < 0 || index >= armorConfigurations.Count) return;

        var cfg = armorConfigurations[index];

        // 合算 (*=) ではなく代入 (=) に変更
        _currentArmorStats.defenseMultiplier = cfg.defenseMultiplier;
        _currentArmorStats.moveSpeedMultiplier = cfg.moveSpeedMultiplier;
        _currentArmorStats.energyRecoveryMultiplier = cfg.energyRecoveryMultiplier;

        // 技名も同期
        _currentArmorStats.attack1Name = cfg.attack1Name;
        _currentArmorStats.attack2Name = cfg.attack2Name;
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

        // 現在表示されているアーマーのステータスを取得
        ArmorStats activeStats = armorConfigurations[_currentVisibleArmorIndex];

        // UIの色の切り替え
        if (attack1Icon != null) attack1Icon.color = isAttack1 ? emphasizedColor : normalColor;
        if (attack2Icon != null) attack2Icon.color = isAttack1 ? normalColor : emphasizedColor;

        // ★技名のテキストをアーマーの設定から取得して表示
        if (attack1Text != null)
        {
            attack1Text.text = activeStats.attack1Name; // "Attack 1" ではなくアーマー固有の名前
            attack1Text.color = isAttack1 ? emphasizedColor : normalColor;
        }
        if (attack2Text != null)
        {
            attack2Text.text = activeStats.attack2Name; // "Attack 2" ではなくアーマー固有の名前
            attack2Text.color = isAttack1 ? normalColor : emphasizedColor;
        }
    }

    // スロット番号(0か1)を指定してアーマー（見た目と能力）を切り替える
    public void ChangeArmorBySlot(int slotNumber)
    {
        string key = (slotNumber == 0) ? "SelectedArmor_Slot0" : "SelectedArmor_Slot1";
        int armorIndex = PlayerPrefs.GetInt(key, -1);

        if (armorIndex == -1) return;

        // --- 追加：エフェクトの再生 ---
        if (switchEffectObject != null)
        {
            if (_effectCoroutine != null) StopCoroutine(_effectCoroutine);
            _effectCoroutine = StartCoroutine(PlaySwitchEffect());
        }

        // 1. まず見た目と現在のインデックスを更新
        UpdateArmorVisualAndIcon(armorIndex);

        // 2. そのインデックスに基づいてステータスを「上書き」更新
        RefreshTotalStats();
    }

    // エフェクトを表示して消すコルーチン
    private IEnumerator PlaySwitchEffect()
    {
        switchEffectObject.SetActive(true);

        // 回転をリセットしたい場合はここを有効化
        // switchEffectObject.transform.localRotation = Quaternion.identity;

        CanvasGroup cg = switchEffectObject.GetComponent<CanvasGroup>();

        float elapsed = 0;
        while (elapsed < effectDuration)
        {
            elapsed += Time.deltaTime;

            // --- 追加：回転処理 ---
            // deltaTimeを掛けることで、フレームレートに関係なく一定速度で回転します
            switchEffectObject.transform.Rotate(rotationSpeed * Time.deltaTime);

            if (cg != null)
            {
                cg.alpha = Mathf.Lerp(1f, 0f, elapsed / effectDuration);
            }
            yield return null;
        }

        switchEffectObject.SetActive(false);
        if (cg != null) cg.alpha = 1f;
    }

    // 見た目とアイコンだけを更新（ステータス合算は維持）
    private void UpdateArmorVisualAndIcon(int activeIndex)
    {
        _currentVisibleArmorIndex = activeIndex; // ★現在のインデックスを保存

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

        // ★アーマーが変わったので、技名表示も更新する
        UpdateWeaponUIEmphasis();
    }

    // --- LogCurrentStatus メソッドの修正 ---
    private void LogCurrentStatus(int s0, int s1)
    {
        // 修正ポイント1: armorName ではなく .name を参照する
        string armor0Name = (s0 >= 0 && s0 < armorConfigurations.Count) ? armorConfigurations[s0].name : "None";
        string armor1Name = (s1 >= 0 && s1 < armorConfigurations.Count) ? armorConfigurations[s1].name : "None";

        // 修正ポイント2: _currentMode の代わりに、現在表示されているアーマーの名前を取得
        string currentVisibleName = "None";
        if (_currentVisibleArmorIndex >= 0 && _currentVisibleArmorIndex < armorConfigurations.Count)
        {
            currentVisibleName = armorConfigurations[_currentVisibleArmorIndex].name;
        }

        Debug.Log($"<color=cyan><b>[Player Status Report]</b></color>\n" +
                  $"構成アーマー: [<color=yellow>{armor0Name}</color>] & [<color=yellow>{armor1Name}</color>]\n" +
                  $"表示中の外見: <color=orange>{currentVisibleName}</color>\n" + // _currentMode を currentVisibleName に
                  $"--- 合算ステータス ---\n" +
                  $"- 移動速度倍率: {_currentArmorStats.moveSpeedMultiplier:F2}x\n" +
                  $"- 防御倍率 (被ダメ): {_currentArmorStats.defenseMultiplier:F2}x\n" +
                  $"- エナジー回復倍率: {_currentArmorStats.energyRecoveryMultiplier:F2}x");
    }
}