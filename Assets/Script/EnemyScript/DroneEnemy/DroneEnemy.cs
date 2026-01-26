using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DroneEnemy : MonoBehaviour
{
    // --- HP設定 ---
    [Header("耐久力設定")]
    public float maxHealth = 200f; // 最大HP
    private float currentHealth;    // 現在のHP
    private bool isDead = false;    // 死亡フラグ

    [Header("UI設定")]
    public Slider healthSlider;        // Slider本体をアサイン
    public GameObject healthBarCanvas; // Canvasをアサイン
    public Image healthBarFillImage; // SliderのFill(中身)のImageをアサイン
    public Gradient healthGradient;  // インスペクターで色を設定

    // 爆発エフェクトのPrefab
    [Header("エフェクト設定")]
    public GameObject explosionPrefab;

    //[Header("音声設定")]
    //private AudioSource droneAudioSource; // コンポーネント保持用
    //[SerializeField] private AudioClip shotClip;              // 発射音のClip

    // --- 索敵用パラメータ ---
    [Header("ターゲット設定")]
    // private に変更し、AwakeでTag検索により設定
    private Transform playerTarget;

    public float detectionRange = 15f;     // Playerを見つける範囲
    public Transform beamOrigin;           // 弾の発射地点となるTransform

    [Range(0, 180)]
    public float attackAngle = 30f;        // 攻撃可能な視界角度（全角）

    [Header("攻撃設定")]
    public float attackRate = 5f;          // 一発ごとの間隔計算に使用 (例: 1/5 = 0.2秒間隔)
    public GameObject beamPrefab;          // 発射する弾のPrefab
    public float beamSpeed = 40f;          // 弾の速さ

    [Header("バースト攻撃設定")]
    public int bulletsPerBurst = 5;
    public float burstCooldownTime = 2f;

    [Header("硬直設定")]
    public float hardStopDuration = 0.5f;

    [Header("移動・旋回設定")]
    public float rotationSpeed = 5f;       // ドローン本体のY軸回転速度
    public float gunRotationSpeed = 20f;   // 銃の全方位回転速度
    public float hoverAltitude = 5f;
    public float driftSpeed = 1f;
    public float driftRange = 5f;
    public float altitudeCorrectionSpeed = 2f;

    // 障害物回避のための設定
    [Header("障害物回避設定")]
    public LayerMask obstacleLayer;        // 障害物となるレイヤー
    public float avoidanceCheckDistance = 3f; // 障害物チェック距離
    public float wallHitResetRange = 1f;   // 壁に接触したと見なす範囲

    // --- 内部変数 ---
    private float nextAttackTime = 0f;
    private float hardStopEndTime = 0f;
    private Vector3 currentDriftTarget;
    private bool isAttacking = false;

    // --- 内部メソッド: 色を更新する ---
    private void UpdateHealthBarColor()
    {
        if (healthBarFillImage != null && healthSlider != null)
        {
            // 現在のHPの割合(0.0 ~ 1.0)を計算
            float healthRatio = currentHealth / maxHealth;
            // グラデーションから対応する色を取得して適用
            healthBarFillImage.color = healthGradient.Evaluate(healthRatio);
        }
    }

    private void Awake()
    {
        currentHealth = maxHealth;

        // Sliderの初期設定
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = maxHealth;
        }

        // 初回の色設定
        UpdateHealthBarColor();

        // --- 既存のAwake処理 ---
        //droneAudioSource = GetComponent<AudioSource>();
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null) playerTarget = playerObject.transform;
        SetNewDriftTarget();
    }

    private void Update()
    {
        if (isDead || playerTarget == null || Time.time < hardStopEndTime) return;

        // HPバーを常にカメラに向ける（ビルボード）
        if (healthBarCanvas != null)
        {
            healthBarCanvas.transform.rotation = Camera.main.transform.rotation;
        }

        // 移動前に障害物チェックと目標地点のリセット
        CheckForObstaclesAndResetTarget();

        // 弾をPlayerに向け旋回
        RotateGunToPlayer();

        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);

        // Playerが攻撃範囲内にいるか？
        if (distanceToPlayer <= detectionRange)
        {
            // ドローン本体をPlayerに向け旋回
            LookAtPlayer();

            // 攻撃中でなければ、視界内にいればバースト攻撃を開始
            if (!isAttacking && IsPlayerInFrontView())
            {
                StartCoroutine(BurstAttackSequence());
            }
        }

        // 常時ランダムな移動
        DriftHover();
    }

    // -------------------------------------------------------------------
    //                       ドローン本体の旋回 (Y軸のみ)
    // -------------------------------------------------------------------

    /// <summary>
    /// ドローン本体の向きをPlayerの方向へ滑らかに旋回させる（Y軸回転のみ）
    /// </summary>
    private void LookAtPlayer()
    {
        Vector3 targetDirection = playerTarget.position - transform.position;
        targetDirection.y = 0; // 水平方向のみの回転

        if (targetDirection == Vector3.zero) return;

        Quaternion targetRotation = Quaternion.LookRotation(targetDirection);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            Time.deltaTime * rotationSpeed
        );
    }

    // -------------------------------------------------------------------
    //                       銃の旋回機能
    // -------------------------------------------------------------------

    /// <summary>
    /// 銃 (beamOrigin) をPlayerのTransform方向に全方位旋回させる
    /// </summary>
    private void RotateGunToPlayer()
    {
        if (beamOrigin == null || playerTarget == null) return;

        // Playerの位置から銃の位置を引いて、方向ベクトルを取得
        Vector3 targetDirection = playerTarget.position - beamOrigin.position;

        // 目標とする回転 (Playerの方向を向く)
        Quaternion targetRotation = Quaternion.LookRotation(targetDirection);

        // スムーズに旋回させる
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

        float shotDelay = 1f / attackRate; // 間隔を計算

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
            Debug.LogError("発射地点またはPrefabが設定されていません。");
            return;
        }

        //if (droneAudioSource != null && shotClip != null)
        //{
        //    droneAudioSource.PlayOneShot(shotClip);
        //}

        // 銃がすでにPlayerの方向を向いているため、beamOrigin.rotationを使用
        Quaternion bulletRotation = beamOrigin.rotation;

        GameObject bullet = Instantiate(beamPrefab, beamOrigin.position, bulletRotation);

        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // 弾を発射方向に加速
            rb.linearVelocity = bullet.transform.forward * beamSpeed;
        }
        else
        {
            Debug.LogWarning("弾PrefabにRigidbodyがありません。");
        }
    }

    // -------------------------------------------------------------------
    //                       ランダム移動処理 (回避機能付き)
    // -------------------------------------------------------------------

    /// <summary>
    /// ドローンの移動目標が障害物に近すぎないかチェックし、近ければ目標をリセット
    /// </summary>
    private void CheckForObstaclesAndResetTarget()
    {
        Vector3 directionToTarget = (currentDriftTarget - transform.position);

        // 1. Raycastで目標地点への途中に障害物があるかチェック
        if (Physics.Raycast(transform.position, directionToTarget.normalized, out RaycastHit hit, avoidanceCheckDistance, obstacleLayer))
        {
            Debug.Log("🎯 ターゲット方向 (" + hit.collider.name + ") に壁を見つけたため、ターゲットをリセットします。", gameObject);
            SetNewDriftTarget();
            return;
        }

        // 2. 目標地点自体が壁の内部や極端に近くないかチェック (OverlapSphere)
        if (Physics.CheckSphere(currentDriftTarget, wallHitResetRange, obstacleLayer))
        {
            Debug.Log("🎯 現在のターゲット地点が壁の近くに設定されているため、ターゲットをリセットします。", gameObject);
            SetNewDriftTarget();
            return;
        }

        // 3. (保険): ドローン自体のすぐ前方に壁がぶつかっていないかチェック
        if (Physics.Raycast(transform.position, transform.forward, avoidanceCheckDistance * 0.5f, obstacleLayer))
        {
            Debug.Log("🎯 ドローン本体前方に壁にぶつかっています。ターゲットをリセットします。", gameObject);
            SetNewDriftTarget();
        }
    }

    private void DriftHover()
    {
        Vector3 currentPos = transform.position;

        // 1. 高度補正 (Y軸の移動)
        float targetY = hoverAltitude;
        float newY = Mathf.Lerp(currentPos.y, targetY, Time.deltaTime * altitudeCorrectionSpeed);

        // 2. 水平方向の移動 (X/Z軸のドリフト)
        Vector3 horizontalTarget = new Vector3(currentDriftTarget.x, newY, currentDriftTarget.z);

        transform.position = Vector3.MoveTowards(
            currentPos,
            horizontalTarget,
            Time.deltaTime * driftSpeed
        );

        // 3. 目標地点に近づいたら新しい目標を設定
        if (Vector3.Distance(new Vector3(currentPos.x, 0, currentPos.z), new Vector3(currentDriftTarget.x, 0, currentDriftTarget.z)) < 0.5f)
        {
            SetNewDriftTarget();
        }
    }

    private void SetNewDriftTarget()
    {
        Vector3 newTarget;
        int attempts = 0;
        const int maxAttempts = 10;

        // 障害物がない目標地点が見つかるまで繰り返す
        do
        {
            Vector2 randomCircle = Random.insideUnitCircle * driftRange;

            newTarget = new Vector3(
                transform.position.x + randomCircle.x,
                hoverAltitude,
                transform.position.z + randomCircle.y
            );

            attempts++;

            // CheckSphereで新しいターゲット地点が壁に近すぎないか確認
        } while (Physics.CheckSphere(newTarget, wallHitResetRange, obstacleLayer) && attempts < maxAttempts);


        if (attempts >= maxAttempts)
        {
            Debug.LogWarning("ターゲット地点を見つけるのに失敗しました。現在地を保持します。", gameObject);
            currentDriftTarget = transform.position;
        }
        else
        {
            currentDriftTarget = newTarget;
            Vector3 horizontalDirection = new Vector3(currentDriftTarget.x, transform.position.y, currentDriftTarget.z) - transform.position;

            // 見つかったターゲット方向へドローンの向きを補正する
            if (horizontalDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(horizontalDirection), Time.deltaTime * rotationSpeed);
            }
        }
    }

    // -------------------------------------------------------------------
    //                       ヘルスとダメージ処理
    // -------------------------------------------------------------------

    public void TakeDamage(float damageAmount)
    {
        if (isDead) return;

        currentHealth -= damageAmount;

        if (healthSlider != null)
        {
            healthSlider.value = currentHealth;
        }

        // ダメージ時に色を更新
        UpdateHealthBarColor();

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            hardStopEndTime = Time.time + hardStopDuration;
        }
    }

    /// <summary>
    /// 死亡処理
    /// </summary>
    private void Die()
    {
        if (isDead) return;
        isDead = true;

        // 死亡時にHPバーを非表示にする
        if (healthBarCanvas != null) healthBarCanvas.SetActive(false);

        if (explosionPrefab != null)
        {
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        }

        StopAllCoroutines();
        Destroy(gameObject, 0.1f);
    }

    // -------------------------------------------------------------------
    //                       その他のユーティリティ
    // -------------------------------------------------------------------

    /// <summary>
    /// Playerがエネミーの前方視界角度内にいるかチェックする
    /// </summary>
    private bool IsPlayerInFrontView()
    {
        if (playerTarget == null) return false;

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
            // 視界の表示
            Quaternion leftRayRotation = Quaternion.AngleAxis(-attackAngle / 2, Vector3.up);
            Quaternion rightRayRotation = Quaternion.AngleAxis(attackAngle / 2, Vector3.up);

            Vector3 leftRayDirection = leftRayRotation * transform.forward;
            Vector3 rightRayDirection = rightRayRotation * transform.forward;

            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, leftRayDirection * detectionRange);
            Gizmos.DrawRay(transform.position, rightRayDirection * detectionRange);

            // 移動範囲とターゲット地点の表示
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, driftRange);
            Gizmos.DrawSphere(currentDriftTarget, 0.5f);

            // 回避チェック用のRaycast表示
            Vector3 directionToTarget = (currentDriftTarget - transform.position).normalized;
            Gizmos.color = Color.magenta;
            Gizmos.DrawRay(transform.position, directionToTarget * avoidanceCheckDistance);

            // ターゲット地点の障害物チェック範囲表示
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(currentDriftTarget, wallHitResetRange);
        }
    }

    public override bool Equals(object obj)
    {
        return obj is DroneEnemy enemy &&
               base.Equals(obj) &&
               nextAttackTime == enemy.nextAttackTime;
    }

    public override int GetHashCode()
    {
        return System.HashCode.Combine(base.GetHashCode(), nextAttackTime);
    }
}