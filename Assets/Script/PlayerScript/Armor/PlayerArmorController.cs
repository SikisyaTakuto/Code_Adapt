// PlayerArmorController.cs
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI; // UI���������߂ɒǉ�

public class PlayerArmorController : MonoBehaviour
{
    // ���ݑI������Ă���A�[�}�[�̃C���f�b�N�X
    private int _currentArmorIndex = 0;
    public ArmorData CurrentArmorData { get; private set; }

    // PlayerController �ւ̎Q��
    private PlayerController _playerController;

    // UI�v�f�ւ̎Q�� (UI�ŋ����\�����邽�߂ɕK�v)
    public List<Image> armorUIIndicators; // ��: 3�̃A�[�}�[�A�C�R����Image�R���|�[�l���g

    // �v���C���[�̃A�[�}�[���f���̐eTransform
    public Transform armorModelParent;

    // ���ݕ\������Ă���A�[�}�[���f���̃C���X�^���X
    private GameObject _currentArmorInstance;

    private List<ArmorData> _selectedArmors; // ArmorManager����n�����I���ς݃A�[�}�[���X�g

    void Awake()
    {
        _playerController = GetComponent<PlayerController>();
        if (_playerController == null)
        {
            Debug.LogError("PlayerArmorController: PlayerController��������܂���B");
            enabled = false;
        }

        if (armorModelParent == null)
        {
            armorModelParent = this.transform; // �f�t�H���g�Ńv���C���[���g��Transform���g�p
            Debug.LogWarning("PlayerArmorController: Armor Model Parent���ݒ肳��Ă��܂���B�v���C���[���g��Transform���g�p���܂��B");
        }
    }

    void Start()
    {
        // ArmorManager����I�����ꂽ�A�[�}�[�f�[�^���擾
        if (ArmorManager.Instance != null && ArmorManager.Instance.selectedArmors.Count > 0)
        {
            _selectedArmors = ArmorManager.Instance.selectedArmors;
            SwitchArmor(0); // �Q�[���J�n���ɍŏ��̃A�[�}�[�𑕔�
        }
        else
        {
            Debug.LogError("PlayerArmorController: ArmorManager����I�����ꂽ�A�[�}�[�f�[�^������܂���B"); 
            // �f�o�b�O�p�ɁA�S�A�[�}�[����K����3�I�ԂȂǁA�t�H�[���o�b�N�������������Ă��ǂ�
            if (ArmorManager.Instance != null && ArmorManager.Instance.allAvailableArmors.Count >= 3)
            {
                _selectedArmors = new List<ArmorData> {
                    ArmorManager.Instance.allAvailableArmors[0],
                    ArmorManager.Instance.allAvailableArmors[1],
                    ArmorManager.Instance.allAvailableArmors[2]
                };
                SwitchArmor(0);
                Debug.LogWarning("�f�o�b�O�p: ArmorManager����I���f�[�^���Ȃ��������߁A�f�t�H���g�̃A�[�}�[�𑕔����܂����B");
            }
            else
            {
                enabled = false; // ����ȏ�i�߂Ȃ��̂ŃX�N���v�g�𖳌���
                Debug.LogError("PlayerArmorController: �����ł���A�[�}�[������܂���B");
            }
        }
    }

    void Update()
    {
        // �L�[���͂ɂ��A�[�}�[�؂�ւ�
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
    /// �w�肳�ꂽ�C���f�b�N�X�̃A�[�}�[�ɐ؂�ւ���
    /// </summary>
    /// <param name="index">�؂�ւ������A�[�}�[�̃C���f�b�N�X (0, 1, 2)</param>
    public void SwitchArmor(int index)
    {
        if (index < 0 || index >= _selectedArmors.Count)
        {
            Debug.LogWarning($"PlayerArmorController: �����ȃA�[�}�[�C���f�b�N�X�ł�: {index}");
            return;
        }

        _currentArmorIndex = index;
        CurrentArmorData = _selectedArmors[_currentArmorIndex];
        Debug.Log($"�A�[�}�[�� '{CurrentArmorData.armorName}' �ɐ؂�ւ��܂����B");

        // �ȑO�̃A�[�}�[���f����j��
        if (_currentArmorInstance != null)
        {
            Destroy(_currentArmorInstance);
        }

        // �V�����A�[�}�[���f���𐶐����APlayer��Transform�̎q�ɂ���
        if (CurrentArmorData.armorPrefab != null)
        {
            _currentArmorInstance = Instantiate(CurrentArmorData.armorPrefab, armorModelParent);
            // ���f���̈ʒu���]�𒲐�����K�v�����邩������܂��� (prefab�̐ݒ�ɂ��)
            _currentArmorInstance.transform.localPosition = Vector3.zero;
            _currentArmorInstance.transform.localRotation = Quaternion.identity;
        }

        // PlayerController�̔\�͒l���X�V
        ApplyArmorStatsToPlayerController(CurrentArmorData);

        // UI�̋����\�����X�V
        UpdateArmorUIHighlight();
    }

    /// <summary>
    /// ���݂̃A�[�}�[�f�[�^�Ɋ�Â���PlayerController�̔\�͒l���X�V����
    /// </summary>
    /// <param name="armorData">�K�p����A�[�}�[�f�[�^</param>
    private void ApplyArmorStatsToPlayerController(ArmorData armorData)
    {
        if (_playerController == null) return;

        // ��{�\�͂̕ύX
        _playerController.moveSpeed = _playerController.baseMoveSpeed * armorData.moveSpeedModifier;
        _playerController.boostMultiplier = _playerController.baseBoostMultiplier * armorData.boostMultiplierModifier;
        _playerController.verticalSpeed = _playerController.baseVerticalSpeed * armorData.verticalSpeedModifier;
        _playerController.energyConsumptionRate = _playerController.baseEnergyConsumptionRate * armorData.energyConsumptionModifier;
        _playerController.energyRecoveryRate = _playerController.baseEnergyRecoveryRate * armorData.energyRecoveryModifier;

        // �U���\�͂̕ύX
        // �U���͂͂����ł܂Ƃ߂ĕύX���邩�A�e�U�����\�b�h���Ō��݂̃A�[�}�[�f�[�^���Q�Ƃ��Čv�Z����悤�ɂ���
        // �����PlayerController���̊����̍U���_���[�W�ϐ��ɒ��ډe����^����`�ɂ��܂��B
        // �����U���̎d�g�݂����G�ɂȂ�Ȃ�A�e�U���X�N���v�g��ArmorData���Q�Ƃ���悤�ɕύX���K�v
        _playerController.meleeAttackRange = _playerController.baseMeleeAttackRange * armorData.meleeAttackRangeModifier;
        _playerController.meleeDamage = _playerController.baseMeleeDamage * armorData.meleeAttackDamageModifier; // PlayerController�ɂ��̕ϐ���ǉ�
        _playerController.beamDamage = _playerController.baseBeamDamage * armorData.beamAttackDamageModifier; // PlayerController�ɂ��̕ϐ���ǉ�
        _playerController.bitAttackEnergyCost = _playerController.baseBitAttackEnergyCost * armorData.bitAttackEnergyCostModifier; // PlayerController�ɂ��̕ϐ���ǉ�

        // ��s�\�͂̐��� (�o�X�^�[�A�[�}�[�Ȃ�)
        _playerController.canFly = armorData.canFly;

        // ����\�� (��: �\�[�h�r�b�g�̗L��/����)
        _playerController.canUseSwordBitAttack = armorData.canUseSwordBit;

        // ����̌����ڂ�؂�ւ��� (primaryWeapon, secondaryWeapon ��Prefab�����[�h���ăA�^�b�`)
        // �����ɕ����Prefab���C���X�^���X�����āA�v���C���[�̎�ɃA�^�b�`���郍�W�b�N��ǉ�
        _playerController.EquipWeapons(armorData.primaryWeapon, armorData.secondaryWeapon);
    }

    /// <summary>
    /// UI�̃A�[�}�[�A�C�R���̋����\�����X�V����
    /// </summary>
    private void UpdateArmorUIHighlight()
    {
        if (armorUIIndicators == null || armorUIIndicators.Count == 0) return;

        for (int i = 0; i < armorUIIndicators.Count; i++)
        {
            if (armorUIIndicators[i] != null)
            {
                // ���݂̃A�[�}�[�ł���΃n�C���C�g�A�����łȂ���Βʏ�\��
                armorUIIndicators[i].color = (i == _currentArmorIndex) ? Color.yellow : Color.white; // ��: ���F�Ńn�C���C�g
            }
        }
    }
}