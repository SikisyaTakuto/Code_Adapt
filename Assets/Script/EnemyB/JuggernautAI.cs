using UnityEngine;
using UnityEngine.AI;

public class JuggernautStaticAI : MonoBehaviour
{
    // --- 状態定義 ---
    // 💡 ChaseをIdleに、Attackを再導入
    public enum EnemyState { Dormant, Awakening, Idle, Attack }
    public EnemyState currentState = EnemyState.Dormant; // 初期状態はDormant

    // --- AI 設定 ---
    public Transform player;
    public float sightRangeDormant = 10f; // Down状態からStandupへの閾値
    public float sightRangeActive = 20f;  // Idle中の視界範囲
    public float attackRange = 10f;       // 💡 攻撃開始距離
    public float viewAngle = 90f;
    public float rotationSpeed = 3f;      // 追従時の回転速度

    // --- 覚醒設定 ---
    public float awakeningTime = 2.0f;    // Standupアニメーションの時間

    // --- 💡 攻撃設定 (ミサイル発射) ---
    public GameObject missilePrefab;     // 💡 発射するミサイルのプレハブ
    public Transform muzzlePointLeft;   // 左腕の銃口
    public Transform muzzlePointRight;  // 右腕の銃口

    public int missilesPerBurst = 4;        // 1回のバーストで発射するミサイル数
    public float timeBetweenMissiles = 0.3f; // バースト内の連射間隔
    private bool isLeftMuzzle = true;
    public float attackDuration = 1.5f;       // 攻撃後のクールダウン時間 (次の攻撃までの間隔)

    // --- 💡 追加 ---
    // ミサイル設定に追加
    public float missileLaunchForce = 50f; // ミサイルの発射初速

    // --- コンポーネント ---
    private Animator animator;

    // ----------------------------------------------------------------------

    void Start()
    {
        animator = GetComponent<Animator>();

        // ... (プレイヤーとアニメーターのnullチェック) ...

        if (animator != null)
        {
            animator.SetBool("IsDormant", true);
        }
    }

    // ----------------------------------------------------------------------

    void Update()
    {
        if (player == null || animator == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        bool playerFound = CheckForPlayer(distanceToPlayer);

        switch (currentState)
        {
            case EnemyState.Dormant:
                DormantLogic(distanceToPlayer);
                break;
            case EnemyState.Awakening:
                break;
            case EnemyState.Idle: // 💡 Idleロジック (追従と攻撃判定を行う)
                IdleLogic(playerFound, distanceToPlayer);
                break;
            case EnemyState.Attack: // 💡 Attackロジック (攻撃中の追従と終了判定を行う)
                AttackLogic(playerFound, distanceToPlayer);
                break;
        }
    }

    // ----------------------------------------------------
    // --- ロジック関数 ---
    // ----------------------------------------------------

    void DormantLogic(float distance)
    {
        if (distance <= sightRangeDormant)
        {
            TransitionToAwakening();
        }
    }

    // 💡 追従ロジックと攻撃判定をIdleで実行
    void IdleLogic(bool playerFound, float distance)
    {
        // 1. プレイヤーを狙う (追従)
        if (playerFound)
        {
            Vector3 direction = (player.position - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);
        }

        // 2. 攻撃判定
        if (playerFound && distance <= attackRange)
        {
            TransitionToAttack();
        }
        // プレイヤーを見失っても状態はIdleのまま維持
    }

    // 💡 攻撃中のロジック
    void AttackLogic(bool playerFound, float distance)
    {
        // 攻撃中もプレイヤーの方向を追従
        Vector3 direction = (player.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);

        // プレイヤーが射程外に出たり、視界から外れた場合は攻撃完了を待たずに強制的にIdleに戻る
        if (!playerFound || distance > attackRange * 1.2f)
        {
            TransitionToIdle();
        }
    }

    // ----------------------------------------------------
    // --- 状態遷移関数 ---
    // ----------------------------------------------------

    void TransitionToAwakening()
    {
        currentState = EnemyState.Awakening;
        if (animator != null)
        {
            animator.SetBool("IsDormant", false);
            animator.SetTrigger("Awaken");       // Standupアニメーション開始
            animator.SetBool("IsIdle", false);  // 💡 Idleアニメフラグを修正
        }
        Invoke("TransitionToAwakeningComplete", awakeningTime);
    }

    void TransitionToAwakeningComplete()
    {
        TransitionToIdle(); // 💡 覚醒完了後はIdleへ移行
    }

    // 💡 新しいIdle状態への遷移 (追従/待機状態)
    void TransitionToIdle()
    {
        currentState = EnemyState.Idle;
        CancelInvoke("FireMissile");
        CancelInvoke("TransitionToAttackComplete");

        if (animator != null)
        {
            animator.SetBool("IsDormant", false);
            animator.SetBool("IsIdle", true); // 💡 Idleアニメーション開始
            animator.SetBool("IsAiming", false); // 攻撃構えを解除
        }
    }

    // 💡 Attack状態への遷移
    void TransitionToAttack()
    {
        currentState = EnemyState.Attack;
        CancelInvoke("FireMissile");

        if (animator != null)
        {
            animator.SetBool("IsIdle", false);
            animator.SetBool("IsAiming", true); // 攻撃構えアニメーション
            animator.SetTrigger("Shoot");       // 攻撃発射トリガー
        }

        // 左右交互発射のループを予約
        for (int i = 0; i < missilesPerBurst; i++)
        {
            Invoke("FireMissile", i * timeBetweenMissiles); // 💡 関数名を修正
        }

        float totalBurstTime = (missilesPerBurst - 1) * timeBetweenMissiles;
        float totalAttackTime = totalBurstTime + attackDuration;

        Invoke("TransitionToAttackComplete", totalAttackTime);
        isLeftMuzzle = true; // 銃口リセット
    }

    void TransitionToAttackComplete()
    {
        // 攻撃が完了したらIdleに戻る (Idleが追従と攻撃判定を行うため)
        TransitionToIdle();
    }

    // ----------------------------------------------------
    // --- プレイヤー視界判定 ---
    // ----------------------------------------------------

    bool CheckForPlayer(float currentDistance)
    {
        float activeSightRange = (currentState == EnemyState.Dormant) ? sightRangeDormant : sightRangeActive;

        if (currentDistance > activeSightRange) return false;

        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, directionToPlayer);
        // Chase状態でも視野角外は反応しない
        if (angle > viewAngle / 2f) return false;

        // Raycastによる遮蔽物チェック
        RaycastHit hit;
        Vector3 eyePosition = transform.position + Vector3.up * 0.1f;

        if (Physics.Raycast(eyePosition, directionToPlayer, out hit, activeSightRange))
        {
            if (hit.collider.CompareTag("Player"))
            {
                Debug.DrawLine(eyePosition, hit.point, Color.red);
                return true;
            }
        }
        return false;
    }

    // ----------------------------------------------------
    // --- 💡 ミサイル発射処理 (両腕交互発射) ---
    // ----------------------------------------------------

    public void FireMissile()
    {
        if (missilePrefab == null || (muzzlePointLeft == null && muzzlePointRight == null))
        {
            Debug.LogError("JuggernautStaticAI: ミサイルのプレハブまたは銃口が未設定です！");
            return;
        }

        Transform currentMuzzle = isLeftMuzzle ? muzzlePointLeft : muzzlePointRight;

        if (currentMuzzle != null)
        {
            // 1. ミサイルを生成
            GameObject newMissile = Instantiate(missilePrefab, currentMuzzle.position, currentMuzzle.rotation);
            newMissile.transform.parent = null;

            // 2. 💡 Rigidbodyを取得し、前方に力を加える
            Rigidbody rb = newMissile.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // 銃口の前方 (currentMuzzle.forward) に力を加える
                rb.AddForce(currentMuzzle.forward * missileLaunchForce, ForceMode.Impulse);
            }
            else
            {
                Debug.LogWarning("ミサイルPrefabにRigidbodyがありません。");
            }
        }

        isLeftMuzzle = !isLeftMuzzle;
    }

    // ----------------------------------------------------
    // --- 安全対策 (OnDisable) ---
    // ----------------------------------------------------

    void OnDisable()
    {
        CancelInvoke("TransitionToAwakeningComplete");
        CancelInvoke("FireMissile");
        CancelInvoke("TransitionToAttackComplete");
    }
}