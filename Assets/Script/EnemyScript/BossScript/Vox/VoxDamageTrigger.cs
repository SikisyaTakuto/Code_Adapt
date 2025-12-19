using UnityEngine;

public class VoxBoneDamage : MonoBehaviour
{
    [Header("ダメージ設定")]
    public float damage = 10f;

    // VoxControllerから一括でON/OFFされるフラグ
    [HideInInspector] public bool isAttacking = false;

    private void OnTriggerEnter(Collider other)
    {
        // 攻撃フェーズ中、かつ当たったのがプレイヤーなら
        if (isAttacking && other.CompareTag("Player"))
        {
            PlayerStatus status = other.GetComponent<PlayerStatus>();
            if (status == null) status = other.GetComponentInParent<PlayerStatus>();

            if (status != null)
            {
                // アーマー補正なし(1.0f)でダメージを与える
                status.TakeDamage(damage, 1.0f);
                Debug.Log($"{gameObject.name} がプレイヤーにダメージを与えました");
            }
        }
    }
}