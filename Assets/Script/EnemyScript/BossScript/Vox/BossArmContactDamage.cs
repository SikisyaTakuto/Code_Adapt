using UnityEngine;

public class BossArmContactDamage : MonoBehaviour
{
    [Header("Damage Settings")]
    [SerializeField] private float rawDamage = 50.0f;        // 与える基礎ダメージ
    [SerializeField] private float defenseMultiplier = 1.0f; // 防御倍率（通常は1.0）

    // 物理的な衝突（Collision）でダメージを与える場合
    private void OnCollisionEnter(Collision collision)
    {
        CheckAndApplyDamage(collision.gameObject);
    }

    // トリガー判定（すり抜け）でダメージを与える場合
    private void OnTriggerEnter(Collider other)
    {
        CheckAndApplyDamage(other.gameObject);
    }

    private void CheckAndApplyDamage(GameObject target)
    {
        // 衝突した相手がPlayerStatusを持っているか確認
        PlayerStatus player = target.GetComponent<PlayerStatus>();

        if (player != null)
        {
            // PlayerStatusのTakeDamageメソッドを呼び出す
            player.TakeDamage(rawDamage, defenseMultiplier);
            Debug.Log($"<color=orange>ボスのアームがプレイヤーに接触！ダメージ：{rawDamage}</color>");
        }
    }
}