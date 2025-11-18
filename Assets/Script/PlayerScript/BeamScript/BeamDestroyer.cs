using UnityEngine;

/// <summary>
/// 生成されたビームを指定時間後に破壊し、Raycastingで衝突判定とダメージ処理を行うスクリプト。
/// </summary>
public class BeamDestroyer : MonoBehaviour
{
    [Tooltip("ビームが自動的に破壊されるまでの時間 (秒)。")]
    public float lifetime = 5f;

    [Header("移動とダメージ設定")]
    [Tooltip("ビームの移動速度。")]
    public float beamSpeed = 40f;

    [Tooltip("ビームが与えるダメージ量。")]
    public int damageAmount = 10;

    // 前フレームの位置を保存し、Raycastingの始点とする
    private Vector3 previousPosition;

    private void Start()
    {
        // オブジェクトを一定時間後に破壊する
        Destroy(gameObject, lifetime);

        // 初期の位置を保存
        previousPosition = transform.position;
    }

    private void Update()
    {
        // 1. ビームを前方に移動させる (物理エンジン不使用)
        transform.position += transform.forward * beamSpeed * Time.deltaTime;

        // 2. Raycastingによる衝突判定
        // 前フレームの位置から現在の位置までの間に衝突が発生したかをチェック
        float distance = Vector3.Distance(previousPosition, transform.position);

        // RaycastNonAllocを使用して、メモリ割り当てを抑えることもできますが、
        // ここでは最もシンプルな Raycast を使用します。
        RaycastHit hit;

        // Raycast(始点, 方向, 衝突情報, 距離)
        if (Physics.Raycast(previousPosition, transform.forward, out hit, distance))
        {
            // Playerに当たった場合の処理
            if (hit.collider.CompareTag("Player"))
            {
                ApplyDamage(hit.collider.gameObject);
            }

            // 衝突位置にビームを移動させてから破壊（ビームが壁などを貫通しないように）
            transform.position = hit.point;
            Destroy(gameObject);
        }

        // 3. 次のフレームのために位置を更新
        previousPosition = transform.position;
    }

    /// <summary>
    /// 衝突したオブジェクトにダメージを与える処理。
    /// </summary>
    private void ApplyDamage(GameObject target)
    {
        // プレイヤーのHealthコンポーネントを取得 (PlayerHealthは自作が必要です)
        PlayerController player = target.GetComponent<PlayerController>();

        if (player != null)
        {
            player.TakeDamage(damageAmount);
        }
        else
        {
            Debug.LogWarning("PlayerタグのオブジェクトにPlayerHealthコンポーネントが見つかりません。", target);
        }
    }
}