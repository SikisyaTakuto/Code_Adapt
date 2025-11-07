using UnityEngine;
using UnityEngine.AI; // NavMeshAgentを使用するために必要
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))] // NavMeshAgentコンポーネントが必須であることを示す
[RequireComponent(typeof(EnemyHealth))] // EnemyHealthコンポーネントが必須であることを示す
public class EnemyController : MonoBehaviour
{
    [Header("Health Settings")]
    [Tooltip("敵の最大HP。EnemyHealthスクリプトで設定されますが、念のためこちらでも確認できます。")]
    public float maxHealth = 30f; // EnemyHealthと同期させるか、EnemyHealthで最終設定する
    private EnemyHealth enemyHealth; // EnemyHealthスクリプトへの参照

    [Header("Attack Settings")]
    [Tooltip("敵の攻撃力。プレイヤーに与えるダメージ。")]
    public float attackDamage = 10f; // 敵の攻撃力
    [Tooltip("ビーム攻撃のクールダウン時間（秒）。")]
    public float beamAttackCooldown = 3.0f; // ビーム攻撃のクールダウン
    [Tooltip("ビーム攻撃の射程距離。")]
    public float beamAttackRange = 20f; // ビーム攻撃の射程距離
    [Tooltip("ビームが生成される位置のTransform。")]
    public Transform beamSpawnPoint; // ビームが出る場所 (Raycastの開始点としても使用)
    [Tooltip("ビームのエフェクト/Prefab。")]
    public GameObject beamPrefab; // ビームのエフェクト（オプション）
    [Tooltip("ビームエフェクトの持続時間。")]
    public float beamDuration = 0.5f; // ビームエフェクトの表示時間
    [Tooltip("ターゲットとなるプレイヤーのタグ。")]
    public string playerTag = "Player"; // プレイヤーオブジェクトのタグ
    [Tooltip("攻撃を開始してからビームを発射するまでの準備時間（秒）。この間敵は停止する。")]
    public float attackPreparationTime = 1.0f; // 攻撃前の停止時間
    [Tooltip("ビームの線の太さ。")] // ★追加
    public float beamWidth = 0.5f; // ★追加
    [Tooltip("ビームの線の色（開始色）。")] // ★追加
    public Color beamStartColor = Color.red; // ★追加
    [Tooltip("ビームの線の色（終了色）。")] // ★追加
    public Color beamEndColor = Color.magenta; // ★追加

    private bool canAttack = true; // 攻撃クールダウン管理用
    private bool isAttacking = false; // 攻撃中フラグ
    private GameObject currentBeamVisualizer; // ★追加: 現在表示中のビームの視覚化オブジェクトへの参照

    [Header("Movement Settings")]
    [Tooltip("ランダム移動の基準となる中心点。")]
    public Vector3 walkPointCenter; // ランダム移動の中心点
    [Tooltip("ランダム移動の範囲（半径）。")]
    public float walkPointRange = 10f; // ランダム移動の範囲
    [Tooltip("NavMeshAgentの移動速度。")]
    public float moveSpeed = 3.5f; // 移動速度
    [Tooltip("目的地に到達したと見なす距離。")]
    public float destinationThreshold = 1.0f; // 目的地に到達したと見なす距離

    private NavMeshAgent agent; // NavMeshAgentコンポーネント
    private Vector3 currentDestination; // 現在の目的地

    [Header("State Settings")]
    [Tooltip("敵がアクティブかどうか。")]
    public bool isActivated = false; // 敵が行動を開始するかどうか

    private Transform playerTransform; // プレイヤーのTransform

    void Awake()
    {
        enemyHealth = GetComponent<EnemyHealth>();
        agent = GetComponent<NavMeshAgent>();

        // NavMeshAgentの速度を設定
        agent.speed = moveSpeed;

        // EnemyHealthのmaxHealthをこのスクリプトの設定で上書き（または同期）
        enemyHealth.maxHealth = maxHealth;
        // EnemyHealthのcurrentHealthはAwakeで初期化されるのでここでは不要

        // プレイヤーオブジェクトをタグで検索
        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObject != null)
        {
            playerTransform = playerObject.transform;
        }
        else
        {
            Debug.LogWarning($"プレイヤーオブジェクトにタグ '{playerTag}' が見つかりません。敵は攻撃行動を行いません。");
        }

        // ★追加: 敵のHPが0になったときにビームエフェクトを消去するためのイベント購読
        if (enemyHealth != null)
        {
            enemyHealth.onDeath += HandleEnemyDeath;
        }
    }

    void Start()
    {
        // ゲーム開始時に最初の目的地を設定
        SetNewRandomDestination();
        if (!isActivated)
        {
            // 非アクティブなら移動を停止
            agent.isStopped = true;
        }
    }

    void Update()
    {
        if (!isActivated || enemyHealth.currentHealth <= 0)
        {
            // HPが0以下になった場合、ビームエフェクトを強制的に消去
            if (enemyHealth.currentHealth <= 0)
            {
                if (currentBeamVisualizer != null)
                {
                    Destroy(currentBeamVisualizer);
                    currentBeamVisualizer = null;
                }
            }
            return; // 非アクティブまたはHPが0以下なら何もしない
        }

        // 攻撃中は移動ロジックを実行しない
        if (isAttacking)
        {
            // 攻撃中はプレイヤーの方を向き続ける
            if (playerTransform != null)
            {
                Vector3 lookAtPlayer = playerTransform.position;
                lookAtPlayer.y = transform.position.y; // Y軸は固定して水平方向だけ回転
                transform.LookAt(lookAtPlayer);
            }
            return; // 攻撃中はこれ以降のUpdate処理をスキップ
        }

        // プレイヤーが射程距離内にいるかチェック
        if (playerTransform != null && Vector3.Distance(transform.position, playerTransform.position) <= beamAttackRange)
        {
            // プレイヤーの方を向く
            Vector3 lookAtPlayer = playerTransform.position;
            lookAtPlayer.y = transform.position.y; // Y軸は固定して水平方向だけ回転
            transform.LookAt(lookAtPlayer);

            // 攻撃クールダウン中ではないか
            if (canAttack)
            {
                agent.isStopped = true; // 攻撃中は移動を停止
                StartCoroutine(BeamAttackRoutine());
            }
        }
        else
        {
            agent.isStopped = false; // プレイヤーが射程外なら移動を再開
            // 目的地に到達したか、まだ目的地が設定されていない場合
            if (!agent.pathPending && agent.remainingDistance < destinationThreshold)
            {
                SetNewRandomDestination();
            }
        }
    }

    /// <summary>
    /// ランダムな目的地を生成し、NavMeshAgentに設定する
    /// </summary>
    void SetNewRandomDestination()
    {
        // ランダムな方向を生成
        Vector3 randomDirection = Random.insideUnitSphere * walkPointRange;
        randomDirection += walkPointCenter; // 中心点を基準にする

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, walkPointRange, NavMesh.AllAreas))
        {
            currentDestination = hit.position;
            agent.SetDestination(currentDestination);
            Debug.Log($"新しい目的地を設定: {currentDestination}");
        }
        else
        {
            // 有効なNavMesh上の位置が見つからない場合、再試行
            Debug.LogWarning("NavMesh上で有効なランダムな位置が見つかりませんでした。再試行します。");
            // 少し待ってから再試行するか、別のロジックを検討
            // 今回は次のフレームで再度Updateが呼ばれるので、そこで再試行されることを期待
        }
    }

    /// <summary>
    /// ビーム攻撃のコルーチン
    /// </summary>
    IEnumerator BeamAttackRoutine()
    {
        canAttack = false; // 攻撃を開始したらクールダウン
        isAttacking = true; // 攻撃中フラグを立てる
        agent.isStopped = true; // 移動を確実に停止させる

        // 攻撃前の準備時間（数秒間停止する部分）
        Debug.Log("攻撃準備中...");
        yield return new WaitForSeconds(attackPreparationTime); // ここで数秒間停止する

        // 敵のHPが0以下の場合、ビームを撃たずに終了
        if (enemyHealth.currentHealth <= 0)
        {
            isAttacking = false;
            agent.isStopped = false;
            canAttack = true; // クールダウンをリセット
            yield break;
        }

        Vector3 rayOrigin = beamSpawnPoint.position;
        Vector3 rayDirection = (playerTransform.position - rayOrigin).normalized; // プレイヤーの方向へRayを飛ばす
        Vector3 beamEndPoint = rayOrigin + rayDirection * beamAttackRange; // ビームのデフォルト終点

        RaycastHit hit;
        //PlayerHealth playerHealth = null; // ヒットしたPlayerHealthを保持する変数

        if (Physics.Raycast(rayOrigin, rayDirection, out hit, beamAttackRange))
        {
            Debug.Log($"Raycastがヒットしました: {hit.collider.name}, タグ: {hit.collider.tag}");
            beamEndPoint = hit.point; // ヒットした位置をビームの終点とする

            if (hit.collider.CompareTag(playerTag))
            {
                //playerHealth = hit.collider.GetComponent<PlayerHealth>();
                //if (playerHealth != null)
                //{
                //    playerHealth.TakeDamage(attackDamage);
                //    Debug.Log($"敵がプレイヤーに {attackDamage} ダメージを与えました。（Raycast）");
                //}
                //else
                //{
                //    Debug.LogWarning("Raycastがプレイヤーにヒットしましたが、PlayerHealthコンポーネントが見つかりません。");
                //}
            }
        }
        else
        {
            Debug.Log("Raycastは何もヒットしませんでした。");
        }

        // ビームの視覚化（Line Renderer）
        // 既存のビジュアライザーがあれば破棄してから新しく生成
        if (currentBeamVisualizer != null)
        {
            Destroy(currentBeamVisualizer);
            currentBeamVisualizer = null;
        }
        currentBeamVisualizer = new GameObject("EnemyBeamVisualizer");
        LineRenderer lineRenderer = currentBeamVisualizer.AddComponent<LineRenderer>();
        lineRenderer.startWidth = beamWidth;
        lineRenderer.endWidth = beamWidth;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default")); // 標準のシェーダー
        lineRenderer.startColor = beamStartColor;
        lineRenderer.endColor = beamEndColor;
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, rayOrigin);
        lineRenderer.SetPosition(1, beamEndPoint);

        // ビームエフェクトの生成と表示（オプション）
        GameObject beamEffectInstance = null;
        if (beamPrefab != null)
        {
            beamEffectInstance = Instantiate(beamPrefab, rayOrigin, Quaternion.identity);
            beamEffectInstance.transform.LookAt(beamEndPoint); // エフェクトをビームの方向に向ける
            beamEffectInstance.transform.parent = currentBeamVisualizer.transform; // ラインレンダラーの子にする（まとめて破棄するため）
        }

        // 指定時間表示した後、破棄
        yield return new WaitForSeconds(beamDuration);
        if (currentBeamVisualizer != null) // コルーチン中に敵が倒された場合に備えてnullチェック
        {
            Destroy(currentBeamVisualizer); // Line Rendererと子オブジェクト（エフェクト）をまとめて破棄
            currentBeamVisualizer = null;
        }

        // 攻撃アニメーションやSE再生などがあればここに追加

        yield return new WaitForSeconds(beamAttackCooldown); // クールダウン待機
        canAttack = true; // クールダウン終了
        isAttacking = false; // 攻撃中フラグをリセット
        agent.isStopped = false; // 攻撃が完了したら移動を再開
    }

    /// <summary>
    /// 敵のHPが0になったときに呼び出されるハンドラ。
    /// </summary>
    private void HandleEnemyDeath()
    {
        Debug.Log("敵が倒されました！ビームエフェクトを消去します。");
        if (currentBeamVisualizer != null)
        {
            Destroy(currentBeamVisualizer);
            currentBeamVisualizer = null;
        }
        // 敵が倒されたら、NavMeshAgentを停止
        if (agent != null && agent.enabled)
        {
            agent.isStopped = true;
            agent.enabled = false; // エージェントを無効化して移動を完全に停止
        }
        // 必要に応じて、敵のモデルを非表示にする、パーティクルエフェクトを再生するなどの処理を追加
        // 例: gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        // ★追加: スクリプトが破棄されるときにイベント購読を解除する
        if (enemyHealth != null)
        {
            enemyHealth.onDeath -= HandleEnemyDeath;
        }
    }

    /// <summary>
    /// 敵をアクティブにするメソッド（チュートリアルスクリプトから呼び出すことを想定）
    /// </summary>
    public void ActivateEnemy(Vector3 centerPoint, float range)
    {
        isActivated = true;
        if (agent != null && !agent.enabled) // エージェントが無効になっている場合、有効にする
        {
            agent.enabled = true;
        }
        agent.isStopped = false; // 移動を再開
        walkPointCenter = centerPoint; // チュートリアルで設定された範囲を使用
        walkPointRange = range;
        SetNewRandomDestination(); // アクティブになったらすぐに最初の目的地を設定
        Debug.Log("敵がアクティブになりました。");
    }

    // デバッグ表示用（シーンビューでのみ表示）
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        // ランダム移動の範囲を球体で表示
        Gizmos.DrawWireSphere(walkPointCenter, walkPointRange);

        Gizmos.color = Color.red;
        // ビーム攻撃の射程距離を球体で表示
        if (playerTransform != null)
        {
            Gizmos.DrawWireSphere(transform.position, beamAttackRange);
        }

        // Raycastの開始位置と方向を視覚化
        if (beamSpawnPoint != null && playerTransform != null)
        {
            Gizmos.color = Color.cyan;
            Vector3 rayOrigin = beamSpawnPoint.position;
            Vector3 rayDirection = (playerTransform.position - rayOrigin).normalized;
            Gizmos.DrawLine(rayOrigin, rayOrigin + rayDirection * beamAttackRange);
            Gizmos.DrawSphere(rayOrigin, 0.1f); // Rayの始点に小さな球を描画
        }
    }
}
