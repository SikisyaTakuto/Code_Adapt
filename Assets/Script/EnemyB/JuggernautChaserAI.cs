using UnityEngine;
using UnityEngine.AI;

public class JuggernautChaserAI : MonoBehaviour
{
    // --- 状態定義 ---
    public enum EnemyState { Dormant, Awakening, Chase, Attack }
    public EnemyState currentState = EnemyState.Dormant;

    // --- AI 設定 ---
    public Transform player;
    public float sightRangeDormant = 10f;
    public float sightRangeActive = 20f;
    public float attackRange = 10f;
    public float maxChaseDistance = 30f;
    public float viewAngle = 90f;
    public float rotationSpeed = 3f;
    public float awakeningTime = 2.0f;

    // --- 攻撃設定 (ミサイル発射) ---
    public GameObject missilePrefab;
    public Transform muzzlePointLeft;
    public Transform muzzlePointRight;
    public int missilesPerBurst = 4;
    public float timeBetweenMissiles = 0.3f;
    public float attackDuration = 1.0f;
    public float missileLaunchForce = 50f;

    // 💡 新規追加: 装填されているミサイルの視覚モデルの参照
    public GameObject loadedMissileLeft;  // 左腕に装填されたミサイルモデル
    public GameObject loadedMissileRight; // 右腕に装填されたミサイルモデル

    // --- コンポーネント & 内部変数 ---
    private NavMeshAgent agent;
    private Animator animator;
    private bool isLeftMuzzle = true;
    private EnemyHealth health;

    // ----------------------------------------------------------------------

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        health = GetComponent<EnemyHealth>();

        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null) player = playerObject.transform;
        }

        if (agent != null)
        {
            agent.enabled = false;
        }

        if (animator != null)
        {
            animator.SetBool("IsDormant", true);
        }
    }

    // ----------------------------------------------------------------------

    void Update()
    {
        // 🚨 死亡時の即時停止処理
        if (health != null && health.currentHealth <= 0)
        {
            HandleDeath();
            return;
        }

        // NavMeshAgent初期化チェック
        if (agent != null && !agent.enabled)
        {
            agent.enabled = true;
            if (agent.isActiveAndEnabled && !agent.pathPending)
            {
                TransitionToDormant();
            }
            return;
        }

        if (player == null || animator == null || agent == null || !agent.isActiveAndEnabled) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        bool playerFound = CheckForPlayer(distanceToPlayer);

        // --- 状態ごとのロジック実行 ---
        switch (currentState)
        {
            case EnemyState.Dormant:
                DormantLogic(distanceToPlayer);
                break;
            case EnemyState.Chase:
                ChaseLogic(playerFound, distanceToPlayer);
                break;
            case EnemyState.Attack:
                AttackLogic(playerFound, distanceToPlayer);
                break;
                // Awakening のロジックは Invoke に任せる
        }

        // Runアニメーション制御
        if (currentState == EnemyState.Chase)
        {
            bool isMoving = agent.velocity.magnitude > 0.1f;
            animator.SetBool("IsRunning", isMoving);
        }
        else
        {
            animator.SetBool("IsRunning", false);
        }
    }

    // ----------------------------------------------------
    // --- ロジック関数 (補完) ---
    // ----------------------------------------------------

    void DormantLogic(float distance)
    {
        // Dormant状態での視界範囲に入ったら覚醒へ
        if (distance <= sightRangeDormant)
        {
            TransitionToAwakening();
        }
    }

    void ChaseLogic(bool playerFound, float distance)
    {
        // 常にプレイヤーを目的地に設定
        agent.isStopped = false;
        agent.SetDestination(player.position);

        // 攻撃判定
        if (playerFound && distance <= attackRange)
        {
            TransitionToAttack();
        }
        // 💡 修正点 1: プレイヤーを見失った場合でも、最大追跡距離を超えない限りChaseを維持
        // プレイヤーを見失い、かつ最大追跡距離を超えたらDormantに戻る
        else if (distance > maxChaseDistance)
        {
            TransitionToDormant();
        }
    }

    void AttackLogic(bool playerFound, float distance)
    {
        // 攻撃中もプレイヤーの方向を追従（手動回転）
        Vector3 direction = (player.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);

        // プレイヤーが射程外に出たら強制的にChaseに戻る
        if (!playerFound || distance > attackRange * 1.2f)
        {
            TransitionToChase();
        }
    }

    // ----------------------------------------------------
    // --- 死亡処理関数 ---
    // ----------------------------------------------------

    void HandleDeath()
    {
        CancelInvoke();

        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        if (animator != null)
        {
            animator.SetBool("IsDormant", false);
            animator.SetBool("IsAwakening", false);
            animator.SetBool("IsRunning", false);
            animator.SetBool("IsAiming", false);
            // 💡 死亡アニメーションを再生する場合はここでトリガーをセット
            // animator.SetTrigger("Die");
        }
    }

    // ----------------------------------------------------
    // --- 状態遷移関数 (死亡チェック済み) ---
    // ----------------------------------------------------

    void TransitionToDormant()
    {
        if (health != null && health.currentHealth <= 0) return;

        currentState = EnemyState.Dormant;
        CancelInvoke();

        if (agent != null)
        {
            agent.isStopped = true;
            agent.updateRotation = true;
        }

        if (animator != null)
        {
            animator.SetBool("IsDormant", true);
            animator.SetBool("IsAwakening", false);
            animator.SetBool("IsRunning", false);
            animator.SetBool("IsAiming", false);
        }
    }

    void TransitionToAwakening()
    {
        if (health != null && health.currentHealth <= 0) return;

        currentState = EnemyState.Awakening;

        if (agent != null)
        {
            agent.isStopped = true;
            agent.updateRotation = true;
        }

        if (animator != null)
        {
            animator.SetBool("IsDormant", false);
            animator.SetBool("IsAwakening", true);
            animator.SetBool("IsRunning", false);
        }

        Invoke("TransitionToChase", awakeningTime);
    }

    void TransitionToChase()
    {
        if (health != null && health.currentHealth <= 0) return;
// 💡 修正点 2: 覚醒中にプレイヤーが遠ざかった場合の早期Dormant復帰ロジックを削除
        /* 削除されたロジック:
        if (currentState == EnemyState.Awakening && player != null && Vector3.Distance(transform.position, player.position) > sightRangeActive)
        {
            TransitionToDormant();
            return;
        }
        */

        currentState = EnemyState.Chase;
        CancelInvoke("FireMissile");
        CancelInvoke("TransitionToAttackComplete");

        if (agent != null)
        {
            agent.isStopped = false;
            agent.updateRotation = true;
        }

        if (animator != null)
        {
            animator.SetBool("IsAwakening", false);
            animator.SetBool("IsAiming", false);
        }
    }

    void TransitionToAttack()
    {
        if (health != null && health.currentHealth <= 0) return;

        currentState = EnemyState.Attack;
        CancelInvoke("FireMissile");

        if (agent != null)
        {
            agent.isStopped = true;
            agent.updateRotation = false;
        }

        if (animator != null)
        {
            animator.SetBool("IsRunning", false);
            animator.SetBool("IsAiming", true);
            animator.SetTrigger("Shoot");
        }

        for (int i = 0; i < missilesPerBurst; i++)
        {
            Invoke("FireMissile", i * timeBetweenMissiles);
        }

        float totalBurstTime = (missilesPerBurst - 1) * timeBetweenMissiles;
        float totalAttackTime = totalBurstTime + attackDuration;

        Invoke("TransitionToAttackComplete", totalAttackTime);
        isLeftMuzzle = true;
    }

    void TransitionToAttackComplete()
    {
        if (health != null && health.currentHealth <= 0) return;

        // 💡 修正点: 攻撃完了時にミサイルモデルを再表示（再装填演出）
        if (loadedMissileLeft != null) loadedMissileLeft.SetActive(true);
        if (loadedMissileRight != null) loadedMissileRight.SetActive(true);

        TransitionToChase();
    }

    // ----------------------------------------------------
    // --- ユーティリティ関数 ---
    // ----------------------------------------------------

    public void FireMissile()
    {
        if (health != null && health.currentHealth <= 0) return;

        if (missilePrefab == null || (muzzlePointLeft == null && muzzlePointRight == null))
        {
            Debug.LogError("ミサイルのプレハブまたは銃口が未設定です！");
            return;
        }

        Transform currentMuzzle = isLeftMuzzle ? muzzlePointLeft : muzzlePointRight;
        GameObject loadedMissile = isLeftMuzzle ? loadedMissileLeft : loadedMissileRight; // 参照取得

        if (currentMuzzle != null)
        {
            // 1. 装填ミサイルのビジュアルを非表示にする
            if (loadedMissile != null)
            {
                loadedMissile.SetActive(false); // 💡 発射したミサイルを非表示
            }

            GameObject newMissile = Instantiate(missilePrefab, currentMuzzle.position, currentMuzzle.rotation);
            newMissile.transform.parent = null;

            Rigidbody rb = newMissile.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(currentMuzzle.forward * missileLaunchForce, ForceMode.Impulse);
            }
        }

        isLeftMuzzle = !isLeftMuzzle;
    }

    bool CheckForPlayer(float currentDistance)
    {
        float activeSightRange = (currentState == EnemyState.Dormant) ? sightRangeDormant : sightRangeActive;

        if (currentDistance > activeSightRange) return false;

        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, directionToPlayer);

        if (angle > viewAngle / 2f) return false;

        RaycastHit hit;
        Vector3 eyePosition = transform.position + Vector3.up * 0.1f;

        if (Physics.Raycast(eyePosition, directionToPlayer, out hit, activeSightRange))
        {
            if (hit.collider.CompareTag("Player"))
            {
                return true;
            }
        }
        return false;
    }

    void OnDisable()
    {
        CancelInvoke();
    }
}