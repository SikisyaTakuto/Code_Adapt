using UnityEngine;

public class PlayerAttackController : MonoBehaviour
{
    // ========== 公開変数（Inspectorで調整可能） ==========

    public float detectRadius = 10f;         // 敵を検知する半径
    public float attackRange = 2f;           // 攻撃を開始できる距離
    public float moveSpeed = 5f;             // 移動速度（未使用、将来の拡張用）
    public float attackCooldown = 0.5f;      // 各攻撃のクールダウン時間（秒）
    public int maxCombo = 5;                 // 最大コンボ数
    public float comboResetTime = 2f;        // 入力がないとコンボがリセットされるまでの時間（秒）

    // ========== 内部変数（状態管理） ==========

    private Transform targetEnemy = null;    // 現在ターゲット中の敵（最も近い敵）
    private float attackTimer = 0f;          // 攻撃のクールダウンタイマー

    private int currentCombo = 0;            // 現在のコンボ段数（0〜maxCombo）
    private float comboResetTimer = 0f;      // コンボリセットまでのタイマー

    // ========== 毎フレーム実行される処理 ==========

    void Update()
    {
        // クールダウンやコンボタイマーの更新
        attackTimer -= Time.deltaTime;
        comboResetTimer -= Time.deltaTime;

        // 最も近い敵を検出し、targetEnemy に格納
        DetectClosestEnemy();

        // 敵が見つかった場合のみ処理
        if (targetEnemy != null)
        {
            // プレイヤーと敵の距離を計算
            float distance = Vector3.Distance(transform.position, targetEnemy.position);

            // 敵が攻撃範囲外にいる場合の処理
            if (distance > attackRange)
            {
                // 左クリックで一気に接近する（ダッシュのような動き）
                if (Input.GetMouseButtonDown(0))
                {
                    // 敵への方向ベクトルを正規化（向きだけ取得）
                    Vector3 dir = (targetEnemy.position - transform.position).normalized;

                    float dashDistance = 15f; // 一気に移動する距離（調整可能）

                    // 目標位置を計算（現在位置 + 移動方向 * ダッシュ距離）
                    Vector3 dashTarget = transform.position + dir * dashDistance;

                    // 敵との距離がダッシュ距離より短ければ、敵の位置までに制限
                    if (distance < dashDistance)
                    {
                        dashTarget = targetEnemy.position;
                    }

                    // プレイヤーの位置を更新（瞬時に移動）
                    transform.position = dashTarget;

                    // プレイヤーの向きを敵の方向に変更（即座に回転）
                    Vector3 lookDir = targetEnemy.position - transform.position;
                    lookDir.y = 0; // 水平方向のみに回転
                    if (lookDir != Vector3.zero)
                        transform.rotation = Quaternion.LookRotation(lookDir);
                }
            }
            else
            {
                // ===== 攻撃範囲内にいる場合の処理 =====

                // 一定時間入力がなければコンボをリセット
                if (comboResetTimer <= 0f)
                {
                    currentCombo = 0;
                }

                // 左クリックと攻撃クールダウンが終わっているかを確認
                if (Input.GetMouseButtonDown(0) && attackTimer <= 0f)
                {
                    // 最大コンボ数に達していなければ攻撃可能
                    if (currentCombo < maxCombo)
                    {
                        currentCombo++;                // コンボ段数を進める
                        Attack(currentCombo);          // 攻撃処理呼び出し
                        attackTimer = attackCooldown;  // クールダウンタイマーリセット
                        comboResetTimer = comboResetTime; // コンボリセットタイマーリセット
                    }
                }
            }
        }
        else
        {
            // 敵がいないときはコンボを強制的にリセット
            currentCombo = 0;
        }
    }

    // ========== 敵検出処理 ==========

    void DetectClosestEnemy()
    {
        // 指定半径内に存在するすべてのColliderを取得
        Collider[] hits = Physics.OverlapSphere(transform.position, detectRadius);

        float closestDist = Mathf.Infinity;
        Transform closestEnemy = null;

        // 検出されたColliderの中から、"Enemy"タグを持つ最も近い敵を探す
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                float dist = Vector3.Distance(transform.position, hit.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestEnemy = hit.transform;
                }
            }
        }

        // 最も近かった敵をターゲットに設定
        targetEnemy = closestEnemy;
    }

    // ========== 攻撃処理（引数はコンボ段数） ==========

    void Attack(int comboIndex)
    {
        // 現時点ではログ出力のみ（後でアニメーションやダメージ処理を追加予定）
        Debug.Log($"攻撃 {comboIndex} 段目！");
    }

    // ========== エディタ上で検知範囲・攻撃範囲を可視化 ==========

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectRadius); // 敵検知範囲

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, attackRange);  // 攻撃可能範囲
    }
}
