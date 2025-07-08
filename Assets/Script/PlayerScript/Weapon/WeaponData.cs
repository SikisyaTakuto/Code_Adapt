// WeaponData.cs
using UnityEngine;

[CreateAssetMenu(fileName = "NewWeaponData", menuName = "Armor System/Weapon Data")]
public class WeaponData : ScriptableObject
{
    public string weaponName;
    public GameObject weaponPrefab; // ����̌����ڂ�Prefab (�v���C���[���f���̓���̈ʒu�ɃA�^�b�`)
    public float damage;
    public float fireRate;
    public float energyCost;
    public bool isMelee; // �ߐڕ��킩
    public bool isRanged; // ���������킩
    // ... ���̑��A����ŗL�̃p�����[�^ (��: �e���A�g�U�Ȃ�)
}