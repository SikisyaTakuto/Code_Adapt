using UnityEngine;
using System.Collections; // コルーチンを使用するために必要

public class DroneEnemy : MonoBehaviour
{
    // --- HP設定 ---
    [Header("ヘルス設定")]
    public float maxHealth = 100f; // 最大HP
    private float currentHealth;   // 現在のHP
    private bool isDead = false;   // 死亡フラグ

    // ?? 新規追加: 爆発エフェクトのPrefab
    [Header("エフェクト設定")]
    public GameObject explosionPrefab;

    // --- 公開パラメータ ---
    [Header("ターゲット設定")]
    public Transform playerTarget;             // PlayerのTransformをここに設定
    public float detectionRange = 15f;         // Playerを検出する範囲
    public Transform beamOrigin;               // 弾の発射元となるTransform

    [Range(0, 180)]
    public float attackAngle = 30f;            // 攻撃可能な正面視野角（全角）

    [Header("攻撃設定")]
    public float attackRate = 5f;              // 弾と弾の間の間隔計算に使用 (例: 1/5 = 0.2秒間隔)
    public GameObject beamPrefab;              // 発射する弾のPrefab
    public float beamSpeed = 40f;              // 弾の速度

    [Header("バースト攻撃設定")]
    public int bulletsPerBurst = 5;
    public float burstCooldownTime = 2f;

    [Header("硬直設定")]
    public float hardStopDuration = 0.5f;

    [Header("浮遊移動設定")]
    public float rotationSpeed = 5f;             // Player追跡時の回転速度（ドローン本体用）
    // ?? 銃口の回転速度を本体と分ける (必要であれば)
    public float gunRotationSpeed = 20f;
    public float hoverAltitude = 5f;
    public float driftSpeed = 1f;
    public float driftRange = 5f;
    public float altitudeCorrectionSpeed = 2f;

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
    //               ドローン本体の回転 (Y軸のみ)
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
    //              ?? 銃口の回転処理 (新規追加)
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
    //                 攻撃処理 (バーストシステム)
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
    //                        空中移動処理
    // -------------------------------------------------------------------

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
        Vector2 randomCircle = Random.insideUnitCircle * driftRange;

        currentDriftTarget = new Vector3(
            transform.position.x + randomCircle.x,
            hoverAltitude,
            transform.position.z + randomCircle.y
        );
    }

    // -------------------------------------------------------------------
    //                 ヘルスとダメージ処理
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

        if (Application.isEditor && transform != null)
        {
            Quaternion leftRayRotation = Quaternion.AngleAxis(-attackAngle / 2, Vector3.up);
            Quaternion rightRayRotation = Quaternion.AngleAxis(attackAngle / 2, Vector3.up);

            Vector3 leftRayDirection = leftRayRotation * transform.forward;
            Vector3 rightRayDirection = rightRayRotation * transform.forward;

            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, leftRayDirection * detectionRange);
            Gizmos.DrawRay(transform.position, rightRayDirection * detectionRange);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, driftRange);
            Gizmos.DrawSphere(currentDriftTarget, 0.5f);
        }
    }
}