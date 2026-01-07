using UnityEngine;

public class BulletDamageHandler : MonoBehaviour
{
    [Header("ダメージ設定")]
    [Tooltip("弾が与える基本ダメージ量")]
    public float damageAmount = 100f;

    [Header("エフェクト設定")]
    public GameObject hitEffectPrefab;

    private bool isProcessed = false;

    private void Start()
    {
        Destroy(gameObject, 5f);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isProcessed) return;

        if (other.CompareTag("Player"))
        {
            isProcessed = true;
            InstantiateHitEffect();
            ApplyDamageToStatus(other.gameObject); // 修正メソッドを呼び出し
            Destroy(gameObject);
        }
        else if (other.CompareTag("Wall") || other.gameObject.layer == LayerMask.NameToLayer("Object"))
        {
            isProcessed = true;
            InstantiateHitEffect();
            Destroy(gameObject);
        }
    }

    private void InstantiateHitEffect()
    {
        if (hitEffectPrefab != null)
        {
            GameObject effect = Instantiate(hitEffectPrefab, transform.position, transform.rotation);
            Destroy(effect, 2f);
        }
    }

    /// <summary>
    /// 直接 PlayerStatus を探してダメージを適用します。
    /// </summary>
    private void ApplyDamageToStatus(GameObject target)
    {
        // 1. PlayerStatus を取得 (自身、親、子から検索)
        PlayerStatus status = target.GetComponentInParent<PlayerStatus>() ?? target.GetComponentInChildren<PlayerStatus>();

        if (status != null)
        {
            float defense = 1.0f; // デフォルトは等倍

            // 2. 現在のアーマーの防御倍率を取得を試みる
            // 各コントローラーが存在するか確認し、存在すればその中から倍率を取得
            var blance = target.GetComponentInParent<BlanceController>() ?? target.GetComponentInChildren<BlanceController>();
            var buster = target.GetComponentInParent<BusterController>() ?? target.GetComponentInChildren<BusterController>();
            var speed = target.GetComponentInParent<SpeedController>() ?? target.GetComponentInChildren<SpeedController>();

            // ※各コントローラーに防御倍率を保持する仕組みがある場合の例
            // ここでは簡易的に、最初に見つかったコントローラーから取得する、
            // もしくは特定の「現在のアーマー管理クラス」から取得するのが理想的です。

            // 3. PlayerStatus の TakeDamage を実行
            status.TakeDamage(damageAmount, defense);

            Debug.Log($"[Bullet] PlayerStatusを検出。{damageAmount}ダメージ(倍率:{defense})を適用しました。");
        }
        else
        {
            Debug.LogWarning("Playerタグのオブジェクトを検出しましたが、PlayerStatusスクリプトが見つかりません。");
        }
    }
}