using UnityEngine;

/// <summary>
/// プレイヤーを追跡し、実弾を発射するAIを制御します。
/// </summary>
[RequireComponent(typeof(DroneSplineMover))] // SplineMoverに依存
public class DroneShooterAI : MonoBehaviour
{
    [Header("Target & Detection")]
    [Tooltip("プレイヤーのTransform")]
    public Transform target;
    [Tooltip("ドローンがプレイヤーを検知する範囲")]
    public float detectionRange = 20f;

    [Header("Weapon Settings")]
    public Projectile projectilePrefab;
    [Tooltip("実弾を発射する場所")]
    public Transform firePoint;
    public float fireRate = 1.5f; // 1.5秒に1発
    private float _nextFireTime = 0f;

    private DroneSplineMover _splineMover;

    void Awake()
    {
        _splineMover = GetComponent<DroneSplineMover>();

        // ターゲットを自動的に検索
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
        }
    }

    void Update()
    {
        if (target == null) return;

        float distanceToTarget = Vector3.Distance(transform.position, target.position);

        if (distanceToTarget < detectionRange)
        {
            // プレイヤーを検知範囲に捉えた場合
            HandleCombat();
        }
        else
        {
            // プレイヤーが範囲外の場合、スプライン移動を継続
            // SplineMoverはUpdateで常に動いているため、特別な処理は不要
        }
    }

    private void HandleCombat()
    {
        // 1. ターゲティング: プレイヤーの方向へ旋回 (水平方向のみ)
        Vector3 directionToTarget = target.position - transform.position;
        directionToTarget.y = 0; // 垂直方向の回転を無視して、安定した飛行を維持

        if (directionToTarget != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(directionToTarget);
            // SplineMoverのrotationSpeedと同じ速度で滑らかに旋回
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                lookRotation,
                _splineMover.rotationSpeed * Time.deltaTime
            );
        }

        // 2. 攻撃
        if (Time.time >= _nextFireTime)
        {
            ShootProjectile();
            _nextFireTime = Time.time + 1f / fireRate;
        }
    }

    private void ShootProjectile()
    {
        if (projectilePrefab == null || firePoint == null)
        {
            Debug.LogError("実弾のプレハブまたは発射点が設定されていません。");
            return;
        }

        // 弾を生成し、発射点の位置と回転に設定
        Projectile newProjectile = Instantiate(
            projectilePrefab,
            firePoint.position,
            firePoint.rotation
        );

        // 弾の向かう方向を調整したい場合は、ここで`newProjectile.transform.LookAt(target)`などを行う
    }
}