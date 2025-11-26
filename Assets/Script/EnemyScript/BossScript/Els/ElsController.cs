using UnityEngine;

public class ElsController : MonoBehaviour
{
    [Header("Target")]
    public Transform player;

    [Header("Boss Active")]
    public bool isActive = false;

    public enum BossState { Orbit, Retreat }
    public BossState state = BossState.Orbit;

    [Header("Orbit Movement")]
    public float orbitDistance = 6f;
    public float orbitSpeed = 2f;
    public float height = 3f;
    public float heightSmooth = 2f;
    public float changeDirInterval = 3f;

    [Header("Retreat Movement")]
    public float retreatDistance = 10f;   // プレイヤーから離れる距離
    public float retreatSpeed = 6f;       // 逃げるスピード
    public float retreatTime = 2f;        // 逃げ続ける時間
    private float retreatTimer = 0f;

    [Header("Attack")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float attackInterval = 2f;

    // 地面と壁の判定パラメータ
    [Header("Collision Avoidance")]
    public float wallCheckDistance = 1f;
    public LayerMask groundLayer;
    public LayerMask wallLayer;

    private float angle = 0f;
    private float attackTimer = 0f;
    private float dir = 1f;
    private float dirTimer = 0f;

    void Update()
    {
        if (!isActive) return;
        if (player == null) return;

        // 行動パターン
        switch (state)
        {
            case BossState.Orbit:
                OrbitMovement();
                ChangeDirectionRandom();
                TryStartRetreat();
                break;

            case BossState.Retreat:
                RetreatMovement();
                break;
        }

        HandleAttack();
    }

    // プレイヤー周回
    void OrbitMovement()
    {
        angle += orbitSpeed * dir * Time.deltaTime;
        float rad = angle * Mathf.Deg2Rad;

        Vector3 targetPos = new Vector3(
            player.position.x + Mathf.Cos(rad) * orbitDistance,
            player.position.y + height,
            player.position.z + Mathf.Sin(rad) * orbitDistance
        );

        // 壁チェック（前方に壁があれば止まる）
        if (Physics.Raycast(transform.position, (targetPos - transform.position).normalized, wallCheckDistance, wallLayer))
        {
            // 壁が近いので、ターゲット位置を補正（今の位置に近づける）
            targetPos = transform.position + (targetPos - transform.position).normalized * 0.2f;
        }

        // 地面チェック（Y座標を地面に合わせる）
        RaycastHit hit;
        if (Physics.Raycast(targetPos + Vector3.up * 5f, Vector3.down, out hit, 20f, groundLayer))
        {
            targetPos.y = hit.point.y + height;  // 地面の上に浮かせる
        }

        // 最終移動
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * heightSmooth);

        transform.LookAt(player);
    }

    // ランダムで方向転換
    void ChangeDirectionRandom()
    {
        dirTimer += Time.deltaTime;
        if (dirTimer >= changeDirInterval)
        {
            dirTimer = 0f;
            dir = Random.value > 0.5f ? 1f : -1f;
            orbitSpeed = Random.Range(1.5f, 3.5f);
        }
    }

    // 一定確率で逃げ行動に入る
    void TryStartRetreat()
    {
        if (Random.value < 0.002f)  // 0.2%/frame → 約5秒に1回くらい
        {
            state = BossState.Retreat;
            retreatTimer = 0f;
        }
    }

    // プレイヤーから距離を取る動き
    void RetreatMovement()
    {
        retreatTimer += Time.deltaTime;

        // プレイヤーの反対方向へ移動
        Vector3 dirAway = (transform.position - player.position).normalized;
        Vector3 targetPos = player.position + dirAway * retreatDistance;

        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * retreatSpeed);

        transform.LookAt(player);

        // 一定時間逃げたら周回に戻る
        if (retreatTimer >= retreatTime)
        {
            state = BossState.Orbit;
        }
    }

    // 攻撃処理
    void HandleAttack()
    {
        attackTimer += Time.deltaTime;

        if (attackTimer >= attackInterval)
        {
            attackTimer = 0f;
            Shoot();
        }
    }

    void Shoot()
    {
        Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
    }
}
