using UnityEngine;
using System.Collections; // コルーチンを使用するために必要

public class DroneEnemy : MonoBehaviour
{
    // --- HP設定 ---
    [Header("ヘルス設定")]
    public float maxHealth = 100f; // 最大HP
    private float currentHealth;    // 現在のHP
    private bool isDead = false;    // 死亡フラグ

    // ?? 新規追加: 爆発エフェクトのPrefab
    [Header("エフェクト設定")]
    public GameObject explosionPrefab;

    // --- 公開パラメータ ---
    [Header("ターゲット設定")]
    public Transform playerTarget;              // PlayerのTransformをここに設定
    public float detectionRange = 15f;          // Playerを検出する範囲
    public Transform beamOrigin;                // 弾の発射元となるTransform

    [Range(0, 180)]
    public float attackAngle = 30f;             // 攻撃可能な正面視野角（全角）

    [Header("攻撃設定")]
    public float attackRate = 5f;               // 弾と弾の間の間隔計算に使用 (例: 1/5 = 0.2秒間隔)
    public GameObject beamPrefab;               // 発射する弾のPrefab
    public float beamSpeed = 40f;               // 弾の速度

    [Header("バースト攻撃設定")]
    public int bulletsPerBurst = 5;
    public float burstCooldownTime = 2f;

    [Header("硬直設定")]
    public float hardStopDuration = 0.5f;

    [Header("浮遊移動設定")]
    public float rotationSpeed = 5f;             // Player追跡時の回転速度（ドローン本体用）
    public float gunRotationSpeed = 20f;
    public float hoverAltitude = 5f;
    public float driftSpeed = 1f;
    public float driftRange = 5f;
    public float altitudeCorrectionSpeed = 2f;

    // ?? 新規追加: 障害物回避のための設定
    [Header("障害物回避設定")]
    public LayerMask obstacleLayer;              // 障害物となるレイヤー
    public float avoidanceCheckDistance = 3f;    // 前方チェック距離
    public float wallHitResetRange = 1f;         // 壁に接触したと見なす距離 (衝突を防ぐために大きめに)

    // --- 内部変数 ---
    private float nextAttackTime = 0f;
    private float hardStopEndTime = 0f;
    private Animator animator;
    private Vector3 currentDriftTarget;

    private bool isAttacking = false;

    private void Awake()
    {
        currentHealth = maxHealth;

        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogWarning("Animator componentが見つかりません。敵にAnimatorをアタッチしてください。");
        }

        SetNewDriftTarget();
    }

    private void Update()
    {
        // デバッグ用コード: OキーでHPを0にする
        if (Input.GetKeyDown(KeyCode.O))
        {
            TakeDamage(maxHealth);
            return;
        }

        // 死亡時、硬直中、またはターゲットがない場合は処理をスキップ
        if (isDead || playerTarget == null || Time.time < hardStopEndTime)
        {
            return;
        }

        // ?? 新規追加: 移動前に前方チェックと目標地点のリセット
        CheckForObstaclesAndResetTarget();

        // ?? 銃口を常にPlayerに向ける
        RotateGunToPlayer();

        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);

        // 2. Playerが攻撃範囲内にいるか？
        if (distanceToPlayer <= detectionRange)
        {
            // ドローン本体をPlayerに向ける
            LookAtPlayer();

            // 攻撃中でなければ、バースト攻撃を開始
            if (!isAttacking && IsPlayerInFrontView())
            {
                StartCoroutine(BurstAttackSequence());
            }
        }

        // 常に空中で浮遊移動
        DriftHover();
    }

    // -------------------------------------------------------------------
    //                       ドローン本体の回転 (Y軸のみ)
    // -------------------------------------------------------------------

    /// <summary>
    /// ドローン本体の向きをPlayerの方向へ向ける（スムーズな回転）
    /// </summary>
    private void LookAtPlayer()
    {
        // ... (元のコードと変更なし。ドローン本体のY軸回転) ...
        Vector3 targetDirection = playerTarget.position - transform.position;
        targetDirection.y = 0; // 空中敵なので、水平回転のみ

        Quaternion targetRotation = Quaternion.LookRotation(targetDirection);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            Time.deltaTime * rotationSpeed
        );
    }

    // -------------------------------------------------------------------
    //                       銃口の回転処理 (新規追加)
    // -------------------------------------------------------------------

    /// <summary>
    /// 銃口 (beamOrigin) をPlayerのTransformへ向けて回転させる（全軸回転）
    /// </summary>
    private void RotateGunToPlayer()
    {
        if (beamOrigin == null || playerTarget == null) return;

        // Playerの位置から銃口の位置を引いて、方向ベクトルを取得
        Vector3 targetDirection = playerTarget.position - beamOrigin.position;

        // 目標とする回転 (Playerの方を向く)
        Quaternion targetRotation = Quaternion.LookRotation(targetDirection);

        // スムーズに回転させる
        beamOrigin.rotation = Quaternion.Slerp(
            beamOrigin.rotation,
            targetRotation,
            Time.deltaTime * gunRotationSpeed
        );
    }

    // -------------------------------------------------------------------
    //                       攻撃処理 (バーストシステム)
    // -------------------------------------------------------------------

    private IEnumerator BurstAttackSequence()
    {
        isAttacking = true;

        float shotDelay = 0.5f / attackRate;

        // 1. バースト攻撃
        for (int i = 0; i < bulletsPerBurst; i++)
        {
            AttackSingleBullet();

            yield return new WaitForSeconds(shotDelay);
        }

        // 2. バースト後のクールタイム
        yield return new WaitForSeconds(burstCooldownTime);

        isAttacking = false;
    }

    private void AttackSingleBullet()
    {
        if (beamOrigin == null || beamPrefab == null)
        {
            Debug.LogError("発射元またはPrefabが設定されていません。");
            return;
        }

        // ?? 銃口が既にPlayerの方向を向いているため、beamOrigin.forwardを直接使用
        Quaternion bulletRotation = beamOrigin.rotation;

        GameObject bullet = Instantiate(beamPrefab, beamOrigin.position, bulletRotation);

        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = bullet.transform.forward * beamSpeed;
        }
        else
        {
            Debug.LogWarning("弾PrefabにRigidbodyがありません。");
        }
    }

    // -------------------------------------------------------------------
    //                       空中移動処理 (修正・追加)
    // -------------------------------------------------------------------

    /// <summary>
    /// ドローンの移動目標が障害物内にないかをチェックし、衝突しそうなら目標をリセット
    /// </summary>
    private void CheckForObstaclesAndResetTarget()
    {
        // currentDriftTargetへのベクトル
        Vector3 directionToTarget = (currentDriftTarget - transform.position);

        // 1. Raycastで目標地点の方向に障害物があるかチェック
        if (Physics.Raycast(transform.position, directionToTarget.normalized, out RaycastHit hit, avoidanceCheckDistance, obstacleLayer))
        {
            // ターゲットの方向が壁！新しいターゲットを設定
            Debug.Log("?? 目標方向 (" + hit.collider.name + ") に壁を検出。目標をリセットします。", gameObject);
            SetNewDriftTarget();
            return;
        }

        // 2. 目標地点自体が壁の中や壁の奥になっていないかをチェック (OverlapSphere)
        if (Physics.CheckSphere(currentDriftTarget, wallHitResetRange, obstacleLayer))
        {
            Debug.Log("?? 現在の目標地点が壁の中に設定されているため、目標をリセットします。", gameObject);
            SetNewDriftTarget();
            return;
        }

        // 3. (保険): ドローン自身の前方が壁に接触しているかチェック
        // ドローンが進行方向に壁を向いていると想定してチェック
        if (Physics.Raycast(transform.position, transform.forward, avoidanceCheckDistance * 0.5f, obstacleLayer))
        {
            Debug.Log("?? ドローン直前の進行方向が壁にぶつかっています。目標をリセットします。", gameObject);
            SetNewDriftTarget();
        }
    }

    private void DriftHover()
    {
        Vector3 currentPos = transform.position;

        // 1. 高度補正 (Y軸の移動)
        float targetY = hoverAltitude;
        float newY = Mathf.Lerp(currentPos.y, targetY, Time.deltaTime * altitudeCorrectionSpeed);

        // 2. 水平方向の移動 (X/Z軸の浮遊)
        Vector3 horizontalTarget = new Vector3(currentDriftTarget.x, newY, currentDriftTarget.z);

        transform.position = Vector3.MoveTowards(
            currentPos,
            horizontalTarget,
            Time.deltaTime * driftSpeed
        );

        // 3. 目標地点に到達したら新しい目標を設定
        if (Vector3.Distance(new Vector3(currentPos.x, 0, currentPos.z), new Vector3(currentDriftTarget.x, 0, currentDriftTarget.z)) < 0.5f)
        {
            SetNewDriftTarget();
        }
    }

    private void SetNewDriftTarget()
    {
        Vector3 newTarget;
        int attempts = 0;
        const int maxAttempts = 10; // ループの無限化を防ぐ

        // 衝突しない目標地点を見つけるまで繰り返す
        do
        {
            Vector2 randomCircle = Random.insideUnitCircle * driftRange;

            newTarget = new Vector3(
                transform.position.x + randomCircle.x,
                hoverAltitude,
                transform.position.z + randomCircle.y
            );

            attempts++;

            // ?? 修正: 新しい目標地点が障害物内にないかチェック
            // CheckSphereで目標地点周辺に障害物がないか確認する
        } while (Physics.CheckSphere(newTarget, wallHitResetRange, obstacleLayer) && attempts < maxAttempts);


        if (attempts >= maxAttempts)
        {
            Debug.LogWarning("目標地点を見つけるのに失敗しました。現在地周辺を維持します。", gameObject);
            // 見つからなかった場合は、現在の位置を目標として、移動を停止させる
            currentDriftTarget = transform.position;
        }
        else
        {
            currentDriftTarget = newTarget;
            // Y座標を無視して現在の位置からのベクトルを計算
            Vector3 horizontalDirection = new Vector3(currentDriftTarget.x, transform.position.y, currentDriftTarget.z) - transform.position;
            // 見つけた目標地点へ向かってドローンの向きを補正する
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(horizontalDirection), Time.deltaTime * rotationSpeed);
        }
    }

    // -------------------------------------------------------------------
    //                       ヘルスとダメージ処理
    // -------------------------------------------------------------------

    public void TakeDamage(float damageAmount)
    {
        if (isDead) return;

        currentHealth -= damageAmount;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// 死亡処理
    /// </summary>
    private void Die()
    {
        if (isDead) return;

        isDead = true;
        Debug.Log(gameObject.name + "は破壊されました！");

        // ?? 爆発エフェクトのインスタンス化と再生
        if (explosionPrefab != null)
        {
            // ドローンの位置に生成
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        }

        if (animator != null)
        {
            animator.SetBool("Dead", true);
        }

        // コルーチンを停止して、弾が連射されるのを防ぐ
        StopAllCoroutines();

        // 死亡後、即座にドローン本体のレンダラーやコライダーを無効化
        // (ここでは簡単なDestroyを使用)
        Destroy(gameObject, 0.1f); // エフェクトが生成されたらすぐにドローン本体を削除
    }

    // -------------------------------------------------------------------
    //                       その他ユーティリティ
    // -------------------------------------------------------------------

    /// <summary>
    /// Playerがエネミーの前方視野角内にいるかをチェックする
    /// </summary>
    private bool IsPlayerInFrontView()
    {
        // ... (変更なし) ...
        Vector3 directionToTarget = playerTarget.position - transform.position;
        directionToTarget.y = 0;

        Vector3 forward = transform.forward;
        forward.y = 0;

        float angle = Vector3.Angle(forward, directionToTarget);

        return angle <= attackAngle / 2f;
    }


    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // ?? 新規追加: 回避チェック距離とターゲットの方向にRayを表示
        if (Application.isEditor && transform != null)
        {
            // 検出範囲の円錐表示 (攻撃視野角)
            Quaternion leftRayRotation = Quaternion.AngleAxis(-attackAngle / 2, Vector3.up);
            Quaternion rightRayRotation = Quaternion.AngleAxis(attackAngle / 2, Vector3.up);

            Vector3 leftRayDirection = leftRayRotation * transform.forward;
            Vector3 rightRayDirection = rightRayRotation * transform.forward;

            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, leftRayDirection * detectionRange);
            Gizmos.DrawRay(transform.position, rightRayDirection * detectionRange);

            // 浮遊範囲と目標地点の表示
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, driftRange);
            Gizmos.DrawSphere(currentDriftTarget, 0.5f);

            // ?? 新規追加: 回避チェックのRaycast表示
            Vector3 directionToTarget = (currentDriftTarget - transform.position).normalized;
            Gizmos.color = Color.magenta;
            Gizmos.DrawRay(transform.position, directionToTarget * avoidanceCheckDistance);

            // ?? 新規追加: 目標地点の障害物チェック範囲表示
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(currentDriftTarget, wallHitResetRange);
        }
    }
}