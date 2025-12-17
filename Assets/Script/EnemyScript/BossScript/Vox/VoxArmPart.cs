// 各アームにこれを貼る
using UnityEngine;

public class VoxArmPart : MonoBehaviour
{
    public VoxController mainBoss;
    public int armIndex; // 0~7

    public void TakeDamage(float dmg)
    {
        mainBoss.DamageArm(armIndex, Mathf.CeilToInt(dmg));
        mainBoss.DamageBoss(Mathf.CeilToInt(dmg / 2)); // アームを叩くと本体にも少し入る、など
    }
}