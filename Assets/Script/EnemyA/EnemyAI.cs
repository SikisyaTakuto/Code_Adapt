using UnityEngine;
using System.Collections; // コルーチンのためにSystem.Collectionsを追加

public class EnemyAI : MonoBehaviour
{
    private Animator anim;

    [Header("ターゲット設定")]
    [SerializeField] private Transform target;
    [SerializeField] private float attackRange = 10f;
    [SerializeField] private float rotationSpeed = 5f;

    [Header("移動・後退設定")]
    [SerializeField] private float moveSpeed = 3f;      // 通常速度
    [SerializeField] private float dashSpeed = 6f;      // ダッシュ速度
    [SerializeField] private float retreatRange = 5f;
    [SerializeField] private float dashStartRange = 20f; // この距離を超えるとダッシュ開始


    [Header("射撃設定")]
    public float fireRate = 2.0f;
    private float nextFireTime;

    [Tooltip("射撃アニメーション開始から弾が出るまでの時間(秒)")]
    [SerializeField] private float shootingDelay = 0.2f; 


    [Header("弾設定")]
    public GameObject bulletPrefab; // Inspectorで弾のプレハブを設定
    public Transform firePoint;      // Inspectorで銃口の位置を設定

    // Animatorのパラメーター名
    private const string IsAimingParam = "IsAiming";
    private const string FireTriggerParam = "FireTrigger";

    // Boolパラメーター名
    private const string MoveForwardParam = "MoveForward";
    private const string MoveBackwardParam = "MoveBackward";
    private const string MoveLeftParam = "MoveLeft";
    private const string MoveRightParam = "MoveRight";
    private const string IsDashingParam = "IsDashing"; // ダッシュ判定用

    void Start()
    {
        anim = GetComponent<Animator>();
        nextFireTime = Time.time;

        if (target == null && GameObject.FindWithTag("Player") != null)
        {
            target = GameObject.FindWithTag("Player").transform;
        }
    }

    void Update()
    {
        if (target == null) return;

        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        float currentSpeedZ = 0f;
        float currentSpeedX = 0f;

        // 1. 攻撃範囲内の行動
        if (distanceToTarget <= attackRange)
        {
            LookAtTarget();
            anim.SetBool(IsAimingParam, true);

            if (distanceToTarget <= retreatRange)
            {
                // プレイヤーが近すぎる場合: 後退
                currentSpeedZ = -moveSpeed;
                transform.Translate(Vector3.forward * currentSpeedZ * Time.deltaTime);
            }
            else
            {
                // 適切な距離（定点射撃範囲）の場合: 左右に移動 (ストレイフ)
                currentSpeedZ = 0f;

                // 左右ストレイフのロジック
                Vector3 directionToTarget = target.position - transform.position;
                float dotProductX = Vector3.Dot(directionToTarget.normalized, transform.right);

                if (dotProductX > 0.1f) // プレイヤーが右側にいる
                {
                    currentSpeedX = moveSpeed;
                }
                else if (dotProductX < -0.1f) // プレイヤーが左側にいる
                {
                    currentSpeedX = -moveSpeed;
                }
                else // ほぼ正面にいる
                {
                    currentSpeedX = 0f;
                }

                // 実際に横に動かす
                transform.Translate(Vector3.right * currentSpeedX * Time.deltaTime);
            }

            // 2. 射撃タイミングのチェック
            if (Time.time >= nextFireTime)
            {
                // 射撃トリガーをセットし、コルーチンを開始する
                anim.SetTrigger(FireTriggerParam);
                StartCoroutine(ShootWithDelay());

                // 次の発射時間を設定
                nextFireTime = Time.time + fireRate;
            }
        }
        // 3. 攻撃範囲外の行動
        else
        {
            // ★ 修正点: 攻撃範囲外に出たら、射撃コルーチンを停止する
            StopAllCoroutines();

            // ★ 追跡開始/停止の判定
            if (distanceToTarget > dashStartRange)
            {
                // プレイヤーが非常に遠い場合: 待機
                currentSpeedZ = 0f;
                currentSpeedX = 0f;
                anim.SetBool(IsAimingParam, false); // 構えを解除 (安全のため)

                // 待機中は何もせず、移動アニメーションを全てOFFにする
                // (アニメーションのBoolリセットロジックが担当するため、ここでは速度を0にするだけでOK)
            }
            else
            {
                // プレイヤーが追跡範囲内の場合 (attackRange < distance <= dashStartRange)

                // 追跡（前進）
                currentSpeedX = 0f;
                anim.SetBool(IsAimingParam, false); // 構えを解除

                // 速度の切り替え（通常速度またはダッシュ）
                if (distanceToTarget > (dashStartRange / 2f)) // 例: 10fより遠い場合をダッシュに
                {
                    currentSpeedZ = dashSpeed;
                }
                else
                {
                    currentSpeedZ = moveSpeed;
                }

                // 実際に前に動かす
                transform.Translate(Vector3.forward * currentSpeedZ * Time.deltaTime);

                // 追跡中はターゲットを向き続けます
                LookAtTarget();
            }
        }

        // --- アニメーションのBoolパラメーター設定（ブレンドツリーなし） ---

        // 全ての移動Boolを一旦リセット
        anim.SetBool(MoveForwardParam, false);
        anim.SetBool(MoveBackwardParam, false);
        anim.SetBool(MoveLeftParam, false);
        anim.SetBool(MoveRightParam, false);
        anim.SetBool(IsDashingParam, false); 

        // 現在の移動方向と速度に基づいて、対応するBoolをtrueにする
        if (currentSpeedZ > 0f) // 前進またはダッシュ
        {
            if (currentSpeedZ > moveSpeed + 0.1f) 
            {
                anim.SetBool(IsDashingParam, true); // ダッシュアニメーションを有効化
            }

            anim.SetBool(MoveForwardParam, true);
        }
        else if (currentSpeedZ < 0f) // 後退
        {
            anim.SetBool(MoveBackwardParam, true);
        }

        // Z軸の移動がない場合のみ、X軸の左右移動をチェックする
        else if (currentSpeedZ == 0f)
        {
            if (currentSpeedX > 0.1f) // 右ストレイフ
            {
                anim.SetBool(MoveRightParam, true);
            }
            else if (currentSpeedX < -0.1f) // 左ストレイフ
            {
                anim.SetBool(MoveLeftParam, true);
            }
        }
    }

    /// <summary>
    /// 射撃アニメーション開始からディレイ後に弾を生成
    /// </summary>
    private IEnumerator ShootWithDelay()
    {
        // shootingDelay秒待機する
        yield return new WaitForSeconds(shootingDelay); 

        // ディレイ後、弾を生成する関数を呼び出す
        ShootBullet();
    }

    /// <summary>
    /// 弾を生成し、発射する
    /// </summary>
    public void ShootBullet() // ★ これが正しい定義（重複を解消）
    {
        if (bulletPrefab != null && firePoint != null)
        {
            // 弾を生成 (銃口の位置と回転(方向)を使用)
            GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);

            // ★ 確認: 弾に速度を与える処理がある場合、以下の処理を追加/修正します
            // ----------------------------------------------------

            // 例: 弾丸に速度を与える（Rigidbodyの場合）
            Rigidbody rb = bullet.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // 弾のローカル前方（Z軸）に速度を与える
                float bulletSpeed = 100f; // 適切な速度を設定
                rb.velocity = bullet.transform.forward * bulletSpeed;
            }
            // ----------------------------------------------------
        }
    }

    /// <summary>
    /// ターゲットの方をゆっくりと向く
    /// </summary>
    void LookAtTarget()
    {
        Vector3 direction = target.position - transform.position;
        direction.y = 0;
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);
    }
}