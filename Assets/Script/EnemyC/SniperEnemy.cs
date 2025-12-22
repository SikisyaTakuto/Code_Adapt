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
    public float landingSpeed = 2.0f;
    public string groundTag = "Ground";

    [Header("射撃タイミング設定")]
    public float chargeTime = 2.5f;
    public float postShotPause = 1.5f;

    [Header("索敵・角度制限設定")]
    public float sightRange = 80f;
    public float rotationSpeed = 3f;
    public float tooCloseDistance = 8f;   // 💡 少し離れた距離でキャンセル
    public float maxUpwardAngle = 45f;   // 💡 頭上（45度以上）にいたらキャンセル
    public float maxDownwardAngle = 30f; // 💡 足元（30度以下）にいたらキャンセル

    [Header("参照オブジェクト")]
    [SerializeField] private GameObject bulletPrefab;
    public Transform muzzlePoint;
    private LineRenderer laserLine;
    private Transform player;

    [Header("ターゲット調整")]
    public float targetHeightOffset = 1.2f;

    private Animator animator;
    private Rigidbody rb;
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

        float distance = Vector3.Distance(transform.position, player.position);

        if (isReloading)
        {
            LookAtPlayer();
            return;
        }

        bool playerFound = CheckForPlayer();

        if (playerFound)
        {
            LookAtPlayer();
            if (!isShooting && currentState == EnemyState.Idle) TransitionToAiming();
            if (currentState == EnemyState.Aiming) AimingLogic();
        }
        else
        {
            // 近すぎる、または角度外でも射程内なら体だけは向ける
            if (distance <= sightRange) LookAtPlayer();

            if (!isShooting && currentState != EnemyState.Idle) TransitionToIdle();
        }

        UpdateLaserPosition(playerFound);
    }

    void LookAtPlayer()
    {
        Vector3 dir = (player.position - transform.position).normalized;
        if (dir == Vector3.zero) return;

        // 左右の回転のみ適用
        Quaternion lookRot = Quaternion.LookRotation(new Vector3(dir.x, 0, dir.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, rotationSpeed * Time.deltaTime);
    }

    bool CheckForPlayer()
    {
        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, player.position);

        // 1. 距離チェック
        if (distance < tooCloseDistance || distance > sightRange) return false;

        // 2. 上下の角度チェック（仰角を計算）
        float verticalAngle = Mathf.Asin(dirToPlayer.y) * Mathf.Rad2Deg;
        if (verticalAngle > maxUpwardAngle || verticalAngle < -maxDownwardAngle) return false;

        // 3. 障害物チェック
        RaycastHit hit;
        Vector3 rayStart = transform.position + Vector3.up * 1.5f;
        Vector3 targetPos = player.position + Vector3.up * targetHeightOffset;
        Vector3 rayDir = (targetPos - rayStart).normalized;

        if (Physics.Raycast(rayStart, rayDir, out hit, sightRange))
            if (hit.collider.CompareTag("Player")) return true;

        return false;
    }

    void AimingLogic()
    {
        if (isShooting) return;

        Vector3 dir = (player.position - transform.position).normalized;
        float horizontalAngle = Quaternion.Angle(transform.rotation, Quaternion.LookRotation(new Vector3(dir.x, 0, dir.z)));

        // 正面（左右5度以内）を向いたら射撃開始
        if (horizontalAngle < 5f) StartCoroutine(FlashAndShootSequence());
    }

    private IEnumerator FlashAndShootSequence()
    {
        if (isShooting || isReloading) yield break;

        isShooting = true;
        currentState = EnemyState.Attack;

        float timer = 0;
        while (timer < chargeTime)
        {
            // 💡 リアルタイム角度・距離チェック
            Vector3 currentDir = (player.position - transform.position).normalized;
            float vAngle = Mathf.Asin(currentDir.y) * Mathf.Rad2Deg;
            float dist = Vector3.Distance(transform.position, player.position);
            float hAngle = Vector3.Angle(transform.forward, new Vector3(currentDir.x, 0, currentDir.z));

            // 無理な角度や距離になったら即中断
            if (dist < tooCloseDistance || vAngle > maxUpwardAngle || vAngle < -maxDownwardAngle || hAngle > 60f)
            {
                CancelShooting();
                yield break;
            }

            timer += Time.deltaTime;
            float blinkSpeed = (timer / chargeTime) > 0.7f ? 20f : 10f;
            if (laserLine) laserLine.enabled = (Mathf.FloorToInt(timer * blinkSpeed) % 2 == 0);

            yield return null;
        }

        if (laserLine) laserLine.enabled = false;
        if (animator) animator.SetTrigger("Shoot");

        ShootBullet();

        yield return new WaitForSeconds(postShotPause);

        isShooting = false;
        if (CheckForPlayer()) TransitionToAiming();
        else TransitionToIdle();
    }

    void CancelShooting()
    {
        if (laserLine) laserLine.enabled = false;
        isShooting = false;
        if (animator) animator.SetBool("IsAiming", false);
        currentState = EnemyState.Idle;
    }

    public void ShootBullet()
    {
        if (isDead || currentAmmo <= 0) return;
        currentAmmo--;

        Vector3 spawnPos = muzzlePoint ? muzzlePoint.position : transform.position + transform.forward;
        Vector3 targetPos = player.position + Vector3.up * targetHeightOffset;
        Vector3 targetDir = (targetPos - spawnPos).normalized;

        // 💡 弾の向き修正（必要に応じてEulerを調整）
        Instantiate(bulletPrefab, spawnPos, Quaternion.LookRotation(targetDir));

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
        if (!playerFound || currentState == EnemyState.Idle || isReloading)
        {
            if (!isShooting) { laserLine.enabled = false; return; }
        }

        if (!isShooting) laserLine.enabled = true;

        Vector3 startPos = muzzlePoint ? muzzlePoint.position : transform.position + Vector3.up * 1.5f;
        Vector3 targetPos = player.position + Vector3.up * targetHeightOffset;

        laserLine.SetPosition(0, startPos);
        laserLine.SetPosition(1, targetPos);
    }

    // --- 着地・死亡ロジック ---
    void TransitionToLanding()
    {
        currentState = EnemyState.Landing;
        if (animator) animator.SetTrigger("StartLanding"); // 💡 AnyState用ではなくトリガーにする
        Invoke("StartFalling", 1f);
    }
    void StartFalling() { if (rb) { rb.isKinematic = false; rb.useGravity = false; } }
    void FixedUpdate() { if (currentState == EnemyState.Landing && rb != null) rb.velocity = Vector3.down * landingSpeed; }

    private void OnCollisionEnter(Collision c)
    {
        if (currentState == EnemyState.Landing && c.gameObject.CompareTag(groundTag))
        {
            if (rb) rb.useGravity = true;
            TransitionToIdle();
        }
    }

    public void TakeDamage(float d) { currentHealth -= d; if (currentHealth <= 0) Die(); }
    void Die()
    {
        if (isDead) return;
        isDead = true;
        if (animator) animator.SetTrigger("Die");
        Destroy(gameObject, 2f);
    }
}