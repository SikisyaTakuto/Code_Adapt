// ArmorData.cs
using UnityEngine;

// �E�N���b�N > Create > Armor System > Armor Data �ŃA�Z�b�g���쐬�ł���悤�ɂȂ�܂�
[CreateAssetMenu(fileName = "NewArmorData", menuName = "Armor System/Armor Data")]
public class ArmorData : ScriptableObject
{
    public string armorName; // ��: "�o�����X", "�o�X�^�[", "�X�s�[�h", "�s��"
    [TextArea(3, 5)] // Unity�G�f�B�^�ŕ����s�̓��͂��\�ɂȂ�܂�
    public string description; // �A�[�}�[�̐�����
    public GameObject armorPrefab; // �e�A�[�}�[�̊O�σ��f�����܂�Prefab

    [Header("Movement Modifiers")]
    public float moveSpeedModifier = 1.0f; // �ړ����x�␳ (1.0�Ńx�[�X���x)
    public float boostMultiplierModifier = 1.0f; // �u�[�X�g���x�{���␳
    public float verticalSpeedModifier = 1.0f; // �㏸/���~���x�␳
    public float energyConsumptionModifier = 1.0f; // �G�l���M�[����␳
    public float energyRecoveryModifier = 1.0f; // �G�l���M�[�񕜗��␳

    [Header("Attack Modifiers")]
    public float attackPowerModifier = 1.0f; // �U���͑S�ʂ̕␳
    public float meleeAttackRangeModifier = 1.0f; // �ߐڍU���͈͕␳
    public float meleeAttackDamageModifier = 1.0f; // �ߐڍU���_���[�W�␳
    public float beamAttackDamageModifier = 1.0f; // �r�[���U���_���[�W�␳
    public float bitAttackEnergyCostModifier = 1.0f; // �r�b�g�U���G�l���M�[����␳

    [Header("Weapon & Special Abilities")]
    // ������i��̓I�ȕ���Prefab��U�����W�b�N�ւ̎Q�ƂȂǁj
    public WeaponData primaryWeapon; // �啐�� (�r�[���T�[�x���A�o�X�^�[�p����Ȃ�)
    public WeaponData secondaryWeapon; // ������ (�r�[�����C�t���Ȃ�)

    public bool canUseSwordBit = false; // �\�[�h�r�b�g���g�p�\�� (�o�����X�p)
    public bool canFly = true; // ��s�\�� (�o�X�^�[��false�Ȃ�)
    // ... ���̑��A�A�[�}�[�ŗL�̃p�����[�^�����\�͂Ɋւ�����
}