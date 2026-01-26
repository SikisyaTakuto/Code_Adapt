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

    void Awake()
    {
        // 起動時の位置を「待機ポジション」として保持
        _idlePosition = transform.position;
    }

    void Update()
    {
        if (controller == null || !controller.isActivated || player == null || isAttacking) return;

        // 待機場所（中心点）からプレイヤーへの距離を測る
        float distFromIdlePos = Vector3.Distance(_idlePosition, player.position);
        // 現在のアーム位置からプレイヤーへの距離を測る
        float distFromPlayer = Vector3.Distance(transform.position, player.position);

        if (distFromIdlePos <= detectionRange)
        {
            // --- プレイヤーが範囲内にいる場合 ---

            if (distFromPlayer <= slamRange && !isCooldown)
            {
                // 射程内なら叩きつけ
                StartCoroutine(SlamRoutine());
            }
            else
            {
                // 射程外なら追跡
                MoveTowardsPlayer();
                LookAtPlayer(player.position);
            }
        }
        else
        {
            // --- プレイヤーが範囲外にいる場合：定位置へ戻る ---
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
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * rotationSpeed);
    }

    IEnumerator SlamRoutine()
    {
        isAttacking = true;
        isCooldown = true;

        // 1. 予備動作
        Vector3 windUpPos = transform.position + Vector3.up * 3.0f;
        float t = 0;
        while (t < 1.0f)
        {
            if (!controller.isActivated) yield break;
            t += Time.deltaTime / anticipationTime;
            transform.position = Vector3.Lerp(transform.position, windUpPos, t);
            yield return null;
        }

        // 2. 叩きつけ
        Vector3 targetSlamPos = player.position;
        while (Vector3.Distance(transform.position, targetSlamPos) > 0.5f)
        {
            if (!controller.isActivated) yield break;
            transform.position = Vector3.MoveTowards(transform.position, targetSlamPos, slamSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = targetSlamPos;

        CheckSlamHit();

        yield return new WaitForSeconds(recoveryTime);
        isAttacking = false;

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