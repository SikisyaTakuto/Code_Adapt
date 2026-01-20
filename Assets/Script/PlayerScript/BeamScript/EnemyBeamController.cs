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
        // 1. 当たったオブジェクトそのもの、あるいはその親や子のどこかに PlayerStatus があるか探す
        // これにより、タグの有無に依存せず「ダメージを受けられる存在か」で判定できます
        PlayerStatus status = target.GetComponentInParent<PlayerStatus>() ??
                             target.GetComponentInChildren<PlayerStatus>() ??
                             target.GetComponent<PlayerStatus>();

        // 2. status が見つかれば、それはプレイヤー（またはダメージ対象）であると確定
        if (status != null)
        {
            hasDealtDamage = true;

            // 防御倍率の計算
            float defense = 1.0f;

            // 各コントローラーの取得（親・子・自身から検索）
            var balance = target.GetComponentInParent<BlanceController>() ?? target.GetComponentInChildren<BlanceController>() ?? target.GetComponent<BlanceController>();
            var buster = target.GetComponentInParent<BusterController>() ?? target.GetComponentInChildren<BusterController>() ?? target.GetComponent<BusterController>();
            var speed = target.GetComponentInParent<SpeedController>() ?? target.GetComponentInChildren<SpeedController>() ?? target.GetComponent<SpeedController>();

            // ここで各アーマーごとの防御倍率処理を記述可能（例）
            // if (balance != null) defense = balance.defenseRate;

            // ダメージ適用
            status.TakeDamage(damageAmount, defense);

            Debug.Log($"[Beam] {status.gameObject.name} にダメージ適用！ (HitObject: {target.name})");
        }
        else
        {
            // PlayerStatus が見つからなかった場合は、プレイヤー以外（壁など）に当たったとみなす
            Debug.Log($"[Beam] プレイヤー以外にヒット: {target.name}");
        }
    }
}