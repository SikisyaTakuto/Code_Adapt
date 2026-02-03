using UnityEngine;
using System.Collections;

public class LongArmBossAI : MonoBehaviour
{
    public Transform player;
    public VoxController controller;
    public float followSpeed = 5f;
    public float rotationSpeed = 10f;

    [Header("待機・範囲設定")]
    public float detectionRange = 40f; // この範囲内にプレイヤーがいたら起動
    public Vector3 followOffset = new Vector3(0, 15, 0);
    private Vector3 _idlePosition;     // 最初に配置した位置を待機場所にする

    [Header("叩きつけ(Slam)設定")]
    public Transform damageSource;
    public float slamRange = 10f;      // 叩きつけを開始する距離
    public float slamSpeed = 50f;
    public float anticipationTime = 0.5f;
    public float slamDamage = 200.0f;

    [Header("共通設定")]
    public float recoveryTime = 1.0f;
    public float attackCooldown = 4.0f;
    public float damageDistance = 4.0f;

    private bool isAttacking = false;
    private bool isCooldown = false;

    [Header("向きの補正")]
    public Vector3 rotationOffset; // Inspectorから (0, 180, 0) などに設定

    void Awake()
    {
        // 起動時の位置を「待機ポジション」として保持
        _idlePosition = transform.position;
    }

    void Update()
    {
        // ここが重要：isAttacking（叩きつけ中）は Update 内の移動・回転処理を完全にスキップする
        if (controller == null || !controller.isActivated || player == null || isAttacking) return;

        float distFromIdlePos = Vector3.Distance(_idlePosition, player.position);
        float distFromPlayer = Vector3.Distance(transform.position, player.position);

        if (distFromIdlePos <= detectionRange)
        {
            if (distFromPlayer <= slamRange && !isCooldown)
            {
                StartCoroutine(SlamRoutine());
            }
            else
            {
                MoveTowardsPlayer();
            }
        }
        else
        {
            ReturnToIdle();
        }
    }

    // プレイヤーに向かって移動
    void MoveTowardsPlayer()
    {
        transform.position = Vector3.Lerp(transform.position, player.position + followOffset, Time.deltaTime * followSpeed);
    }

    // 待機場所へ戻る
    void ReturnToIdle()
    {
        transform.position = Vector3.Lerp(transform.position, _idlePosition, Time.deltaTime * followSpeed);
        // 待機場所に戻る時は、少し前方を向くか、ゆっくり回転させる
        LookAtPlayer(_idlePosition + Vector3.forward);
    }

    // 指定された方向を向く
    void LookAtPlayer(Vector3 targetPos)
    {
        Vector3 direction = (targetPos - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            // 上下方向への傾きを防ぐ（水平方向のみ向く）
            direction.y = 0;

            // 1. ターゲットを向く回転
            Quaternion lookRotation = Quaternion.LookRotation(direction);

            // 2. モデル固有の向き補正（rotationOffset）を適用
            // この順番（Rotation * Offset）にすることで、
            // 「プレイヤーの方向を向いた後、モデルをその場で回転させる」動きになります
            Quaternion finalRotation = lookRotation * Quaternion.Euler(rotationOffset);

            transform.rotation = Quaternion.Slerp(transform.rotation, finalRotation, Time.deltaTime * rotationSpeed);
        }
    }

    IEnumerator SlamRoutine()
    {
        isAttacking = true;
        isCooldown = true;

        // 1. 攻撃開始時の向きを計算して保持
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        directionToPlayer.y = 0;

        // ★攻撃開始直前の回転を「正しい向き」として確定させる
        Quaternion lookRotation = Quaternion.LookRotation(directionToPlayer);
        Quaternion fixedRotation = lookRotation * Quaternion.Euler(rotationOffset);
        transform.rotation = fixedRotation;

        // 2. 予備動作（上昇）
        Vector3 startPos = transform.position;
        Vector3 windUpPos = startPos + Vector3.up * 3.0f;
        float t = 0;
        while (t < 1.0f)
        {
            if (!controller.isActivated) yield break;
            t += Time.deltaTime / anticipationTime;
            transform.position = Vector3.Lerp(startPos, windUpPos, t);
            transform.rotation = fixedRotation; // 固定
            yield return null;
        }

        // 3. 叩きつけ（ターゲットへ突進）
        Vector3 targetSlamPos = player.position;
        while (Vector3.Distance(transform.position, targetSlamPos) > 0.1f)
        {
            if (!controller.isActivated) yield break;
            transform.position = Vector3.MoveTowards(transform.position, targetSlamPos, slamSpeed * Time.deltaTime);
            transform.rotation = fixedRotation; // 固定
            yield return null;
        }
        transform.position = targetSlamPos;

        CheckSlamHit();

        // --- 4. 元の位置・回転に戻る動作 ---
        yield return new WaitForSeconds(recoveryTime * 0.5f); // 叩きつけた後の硬直

        float returnTime = 0.5f; // 戻るのにかかる時間
        float r = 0;
        Vector3 slamEndPos = transform.position;

        while (r < 1.0f)
        {
            if (!controller.isActivated) yield break;
            r += Time.deltaTime / returnTime;

            // 位置を戻す
            transform.position = Vector3.Lerp(slamEndPos, player.position + followOffset, r);

            // ★回転を戻す
            // 攻撃用の固定回転(fixedRotation)から、プレイヤーを向く本来の回転へ徐々に戻す
            Vector3 currentDir = (player.position - transform.position).normalized;
            currentDir.y = 0;
            if (currentDir != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(currentDir) * Quaternion.Euler(rotationOffset);
                transform.rotation = Quaternion.Slerp(fixedRotation, targetRot, r);
            }

            yield return null;
        }

        isAttacking = false; // ここで Update() の移動・回転処理が再開される

        yield return new WaitForSeconds(attackCooldown);
        isCooldown = false;
    }
   

    private void CheckSlamHit()
    {
        Vector3 sourcePos = (damageSource != null) ? damageSource.position : transform.position;
        if (Vector3.Distance(sourcePos, player.position) < damageDistance)
        {
            ApplyDamageToPlayer(player.gameObject, slamDamage);
        }
    }

    private void ApplyDamageToPlayer(GameObject target, float damageAmount)
    {
        PlayerStatus status = target.GetComponentInParent<PlayerStatus>() ?? target.GetComponentInChildren<PlayerStatus>();
        if (status == null) return;

        float defenseMultiplier = 1.0f;
        var balance = target.GetComponentInParent<BlanceController>();
        var buster = target.GetComponentInParent<BusterController>();
        var speed = target.GetComponentInParent<SpeedController>();

        if (balance != null && balance.enabled) defenseMultiplier = balance._modesAndVisuals.CurrentArmorStats?.defenseMultiplier ?? 1.0f;
        else if (buster != null && buster.enabled) defenseMultiplier = buster._modesAndVisuals.CurrentArmorStats?.defenseMultiplier ?? 1.0f;
        else if (speed != null && speed.enabled) defenseMultiplier = speed._modesAndVisuals.CurrentArmorStats?.defenseMultiplier ?? 1.0f;

        status.TakeDamage(damageAmount, defenseMultiplier);
    }

    private void OnDrawGizmos()
    {
        // 索敵範囲（青）
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(Application.isPlaying ? _idlePosition : transform.position, detectionRange);

        // 叩きつけ開始範囲（黄）
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, slamRange);

        // ヒット判定範囲（赤）
        Vector3 sourcePos = (damageSource != null) ? damageSource.position : transform.position;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(sourcePos, damageDistance);
    }
}