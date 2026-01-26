using UnityEngine;
using System.Collections;

public class LongArmBossAI : MonoBehaviour
{
    public Transform player;
    public VoxController controller;
    public float followSpeed = 5f;
    public float rotationSpeed = 10f;

    [Header("待機設定")]
    public Vector3 followOffset = new Vector3(0, 15, 0);

    [Header("叩きつけ(Slam)設定")]
    public Transform damageSource;
    public float slamRange = 15f;
    public float slamSpeed = 150f;
    public float anticipationTime = 0.1f;
    public float slamDamage = 200.0f;

    [Header("共通設定")]
    public float recoveryTime = 0.5f;
    public float attackCooldown = 1.5f;
    public float damageDistance = 4.0f;

    private bool isAttacking = false;
    private bool isCooldown = false;

    void Update()
    {
        if (controller == null || !controller.isActivated || player == null || isAttacking) return;

        MoveTowardsPlayer();
        LookAtPlayer();

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist <= slamRange && !isCooldown)
        {
            StartCoroutine(SlamRoutine());
        }
    }

    IEnumerator SlamRoutine()
    {
        isAttacking = true;
        isCooldown = true;

        // 1. 予備動作
        Vector3 windUpPos = transform.position + Vector3.up * 5.0f;
        float t = 0;
        while (t < 1.0f)
        {
            if (!controller.isActivated) yield break;
            t += Time.deltaTime / anticipationTime;
            transform.position = Vector3.MoveTowards(transform.position, windUpPos, slamSpeed * 0.2f);
            yield return null;
        }

        // 2. 叩きつけ
        Vector3 targetSlamPos = player.position;
        while (Vector3.Distance(transform.position, targetSlamPos) > 0.1f)
        {
            if (!controller.isActivated) yield break;
            transform.position = Vector3.MoveTowards(transform.position, targetSlamPos, slamSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = targetSlamPos;

        // 3. ヒット判定
        CheckSlamHit();

        yield return new WaitForSeconds(recoveryTime);
        FinishAttack();
    }

    private void CheckSlamHit()
    {
        // 判定の基準点を決定（damageSourceが未設定なら自分自身の位置を使う）
        Vector3 sourcePos = (damageSource != null) ? damageSource.position : transform.position;

        // sourcePos（腕の先など）とプレイヤーの距離で判定
        if (Vector3.Distance(sourcePos, player.position) < damageDistance)
        {
            ApplyDamageToPlayer(player.gameObject, slamDamage);
            Debug.Log("<color=red>ボスの腕の先端がプレイヤーにヒット！</color>");
        }
    }

    /// <summary>
    /// 全てのコントローラー（Blance/Buster等）を考慮したダメージ適用
    /// </summary>
    private void ApplyDamageToPlayer(GameObject target, float damageAmount)
    {
        PlayerStatus status = target.GetComponentInParent<PlayerStatus>() ?? target.GetComponentInChildren<PlayerStatus>();
        if (status == null) return;

        float defenseMultiplier = 1.0f;

        // 現在アクティブなコントローラーを探して倍率を取得
        var balance = target.GetComponentInParent<BlanceController>();
        var buster = target.GetComponentInParent<BusterController>();
        var speed = target.GetComponentInParent<SpeedController>(); // ←追加

        if (balance != null && balance.enabled)
        {
            defenseMultiplier = balance._modesAndVisuals.CurrentArmorStats?.defenseMultiplier ?? 1.0f;
        }
        else if (buster != null && buster.enabled)
        {
            defenseMultiplier = buster._modesAndVisuals.CurrentArmorStats?.defenseMultiplier ?? 1.0f;
        }
        else if (speed != null && speed.enabled) // ←追加
        {
            defenseMultiplier = speed._modesAndVisuals.CurrentArmorStats?.defenseMultiplier ?? 1.0f;
        }

        status.TakeDamage(damageAmount, defenseMultiplier);
    }

    void FinishAttack() { isAttacking = false; if (gameObject.activeInHierarchy) StartCoroutine(CooldownRoutine()); }
    IEnumerator CooldownRoutine() { yield return new WaitForSeconds(attackCooldown); isCooldown = false; }
    void MoveTowardsPlayer() { transform.position = Vector3.Lerp(transform.position, player.position + followOffset, Time.deltaTime * followSpeed); }
    void LookAtPlayer() { if (isAttacking) return; Vector3 direction = (player.position - transform.position).normalized; if (direction != Vector3.zero) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * rotationSpeed); }
    // ギズモも基準点に合わせて表示するように修正
    private void OnDrawGizmos()
    {
        Vector3 sourcePos = (damageSource != null) ? damageSource.position : transform.position;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(sourcePos, damageDistance);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, slamRange);
    }
}