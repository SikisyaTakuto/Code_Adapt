// PlayerArmorController.cs
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI; // UIを扱うために追加

public class PlayerArmorController : MonoBehaviour
{
    // 現在選択されているアーマーのインデックス
    private int _currentArmorIndex = 0;
    public ArmorData CurrentArmorData { get; private set; }

    // PlayerController への参照
    private PlayerController _playerController;

    // UI要素への参照 (UIで強調表示するために必要)
    public List<Image> armorUIIndicators; // 例: 3つのアーマーアイコンのImageコンポーネント

    // プレイヤーのアーマーモデルの親Transform
    public Transform armorModelParent;

    // 現在表示されているアーマーモデルのインスタンス
    private GameObject _currentArmorInstance;

    private List<ArmorData> _selectedArmors; // ArmorManagerから渡される選択済みアーマーリスト

    void Awake()
    {
        _playerController = GetComponent<PlayerController>();
        if (_playerController == null)
        {
            Debug.LogError("PlayerArmorController: PlayerControllerが見つかりません。");
            enabled = false;
        }

        if (armorModelParent == null)
        {
            armorModelParent = this.transform; // デフォルトでプレイヤー自身のTransformを使用
            Debug.LogWarning("PlayerArmorController: Armor Model Parentが設定されていません。プレイヤー自身のTransformを使用します。");
        }
    }

    void Start()
    {
        // ArmorManagerから選択されたアーマーデータを取得
        if (ArmorManager.Instance != null && ArmorManager.Instance.selectedArmors.Count > 0)
        {
            _selectedArmors = ArmorManager.Instance.selectedArmors;
            SwitchArmor(0); // ゲーム開始時に最初のアーマーを装備
        }
        else
        {
            Debug.LogError("PlayerArmorController: ArmorManagerから選択されたアーマーデータがありません。"); 
            // デバッグ用に、全アーマーから適当に3つ選ぶなど、フォールバック処理を実装しても良い
            if (ArmorManager.Instance != null && ArmorManager.Instance.allAvailableArmors.Count >= 3)
            {
                _selectedArmors = new List<ArmorData> {
                    ArmorManager.Instance.allAvailableArmors[0],
                    ArmorManager.Instance.allAvailableArmors[1],
                    ArmorManager.Instance.allAvailableArmors[2]
                };
                SwitchArmor(0);
                Debug.LogWarning("デバッグ用: ArmorManagerから選択データがなかったため、デフォルトのアーマーを装備しました。");
            }
            else
            {
                enabled = false; // これ以上進めないのでスクリプトを無効化
                Debug.LogError("PlayerArmorController: 装備できるアーマーがありません。");
            }
        }
    }

    void Update()
    {
        // キー入力によるアーマー切り替え
        if (Input.GetKeyDown(KeyCode.Alpha1) && _selectedArmors.Count > 0)
        {
            SwitchArmor(0);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2) && _selectedArmors.Count > 1)
        {
            SwitchArmor(1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3) && _selectedArmors.Count > 2)
        {
            SwitchArmor(2);
        }
    }

    /// <summary>
    /// 指定されたインデックスのアーマーに切り替える
    /// </summary>
    /// <param name="index">切り替えたいアーマーのインデックス (0, 1, 2)</param>
    public void SwitchArmor(int index)
    {
        if (index < 0 || index >= _selectedArmors.Count)
        {
            Debug.LogWarning($"PlayerArmorController: 無効なアーマーインデックスです: {index}");
            return;
        }

        _currentArmorIndex = index;
        CurrentArmorData = _selectedArmors[_currentArmorIndex];
        Debug.Log($"アーマーを '{CurrentArmorData.armorName}' に切り替えました。");

        // 以前のアーマーモデルを破棄
        if (_currentArmorInstance != null)
        {
            Destroy(_currentArmorInstance);
        }

        // 新しいアーマーモデルを生成し、PlayerのTransformの子にする
        if (CurrentArmorData.armorPrefab != null)
        {
            _currentArmorInstance = Instantiate(CurrentArmorData.armorPrefab, armorModelParent);
            // モデルの位置や回転を調整する必要があるかもしれません (prefabの設定による)
            _currentArmorInstance.transform.localPosition = Vector3.zero;
            _currentArmorInstance.transform.localRotation = Quaternion.identity;
        }

        // PlayerControllerの能力値を更新
        ApplyArmorStatsToPlayerController(CurrentArmorData);

        // UIの強調表示を更新
        UpdateArmorUIHighlight();
    }

    /// <summary>
    /// 現在のアーマーデータに基づいてPlayerControllerの能力値を更新する
    /// </summary>
    /// <param name="armorData">適用するアーマーデータ</param>
    private void ApplyArmorStatsToPlayerController(ArmorData armorData)
    {
        if (_playerController == null) return;

        // 基本能力の変更
        _playerController.moveSpeed = _playerController.baseMoveSpeed * armorData.moveSpeedModifier;
        _playerController.boostMultiplier = _playerController.baseBoostMultiplier * armorData.boostMultiplierModifier;
        _playerController.verticalSpeed = _playerController.baseVerticalSpeed * armorData.verticalSpeedModifier;
        _playerController.energyConsumptionRate = _playerController.baseEnergyConsumptionRate * armorData.energyConsumptionModifier;
        _playerController.energyRecoveryRate = _playerController.baseEnergyRecoveryRate * armorData.energyRecoveryModifier;

        // 攻撃能力の変更
        // 攻撃力はここでまとめて変更するか、各攻撃メソッド内で現在のアーマーデータを参照して計算するようにする
        // 今回はPlayerController内の既存の攻撃ダメージ変数に直接影響を与える形にします。
        // もし攻撃の仕組みが複雑になるなら、各攻撃スクリプトがArmorDataを参照するように変更が必要
        _playerController.meleeAttackRange = _playerController.baseMeleeAttackRange * armorData.meleeAttackRangeModifier;
        _playerController.meleeDamage = _playerController.baseMeleeDamage * armorData.meleeAttackDamageModifier; // PlayerControllerにこの変数を追加
        _playerController.beamDamage = _playerController.baseBeamDamage * armorData.beamAttackDamageModifier; // PlayerControllerにこの変数を追加
        _playerController.bitAttackEnergyCost = _playerController.baseBitAttackEnergyCost * armorData.bitAttackEnergyCostModifier; // PlayerControllerにこの変数を追加

        // 飛行能力の制限 (バスターアーマーなど)
        _playerController.canFly = armorData.canFly;

        // 特殊能力 (例: ソードビットの有効/無効)
        _playerController.canUseSwordBitAttack = armorData.canUseSwordBit;

        // 武器の見た目を切り替える (primaryWeapon, secondaryWeapon のPrefabをロードしてアタッチ)
        // ここに武器のPrefabをインスタンス化して、プレイヤーの手にアタッチするロジックを追加
        _playerController.EquipWeapons(armorData.primaryWeapon, armorData.secondaryWeapon);
    }

    /// <summary>
    /// UIのアーマーアイコンの強調表示を更新する
    /// </summary>
    private void UpdateArmorUIHighlight()
    {
        if (armorUIIndicators == null || armorUIIndicators.Count == 0) return;

        for (int i = 0; i < armorUIIndicators.Count; i++)
        {
            if (armorUIIndicators[i] != null)
            {
                // 現在のアーマーであればハイライト、そうでなければ通常表示
                armorUIIndicators[i].color = (i == _currentArmorIndex) ? Color.yellow : Color.white; // 例: 黄色でハイライト
            }
        }
    }
}