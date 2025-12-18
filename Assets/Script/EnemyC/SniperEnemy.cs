using UnityEngine;
using System.Collections;

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
    public float reloadTime = 3.0f;
    private bool isReloading = false;

    [Header("着地設定")]
    public float initialWaitTime = 1.0f;
    public float landingSpeed = 2.0f;
    public string groundTag = "Ground";

    [Header("射撃タイミング設定")]
    public float chargeTime = 2.5f;
    public float postShotPause = 1.5f;

    [Header("索敵設定")]
    public float sightRange = 80f;
    public float rotationSpeed = 3f;
    public float tooCloseDistance = 5f;

    [Header("参照オブジェクト")]
    [SerializeField] private GameObject bulletPrefab;
    public Transform muzzlePoint;
    private LineRenderer laserLine;
    private Transform player;

    [Header("ターゲット調整")]
    public float targetHeightOffset = 1.2f; // 💡 プレイヤーの足元からどれくらい上（胸・頭）を狙うか

    private Animator animator;
    private Rigidbody rb;

    // 💡 バグ防止用の追加フラグ
    private bool isShooting = false;

    void Start()
    {
        currentHealth = maxHealth;
        currentAmmo = maxAmmo;
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        laserLine = GetComponent<LineRenderer>();

        if (laserLine) { laserLine.enabled = false; laserLine.useWorldSpace = true; }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;

        TransitionToLanding();
    }

    void Update()
    {
        if (isDead || player == null || currentState == EnemyState.Landing) return;

        if (isReloading)
        {
            LookAtPlayer();
            return;
        }

        bool playerFound = CheckForPlayer();

        // --- 状態遷移の整理 ---
        if (playerFound)
        {
            LookAtPlayer();

            // 射撃中でなければエイム状態へ
            if (!isShooting && currentState == EnemyState.Idle)
            {
                TransitionToAiming();
            }

            // エイム中のみ射撃ロジックを実行
            if (currentState == EnemyState.Aiming)
            {
                AimingLogic();
            }
        }
        else
        {
            // プレイヤーを見失い、かつ射撃中でなければ待機へ
            if (!isShooting && currentState != EnemyState.Idle)
            {
                TransitionToIdle();
            }
        }

        UpdateLaserPosition(playerFound);
    }

    void LookAtPlayer()
    {
        Vector3 dir = (player.position - transform.position).normalized;
        if (dir == Vector3.zero) return;
        Quaternion lookRot = Quaternion.LookRotation(new Vector3(dir.x, 0, dir.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, rotationSpeed * Time.deltaTime);
    }

    bool CheckForPlayer()
    {
        float distance = Vector3.Distance(transform.position, player.position);
        if (distance < tooCloseDistance || distance > sightRange) return false;

        RaycastHit hit;
        Vector3 rayStart = transform.position + Vector3.up * 1.5f;
        Vector3 rayDir = ((player.position + Vector3.up) - rayStart).normalized;
        if (Physics.Raycast(rayStart, rayDir, out hit, sightRange))
            if (hit.collider.CompareTag("Player")) return true;
        return false;
    }

    void AimingLogic()
    {
        // 既に射撃中なら何もしない（二重起動防止）
        if (isShooting) return;

        Vector3 dir = (player.position - transform.position).normalized;
        float angle = Quaternion.Angle(transform.rotation, Quaternion.LookRotation(new Vector3(dir.x, 0, dir.z)));

        // 正面を向いたら射撃開始
        if (angle < 5f) StartCoroutine(FlashAndShootSequence());
    }

    private IEnumerator FlashAndShootSequence()
    {
        if (isShooting || isReloading) yield break;

        isShooting = true;
        currentState = EnemyState.Attack;

        float timer = 0;
        while (timer < chargeTime)
        {
            timer += Time.deltaTime;
            float blinkSpeed = (timer / chargeTime) > 0.7f ? 20f : 10f;
            if (laserLine) laserLine.enabled = (Mathf.FloorToInt(timer * blinkSpeed) % 2 == 0);
            yield return null;
        }

        if (laserLine) laserLine.enabled = false;
        if (animator) animator.SetTrigger("Shoot");

        ShootBullet();

        // 撃った後の硬直（クールタイム）
        yield return new WaitForSeconds(postShotPause);

        isShooting = false;

        // 💡 撃ち終わった後、まだプレイヤーが射程内にいるならAimingを維持する
        if (CheckForPlayer())
        {
            TransitionToAiming();
        }
        else
        {
            TransitionToIdle();
        }
    }

    public void ShootBullet()
    {
        if (isDead || currentAmmo <= 0) return;
        currentAmmo--;

        Vector3 spawnPos = muzzlePoint ? muzzlePoint.position : transform.position + transform.forward;
        Instantiate(bulletPrefab, spawnPos, Quaternion.LookRotation((player.position - spawnPos).normalized));

        if (currentAmmo <= 0) StartCoroutine(ReloadRoutine());
    }

    private IEnumerator ReloadRoutine()
    {
        if (isReloading) yield break;
        isReloading = true;
        currentState = EnemyState.Reload;
        if (animator) animator.SetBool("IsReloading", true);

        yield return new WaitForSeconds(reloadTime);

        currentAmmo = maxAmmo;
        isReloading = false;
        if (animator) animator.SetBool("IsReloading", false);
        TransitionToIdle();
    }

    void TransitionToIdle() { currentState = EnemyState.Idle; if (animator) animator.SetBool("IsAiming", false); }
    void TransitionToAiming() { currentState = EnemyState.Aiming; if (animator) animator.SetBool("IsAiming", true); }

    void UpdateLaserPosition(bool playerFound)
    {
        if (laserLine == null) return;
        // 待機中・リロード中は消す
        if (!playerFound || currentState == EnemyState.Idle || isReloading) { laserLine.enabled = false; return; }

        // 射撃シーケンス（点滅）中でなければ常時点灯
        if (!isShooting) laserLine.enabled = true;

        laserLine.SetPosition(0, muzzlePoint.position);
        laserLine.SetPosition(1, player.position + Vector3.up);
    }

    // --- 着地・死亡ロジック ---
    void TransitionToLanding() { currentState = EnemyState.Landing; Invoke("StartFalling", 1f); }
    void StartFalling() { if (rb) { rb.isKinematic = false; rb.useGravity = false; } }
    void FixedUpdate() { if (currentState == EnemyState.Landing && rb != null) rb.velocity = Vector3.down * landingSpeed; }
    private void OnCollisionEnter(Collision c) { if (c.gameObject.CompareTag(groundTag)) { if (rb) rb.useGravity = true; TransitionToIdle(); } }
    public void TakeDamage(float d) { currentHealth -= d; if (currentHealth <= 0) Die(); }
    void Die() { isDead = true; if (animator) animator.SetTrigger("Die"); Destroy(gameObject, 2f); }
}