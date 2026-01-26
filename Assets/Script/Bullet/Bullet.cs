using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Bullet : MonoBehaviour
{
    [Header("移動設定")]
    public float speed = 50f;
    public float lifetime = 3f;

    [Header("ダメージ設定")]
    [Tooltip("弾が与える基本ダメージ量")]
    public float damageAmount = 50f;

    [Header("エフェクト設定")]
    public GameObject hitEffectPrefab;

    private Rigidbody rb;
    private bool isProcessed = false;

    void Awake()
    {
        // Rigidbodyを取得
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError($"{gameObject.name}: Rigidbody コンポーネントが必要です。", this);
            enabled = false;
        }
    }

    void Start()
    {
        // 一定時間後に自動消滅
        Destroy(gameObject, lifetime);

        if (rb != null)
        {
            // 向いている方向に力を加える
            // もし逆向きに飛ぶ場合は -transform.forward に変更してください
            rb.linearVelocity = transform.forward * speed;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 既に何かに当たっていたら処理しない
        if (isProcessed) return;

        // 1. プレイヤーに当たった場合
        if (other.CompareTag("Player"))
        {
            isProcessed = true;
            ApplyDamageToStatus(other.gameObject);
            HandleHitImpact();
        }
        // 2. 壁や特定のレイヤーに当たった場合
        else if (other.CompareTag("Wall") || other.gameObject.layer == LayerMask.NameToLayer("Object"))
        {
            isProcessed = true;
            HandleHitImpact();
        }
    }

    /// <summary>
    /// エフェクト生成と弾の削除を一括で行います
    /// </summary>
    private void HandleHitImpact()
    {
        if (hitEffectPrefab != null)
        {
            GameObject effect = Instantiate(hitEffectPrefab, transform.position, transform.rotation);
            Destroy(effect, 2f);
        }
        Destroy(gameObject);
    }

    /// <summary>
    /// ターゲットから PlayerStatus を探し、ダメージを適用します
    /// </summary>
    private void ApplyDamageToStatus(GameObject target)
    {
        // 自身の親、あるいは子から PlayerStatus を検索
        PlayerStatus status = target.GetComponentInParent<PlayerStatus>() ?? target.GetComponentInChildren<PlayerStatus>();

        if (status != null)
        {
            float defenseMultiplier = 1.0f; // デフォルトは等倍

            // 現在のアーマー（コントローラー）から防御倍率を取得する試み
            // 優先度やアーマーの状態に合わせて調整してください
            var balance = target.GetComponentInParent<BlanceController>() ?? target.GetComponentInChildren<BlanceController>();
            var buster = target.GetComponentInParent<BusterController>() ?? target.GetComponentInChildren<BusterController>();
            var speedCtrl = target.GetComponentInParent<SpeedController>() ?? target.GetComponentInChildren<SpeedController>();

            // ※ここに各コントローラーから防御力を取得するロジックを追加可能
            // 例: if (balance != null) defenseMultiplier = balance.defenseRate;

            status.TakeDamage(damageAmount, defenseMultiplier);
            Debug.Log($"[Bullet] {target.name} に {damageAmount} ダメージを適用。");
        }
        else
        {
            Debug.LogWarning($"{target.name} に Playerタグがありますが、PlayerStatusが見つかりません。");
        }
    }
}