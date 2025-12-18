// ファイル名: VoxBodyPart.cs
using UnityEngine;

public class VoxBodyPart : MonoBehaviour
{
    [SerializeField] private VoxController mainController;
    [SerializeField] private float damageMultiplier = 1.0f; // 部位倍率（弱点なら2.0など）

    public void TakeDamage(float damage)
    {
        if (mainController != null)
        {
            mainController.TakeDamage(damage * damageMultiplier);
        }
    }
}