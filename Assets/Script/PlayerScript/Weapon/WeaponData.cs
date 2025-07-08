// WeaponData.cs
using UnityEngine;

[CreateAssetMenu(fileName = "NewWeaponData", menuName = "Armor System/Weapon Data")]
public class WeaponData : ScriptableObject
{
    public string weaponName;
    public GameObject weaponPrefab; // 武器の見た目のPrefab (プレイヤーモデルの特定の位置にアタッチ)
    public float damage;
    public float fireRate;
    public float energyCost;
    public bool isMelee; // 近接武器か
    public bool isRanged; // 遠距離武器か
    // ... その他、武器固有のパラメータ (例: 弾速、拡散など)
}