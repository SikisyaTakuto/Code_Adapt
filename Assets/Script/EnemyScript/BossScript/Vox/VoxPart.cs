// ファイル名: VoxPart.cs
using UnityEngine;

public class VoxPart : MonoBehaviour
{
    public VoxController mainController; // 親のVoxControllerをインスペクターでアタッチ
    public int armIndex; // このパーツが何番目のアームか (0?7)

    public void TakeDamage(float damage)
    {
        if (mainController != null)
        {
            // アーム自体のHPを減らす
            mainController.DamageArm(armIndex, Mathf.CeilToInt(damage));

            // 本体にも少し（あるいは全額）ダメージを与える
            mainController.DamageBoss(Mathf.CeilToInt(damage * 0.01f));
        }
    }
}