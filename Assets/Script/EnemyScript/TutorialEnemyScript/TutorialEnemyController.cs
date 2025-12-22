using UnityEngine;
using System;
using System.Collections;

public class TutorialEnemyController : MonoBehaviour
{
    public event Action onDeath;

    [Header("UI強調表示設定")]
    public GameObject highlightUI;       // インスペクターで頭上のUIを指定
    public float blinkDuration = 2.0f;   // 出現時の点滅時間

    [Header("ヘルス設定")]
    public float maxHealth = 100f;
    private float currentHealth;
    private bool isDead = false;

    [Header("攻撃設定")]
    public bool canShootBeam = false;
    public GameObject beamPrefab;
    public Transform beamOrigin;
    public float attackRate = 1f;
    public float detectionRange = 20f;
    [Range(0, 180)]
    public float attackAngle = 45f;
    public float hardStopDuration = 0.5f;

    [Header("待機設定")]
    public float startDelay = 5.0f;      // ★出現から攻撃までの猶予時間
    private float canShootAfterTime;     // ★この時刻を過ぎるまで撃てない

    [Header("ターゲット")]
    private Transform playerTarget;
    public float rotationSpeed = 5f;

    private float nextAttackTime = 0f;
    private float hardStopEndTime = 0f;

    void Start()
    {
        currentHealth = maxHealth;
        FindPlayer();

        // ★現在の時刻に待機時間を足して、攻撃開始可能時間を設定
        canShootAfterTime = Time.time + startDelay;

        // 出現時にUIを強調表示する演出を開始
        if (highlightUI != null)
        {
            StartCoroutine(HighlightOnSpawn());
        }
    }

    void Update()
    {
        if (isDead) return;

        if (playerTarget == null)
        {
            FindPlayer();
            return;
        }

        LookAtPlayer();

        if (highlightUI != null && highlightUI.activeSelf)
        {
            highlightUI.transform.LookAt(Camera.main.transform);
        }

        // ★canShootAfterTime（出現から5秒後）を過ぎているかチェックを追加
        if (canShootBeam && Time.time >= canShootAfterTime && Time.time >= hardStopEndTime)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);
            if (distanceToPlayer <= detectionRange && IsPlayerInFrontView())
            {
                if (Time.time >= nextAttackTime)
                {
                    AttackPlayer();
                    nextAttackTime = Time.time + (1f / attackRate);
                }
            }
        }
    }

    // --- 以下、既存のメソッド（FindPlayer, LookAtPlayer, AttackPlayerなどは変更なし） ---

    IEnumerator HighlightOnSpawn()
    {
        float elapsed = 0;
        highlightUI.SetActive(true);

        while (elapsed < blinkDuration)
        {
            highlightUI.SetActive(!highlightUI.activeSelf);
            yield return new WaitForSeconds(0.2f);
            elapsed += 0.2f;
        }

        highlightUI.SetActive(true);
    }

    private void FindPlayer()
    {
        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject != null) playerTarget = playerObject.transform;
    }

    private void LookAtPlayer()
    {
        Vector3 targetDirection = (playerTarget.position - transform.position).normalized;
        targetDirection.y = 0;
        if (targetDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
    }

    private void AttackPlayer()
    {
        if (beamOrigin == null || beamPrefab == null) return;

        Vector3 targetPos = playerTarget.position + Vector3.up * 1.0f;
        Vector3 direction = (targetPos - beamOrigin.position).normalized;

        RaycastHit hit;
        bool didHit = Physics.Raycast(beamOrigin.position, direction, out hit, detectionRange);
        Vector3 endPoint = didHit ? hit.point : beamOrigin.position + (direction * detectionRange);

        GameObject beamObj = Instantiate(beamPrefab, beamOrigin.position, Quaternion.LookRotation(direction));
        EnemyBeamController beamController = beamObj.GetComponent<EnemyBeamController>();
        if (beamController != null)
        {
            beamController.Fire(beamOrigin.position, endPoint, didHit, didHit ? hit.collider.gameObject : null);
        }

        hardStopEndTime = Time.time + hardStopDuration;
    }

    private bool IsPlayerInFrontView()
    {
        Vector3 directionToTarget = (playerTarget.position - transform.position).normalized;
        directionToTarget.y = 0;
        float angle = Vector3.Angle(transform.forward, directionToTarget);
        return angle <= attackAngle / 2f;
    }

    public void TakeDamage(float damageAmount)
    {
        if (isDead) return;
        currentHealth -= damageAmount;
        if (currentHealth <= 0) Die();
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;
        if (highlightUI != null) highlightUI.SetActive(false);
        onDeath?.Invoke();
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 forward = transform.forward;
        Quaternion leftRayRotation = Quaternion.AngleAxis(-attackAngle / 2, Vector3.up);
        Quaternion rightRayRotation = Quaternion.AngleAxis(attackAngle / 2, Vector3.up);
        Vector3 leftRayDirection = leftRayRotation * forward;
        Vector3 rightRayDirection = rightRayRotation * forward;
        Gizmos.DrawRay(transform.position, leftRayDirection * detectionRange);
        Gizmos.DrawRay(transform.position, rightRayDirection * detectionRange);
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}