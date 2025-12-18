using UnityEngine;

public class BossMainArmHit : MonoBehaviour
{
    [Header("攻撃設定")]
    [SerializeField] private float damage = 100f; // プレイヤーへのダメージ量
    [SerializeField] private float knockbackForce = 20f; // 吹き飛ばし力

    // 攻撃中かどうか（VoxControllerのアニメーションイベントから制御）
    public bool isAttacking = false;

    private void OnTriggerEnter(Collider other)
    {
        // 1. 攻撃中で、かつ相手がプレイヤーであるかチェック
        if (isAttacking && other.CompareTag("Player"))
        {
            // 2. 直接 PlayerStatus コンポーネントを探す
            // 親オブジェクトについている場合を考慮して GetComponentInParent も試す
            PlayerStatus playerStatus = other.GetComponent<PlayerStatus>();
            if (playerStatus == null)
            {
                playerStatus = other.GetComponentInParent<PlayerStatus>();
            }

            if (playerStatus != null)
            {
                // 3. 直接ダメージを与える
                // 防御力補正（defenseMultiplier）は一旦 1.0f（等倍）で送信
                playerStatus.TakeDamage(damage, 1.0f);

                Debug.Log($"<color=red>ボスのメインアームがヒット！ 残りHP: {playerStatus.CurrentHP}</color>");

                // 4. ノックバック（物理的な弾き飛ばし）
                Rigidbody rb = other.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    Vector3 knockbackDir = (other.transform.position - transform.position).normalized;
                    knockbackDir.y = 0.5f; // 少し上方向に飛ばす
                    rb.AddForce(knockbackDir * knockbackForce, ForceMode.Impulse);
                }
            }
        }
    }
}