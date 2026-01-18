using UnityEngine;

public class EnemyBeamController : MonoBehaviour
{
    [Header("Visual Settings")]
    public float lifetime = 0.5f;
    public Transform beamVisual;

    [Header("Damage Settings")]
    public float damageAmount = 20f; // floatに統一

    private bool hasDealtDamage = false; // 重複ダメージ防止

    public void Fire(Vector3 startPoint, Vector3 endPoint, bool didHit, GameObject hitObject = null)
    {
        // 【デバッグ用】そもそも何かに当たっているかログを出す
        Debug.Log($"Beam Fire: didHit={didHit}, hitObject={(hitObject != null ? hitObject.name : "null")}");

        if (beamVisual != null) { /* 省略 */ }

        if (didHit && hitObject != null && !hasDealtDamage)
        {
            ApplyDamage(hitObject);
        }
        Destroy(gameObject, lifetime);
    }

    private void ApplyDamage(GameObject target)
    {
        // ヒットした本人、親、子のどこかに "Player" タグがあるか広く探す
        bool isPlayer = target.CompareTag("Player") ||
                        (target.transform.parent != null && target.transform.parent.CompareTag("Player")) ||
                        (target.GetComponentInChildren<PlayerStatus>() != null);

        if (isPlayer)
        {
            // 先ほどのBulletスクリプトと同じロジックで PlayerStatus を取得
            PlayerStatus status = target.GetComponentInParent<PlayerStatus>() ??
                                 target.GetComponentInChildren<PlayerStatus>() ??
                                 target.GetComponent<PlayerStatus>();

            if (status != null)
            {
                hasDealtDamage = true;

                // 防御力の取得（コントローラーを検索）
                float defense = 1.0f;
                var balance = target.GetComponentInParent<BlanceController>() ?? target.GetComponentInChildren<BlanceController>();
                var buster = target.GetComponentInParent<BusterController>() ?? target.GetComponentInChildren<BusterController>();
                var speed = target.GetComponentInParent<SpeedController>() ?? target.GetComponentInChildren<SpeedController>();

                // ※必要に応じてここで defense の値を書き換える

                status.TakeDamage(damageAmount, defense);
                Debug.Log($"[Beam] {target.name} にダメージ適用！");
            }
            else
            {
                Debug.LogWarning($"[Beam] Playerタグを検出しましたが、PlayerStatusが見つかりません: {target.name}");
            }
        }
    }
}