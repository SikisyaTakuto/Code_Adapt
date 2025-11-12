using UnityEngine;
using System.Collections; // コルーチンを使用するために必要

public class DroneEnemy : MonoBehaviour
{
    // --- ?? HP設定 ---
    [Header("ヘルス設定")]
    public float maxHealth = 100f; // 最大HP
    private float currentHealth;   // 現在のHP
    private bool isDead = false;   // 死亡フラグ

    // --- 公開パラメータ ---
    [Header("ターゲット設定")]
    public Transform playerTarget;               // PlayerのTransformをここに設定
    public float detectionRange = 15f;           // Playerを検出する範囲
    public Transform beamOrigin;                 // 弾の発射元となるTransform

    [Range(0, 180)]
    public float attackAngle = 30f;              // 攻撃可能な正面視野角（全角）

    [Header("攻撃設定")]
    public float attackRate = 5f;                // 弾と弾の間の間隔計算に使用 (例: 1/5 = 0.2秒間隔)
    public GameObject beamPrefab;                // 発射する弾のPrefab
    public float beamSpeed = 40f;                // 弾の速度

    [Header("バースト攻撃設定")] // ?? バースト制御
    public int bulletsPerBurst = 5;         // 1回のバーストで発射する弾数
    public float burstCooldownTime = 2f;    // バースト終了後のクールタイム（秒）

    [Header("硬直設定")]
    public float hardStopDuration = 0.5f;        // 攻撃後の硬直時間を短縮 (現在は未使用)

    [Header("浮遊移動設定")]
    public float rotationSpeed = 5f;             // Player追跡時の回転速度
    public float hoverAltitude = 5f;             // ドローンが常に飛ぶ高さ（Y座標）
    public float driftSpeed = 1f;                // 横方向への浮遊速度
    public float driftRange = 5f;                // 浮遊移動する範囲の半径
    public float altitudeCorrectionSpeed = 2f;   // 高さを一定に保つための補正速度

    // --- 内部変数 ---
    private float nextAttackTime = 0f;           // 現在は未使用
    private float hardStopEndTime = 0f;          // 硬直が解除される時間
    private Animator animator;                   // Animatorコンポーネントへの参照
    private Vector3 currentDriftTarget;          // 浮遊移動の目標地点

    private bool isAttacking = false;           // バースト攻撃中かどうかのフラグ

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

        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);

        // 2. Playerが攻撃範囲内にいるか？
        if (distanceToPlayer <= detectionRange)
        {
            // --- プレイヤー発見時の挙動（攻撃優先） ---
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
    //                         攻撃処理 (バーストシステム)
    // -------------------------------------------------------------------

    /// <summary>
    /// 5連射とクールタイムを制御するコルーチン
    /// </summary>
    private IEnumerator BurstAttackSequence()
    {
        isAttacking = true; // 攻撃開始フラグを立てる

        float shotDelay = 0.5f / attackRate; // 弾と弾の間の間隔 (例: 1/5 = 0.2秒)

        // 1. バースト攻撃
        for (int i = 0; i < bulletsPerBurst; i++)
        {
            AttackSingleBullet(); // 弾を1発発射

            // 弾と弾の間のウェイト (連続発射の間隔)
            yield return new WaitForSeconds(shotDelay);
        }

        // 2. バースト後のクールタイム
        yield return new WaitForSeconds(burstCooldownTime);

        isAttacking = false; // 攻撃シーケンス終了
    }


    /// <summary>
    /// 弾を1発発射する処理（単発）
    /// </summary>
    private void AttackSingleBullet()
    {
        if (beamOrigin == null || beamPrefab == null)
        {
            Debug.LogError("発射元またはPrefabが設定されていません。");
            return;
        }

        Vector3 directionToPlayer = playerTarget.position - beamOrigin.position;
        Quaternion bulletRotation = Quaternion.LookRotation(directionToPlayer);

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
    //                         空中移動処理
    // -------------------------------------------------------------------

    /// <summary>
    /// ドローンを一定高度に維持しつつ、ランダムな目標に向けて浮遊移動させる
    /// </summary>
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

    /// <summary>
    /// 新しいランダムな浮遊目標地点を設定する
    /// </summary>
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
    //                       ヘルスとダメージ処理
    // -------------------------------------------------------------------

    /// <summary>
    /// 外部からダメージを受け取るための公開メソッド
    /// </summary>
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
        isDead = true;
        Debug.Log(gameObject.name + "は破壊されました！");

        if (animator != null)
        {
            animator.SetBool("Dead", true);
        }

        Destroy(gameObject, 3f);
    }

    // -------------------------------------------------------------------
    //                         その他ユーティリティ
    // -------------------------------------------------------------------

    /// <summary>
    /// ドローン本体の向きをPlayerの方向へ向ける（スムーズな回転）
    /// </summary>
    private void LookAtPlayer()
    {
        Vector3 targetDirection = playerTarget.position - transform.position;
        targetDirection.y = 0; // 空中敵なので、水平回転のみ

        Quaternion targetRotation = Quaternion.LookRotation(targetDirection);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            Time.deltaTime * rotationSpeed
        );
    }

    /// <summary>
    /// Playerがエネミーの前方視野角内にいるかをチェックする
    /// </summary>
    private bool IsPlayerInFrontView()
    {
        Vector3 directionToTarget = playerTarget.position - transform.position;
        directionToTarget.y = 0;

        Vector3 forward = transform.forward;
        forward.y = 0;

        float angle = Vector3.Angle(forward, directionToTarget);

        return angle <= attackAngle / 2f;
    }

    // -------------------------------------------------------------------
    //                            デバッグ補助
    // -------------------------------------------------------------------

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        if (Application.isEditor && transform != null)
        {
            // 攻撃可能な角度の視錐台を描画
            Quaternion leftRayRotation = Quaternion.AngleAxis(-attackAngle / 2, Vector3.up);
            Quaternion rightRayRotation = Quaternion.AngleAxis(attackAngle / 2, Vector3.up);

            Vector3 leftRayDirection = leftRayRotation * transform.forward;
            Vector3 rightRayDirection = rightRayRotation * transform.forward;

            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, leftRayDirection * detectionRange);
            Gizmos.DrawRay(transform.position, rightRayDirection * detectionRange);

            // 浮遊移動の目標点を描画
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, driftRange);
            Gizmos.DrawSphere(currentDriftTarget, 0.5f);
        }
    }
}