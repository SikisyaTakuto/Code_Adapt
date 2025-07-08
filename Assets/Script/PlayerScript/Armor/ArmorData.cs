// ArmorData.cs
using UnityEngine;

// 右クリック > Create > Armor System > Armor Data でアセットを作成できるようになります
[CreateAssetMenu(fileName = "NewArmorData", menuName = "Armor System/Armor Data")]
public class ArmorData : ScriptableObject
{
    public string armorName; // 例: "バランス", "バスター", "スピード", "不明"
    [TextArea(3, 5)] // Unityエディタで複数行の入力が可能になります
    public string description; // アーマーの説明文
    public GameObject armorPrefab; // 各アーマーの外観モデルを含むPrefab

    [Header("Movement Modifiers")]
    public float moveSpeedModifier = 1.0f; // 移動速度補正 (1.0でベース速度)
    public float boostMultiplierModifier = 1.0f; // ブースト速度倍率補正
    public float verticalSpeedModifier = 1.0f; // 上昇/下降速度補正
    public float energyConsumptionModifier = 1.0f; // エネルギー消費率補正
    public float energyRecoveryModifier = 1.0f; // エネルギー回復率補正

    [Header("Attack Modifiers")]
    public float attackPowerModifier = 1.0f; // 攻撃力全般の補正
    public float meleeAttackRangeModifier = 1.0f; // 近接攻撃範囲補正
    public float meleeAttackDamageModifier = 1.0f; // 近接攻撃ダメージ補正
    public float beamAttackDamageModifier = 1.0f; // ビーム攻撃ダメージ補正
    public float bitAttackEnergyCostModifier = 1.0f; // ビット攻撃エネルギー消費補正

    [Header("Weapon & Special Abilities")]
    // 武器情報（具体的な武器Prefabや攻撃ロジックへの参照など）
    public WeaponData primaryWeapon; // 主武器 (ビームサーベル、バスター用武器など)
    public WeaponData secondaryWeapon; // 副武器 (ビームライフルなど)

    public bool canUseSwordBit = false; // ソードビットが使用可能か (バランス用)
    public bool canFly = true; // 飛行可能か (バスターはfalseなど)
    // ... その他、アーマー固有のパラメータや特殊能力に関する情報
}