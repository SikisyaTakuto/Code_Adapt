using UnityEngine;

public class ScorpionEnemy : MonoBehaviour
{
    // --- 公開パラメータ ---
    [Header("ターゲット設定")]
    public Transform playerTarget;               // PlayerのTransformをここに設定
    public float detectionRange = 15f;           // Playerを検出する範囲
    public Transform beamOrigin;                 // ビームの発射元となるTransform (サソリの尾の先など)

    [Header("攻撃設定")]
    public float attackRate = 2f;                // 1秒間に攻撃する回数 (例: 2fなら0.5秒ごとに攻撃)
    public GameObject beamPrefab;                 // 発射するビームのPrefab
    public float beamSpeed = 30f;                // ビームの速度

    // --- 内部変数 ---
    private float nextAttackTime = 0f;           // 次に攻撃可能な時間

    private void Update()
    {
        // 1. Playerの存在と距離チェック
        if (playerTarget == null)
        {
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);

        // 2. Playerが攻撃範囲内にいるか？
        if (distanceToPlayer <= detectionRange)
        {
            // 範囲内にいる場合、Playerの方を向く
            LookAtPlayer();

            // 3. 攻撃の実行
            if (Time.time >= nextAttackTime)
            {
                AttackPlayer();
                // 次の攻撃可能時間を設定
                nextAttackTime = Time.time + 1f / attackRate;
            }
        }
    }

    /// <summary>
    /// ドローン本体の向きをPlayerの方向へ向ける
    /// </summary>
    private void LookAtPlayer()
    {
        // Playerの方向を計算
        Vector3 targetDirection = playerTarget.position - transform.position;
        targetDirection.y = 0; // Y軸は無視して水平方向の回転のみ (サソリ本体が横に倒れないように)

        // 目標のQuaternionを計算
        Quaternion targetRotation = Quaternion.LookRotation(targetDirection);

        // スムーズに回転させる (ここではすぐに回転させています。滑らかにする場合は Lerp や Slerp を使用)
        transform.rotation = targetRotation;
    }

    /// <summary>
    /// ビームを発射する
    /// </summary>
    private void AttackPlayer()
    {
        if (beamOrigin == null || beamPrefab == null)
        {
            Debug.LogError("ビームの発射元またはPrefabが設定されていません。");
            return;
        }

        // ビームを生成
        GameObject beam = Instantiate(beamPrefab, beamOrigin.position, beamOrigin.rotation);

        // ビームを前方に発射
        Rigidbody rb = beam.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = beamOrigin.forward * beamSpeed;
        }
        else
        {
            // Rigidbodyがない場合、自分で移動させるコンポーネントをビームに付ける必要がある
            Debug.LogWarning("ビームPrefabにRigidbodyがありません。移動ロジックを追加してください。");
        }
    }

    // 範囲を可視化するためのGizmo (エディタでのみ表示)
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}