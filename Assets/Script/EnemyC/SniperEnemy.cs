using UnityEngine;
using System.Collections;
using System;

[RequireComponent(typeof(LineRenderer))]
public class SniperEnemy : MonoBehaviour
{
    [Header("基本ステータス")]
    public float maxHealth = 100f;
    public float currentHealth;
    public GameObject deathExplosionPrefab;
    private bool isDead = false;

    public enum EnemyState { Landing, Idle, Aiming, Attack, Reload }
    public EnemyState currentState = EnemyState.Landing;

    [Header("弾薬・リロード")]
    public int maxAmmo = 5;
    private int currentAmmo;
    public float reloadTime = 4.0f;
    private bool isReloading = false;

    [Header("索敵・攻撃設定（広範囲スナイパー）")]
    public float sightRange = 80f; // さらに広範囲に設定
    public float viewAngle = 180f; // 真横までカバー
    public float rotationSpeed = 3f;
    public float shootDuration = 1.0f;
    public int bulletsPerBurst = 1;

    [Header("演出設定（レーザー点滅）")]
    public float warningDuration = 1.2f;
    public int blinkCount = 5;

    [Header("参照オブジェクト")]
    [SerializeField] private GameObject bulletPrefab;
    public Transform muzzlePoint;
    private LineRenderer laserLine;
    private Transform player;

    // --- 内部変数 ---
    private Animator animator;
    private Rigidbody rb;
    private Collider enemyCollider;
    public string groundTag = "Ground";
    public float landingSpeed = 2.0f;
    public float initialWaitTime = 1.0f;

    // Idle中の回転用
    private float nextRotationTime;
    private Quaternion targetIdleRotation;
    private bool isRotatingInIdle = false;

    void Start()
    {
        currentHealth = maxHealth;
        currentAmmo = maxAmmo;
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        enemyCollider = GetComponent<Collider>();
        laserLine = GetComponent<LineRenderer>();

        if (laserLine != null)
        {
            laserLine.enabled = false;
            laserLine.useWorldSpace = true;
            laserLine.startWidth = 0.05f;
            laserLine.endWidth = 0.01f;
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;

        TransitionToLanding();
    }

    void Update()
    {
        if (isDead || player == null || isReloading)
        {
            if (laserLine != null) laserLine.enabled = false;
            return;
        }
        if (currentState == EnemyState.Landing) return;

        bool playerFound = CheckForPlayer();

        switch (currentState)
        {
            case EnemyState.Idle:
                IdleLogic(playerFound);
                break;
            case EnemyState.Aiming:
                AimingLogic(playerFound);
                break;
        }

        if (currentState == EnemyState.Aiming)
        {
            UpdateLaserPosition();
        }
    }

    // --- 1. アイドル状態のロジック（エラーを修正） ---
    private void IdleLogic(bool playerFound)
    {
        if (playerFound)
        {
            TransitionToAiming();
            return;
        }

        if (Time.time > nextRotationTime)
        {
            nextRotationTime = Time.time + UnityEngine.Random.Range(3f, 6f);
            targetIdleRotation = Quaternion.Euler(0, UnityEngine.Random.Range(0f, 360f), 0);
            isRotatingInIdle = true;
        }

        if (isRotatingInIdle)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, targetIdleRotation, Time.deltaTime * rotationSpeed * 0.5f);
            if (Quaternion.Angle(transform.rotation, targetIdleRotation) < 1.0f) isRotatingInIdle = false;
        }
    }

    // --- 2. レーザー表示ロジック ---
    void UpdateLaserPosition()
    {
        if (laserLine == null || muzzlePoint == null || player == null) return;
        laserLine.enabled = true;
        laserLine.SetPosition(0, muzzlePoint.position);

        RaycastHit hit;
        Vector3 direction = (player.position - muzzlePoint.position).normalized;
        if (Physics.Raycast(muzzlePoint.position, direction, out hit, sightRange))
            laserLine.SetPosition(1, hit.point);
        else
            laserLine.SetPosition(1, muzzlePoint.position + direction * sightRange);
    }

    bool CheckForPlayer()
    {
        if (player == null) return false;

        // 1. 距離チェック（半径内か）
        Vector3 dir = player.position - transform.position;
        if (dir.magnitude > sightRange) return false;

        // 2. ターゲットの「中心」を計算（地面スレスレではなく、少し上を狙う）
        // プレイヤーがCharacterControllerやCapsuleColliderを持っていることを想定
        Vector3 targetPoint = player.position + Vector3.up * 1.0f;

        // 自分の発射位置（目の高さ）
        Vector3 startPoint = transform.position + Vector3.up * 1.5f;

        Vector3 rayDir = (targetPoint - startPoint).normalized;
        float rayDistance = Vector3.Distance(startPoint, targetPoint);

        // 3. 視線チェック
        RaycastHit hit;
        // デバッグ用の線をシーンビューに表示（赤い線が見えればレイは飛んでいる）
        Debug.DrawRay(startPoint, rayDir * rayDistance, Color.red);

        if (Physics.Raycast(startPoint, rayDir, out hit, sightRange))
        {
            // プレイヤーに当たった、もしくはプレイヤーの近くに当たったか
            if (hit.collider.CompareTag("Player"))
            {
                return true;
            }
        }

        // 角度制限をなくしたので、距離と遮蔽物チェックだけで判定を返す
        return false;
    }

    void AimingLogic(bool playerFound)
    {
        Vector3 dir = (player.position - transform.position).normalized;
        Quaternion lookRot = Quaternion.LookRotation(new Vector3(dir.x, 0, dir.z));
        transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRot, rotationSpeed * 60f * Time.deltaTime);

        if (Quaternion.Angle(transform.rotation, lookRot) < 5f)
            StartCoroutine(FlashAndShootSequence());
        else if (!playerFound)
            TransitionToIdle();
    }

    // --- 3. 攻撃シーケンス（点滅） ---
    IEnumerator FlashAndShootSequence()
    {
        currentState = EnemyState.Attack;

        float interval = warningDuration / (blinkCount * 2);
        for (int i = 0; i < blinkCount; i++)
        {
            if (laserLine) laserLine.enabled = false;
            yield return new WaitForSeconds(interval);
            UpdateLaserPosition();
            yield return new WaitForSeconds(interval);
        }

        if (animator != null) animator.SetTrigger("Shoot");
        ShootBullet();

        yield return new WaitForSeconds(shootDuration);

        if (!isDead && !isReloading) TransitionToAiming();
    }

    public void ShootBullet()
    {
        if (isDead || currentAmmo <= 0 || player == null) return;
        currentAmmo--;
        Vector3 targetDir = (player.position - muzzlePoint.position).normalized;
        Instantiate(bulletPrefab, muzzlePoint.position, Quaternion.LookRotation(targetDir) * Quaternion.Euler(-2f, 0, 0));
        if (currentAmmo <= 0) StartReload();
    }

    // --- 状態遷移・着地・死亡 ---
    void TransitionToIdle() { currentState = EnemyState.Idle; if (animator) animator.SetBool("IsAiming", false); }
    void TransitionToAiming() { currentState = EnemyState.Aiming; if (animator) animator.SetBool("IsAiming", true); }
    void StartReload() { isReloading = true; currentState = EnemyState.Reload; if (laserLine) laserLine.enabled = false; Invoke("FinishReload", reloadTime); }
    void FinishReload() { isReloading = false; currentAmmo = maxAmmo; TransitionToIdle(); }

    void TransitionToLanding() { currentState = EnemyState.Landing; Invoke("StartFalling", initialWaitTime); }
    void StartFalling() { if (rb) { rb.isKinematic = false; rb.useGravity = false; } }
    void FixedUpdate() { if (currentState == EnemyState.Landing && rb != null) rb.velocity = Vector3.down * landingSpeed; }

    private void OnCollisionEnter(Collision collision)
    {
        if (currentState == EnemyState.Landing && collision.gameObject.CompareTag(groundTag))
        {
            if (rb) { rb.velocity = Vector3.zero; rb.useGravity = true; }
            if (animator) animator.SetBool("IsFloating", false);
            TransitionToIdle();
        }
    }

    public void TakeDamage(float d) { currentHealth -= d; if (currentHealth <= 0) Die(); }
    void Die()
    {
        isDead = true;
        if (laserLine) laserLine.enabled = false;
        if (animator) animator.SetTrigger("Die");
        Destroy(gameObject, 2f);
    }
}