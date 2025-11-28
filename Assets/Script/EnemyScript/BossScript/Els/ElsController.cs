using UnityEngine;
using UnityEngine.AI;

public class ElsController : MonoBehaviour
{
    // ボスの動きを制御するためのフラグ
    public bool isActivated = false;

    // プレイヤーのTransform
    public Transform playerTransform;

    // プレイヤーと保ちたい最小距離
    public float keepDistance = 10f;

    public float Acceleration = 15f;

    // ランダムな速度設定のための変数
    [Header("Random Speed Settings")]
    public float minSpeed = 3f;     // 最小速度
    public float maxSpeed = 8f;     // 最大速度
    public float speedChangeInterval = 3f; // 速度を切り替える間隔 (秒)

    private float speedChangeTimer; // 速度切り替え用のタイマー

    // ボスのNavMeshAgentコンポーネント
    private NavMeshAgent agent;

    // 浮遊機能関連の変数
    [Header("Floating Settings")]
    public float floatHeight = 5f; // 空中に浮上させる目標の高さ
    public float floatSpeed = 2f; // 浮上・下降の速度
    private bool isFloating = false; // ボスが浮いているかどうか

    public float modeChangeInterval = 10f; // モードを切り替える間隔 (秒)
    private float modeChangeTimer; // モード切り替え用のタイマー

    [Range(0f, 1f)]
    public float floatingChance = 0.5f;// 確率で浮遊モードになる

    private float groundY; // 地面でのY座標を保持
    private float currentMoveSpeed; // 現在の移動速度

    void Start()
    {
        // NavMeshAgentコンポーネントを取得
        agent = GetComponent<NavMeshAgent>();

        // NavMeshAgentがアタッチされているか確認
        if (agent == null)
        {
            Debug.LogError("NavMeshAgentコンポーネントが見つかりません。");
            enabled = false; // スクリプトを無効化
        }

        // プレイヤーのTransformが設定されているか確認
        if (playerTransform == null)
        {
            Debug.LogError("Player Transformが設定されていません。");
        }

        // 慣性を調整するための設定
        agent.stoppingDistance = keepDistance;

        // 減速度を下げて、停止時に滑らかにする
        agent.acceleration = Acceleration;

        // 方向転換を緩やかにする
        agent.angularSpeed = 360f;

        // タイマーを初期化し、即座に速度が設定されるようにする
        speedChangeTimer = speedChangeInterval;

        // 初期のY座標を記録
        groundY = transform.position.y;
    }

    void Update()
    {
        if (isActivated)
        {

            // ランダムなモード切り替えの処理
            HandleRandomModeChange();

            // 浮上・下降処理は常に行う
            HandleFloatingHeight();

            // メイン移動ロジック
            if (isActivated && playerTransform != null && agent != null)
            {
                // NavMeshAgentが有効ならNavMeshAgent経由で移動
                if (agent.enabled)
                {
                    HandleNavMeshMovement();
                }
                // 浮遊中（NavMeshAgentが無効）なら手動で移動
                else if (isFloating)
                {
                    HandleManualMovement();
                }

                // ランダムな速度の切り替えロジック
                HandleRandomSpeed();
            }
            else if (agent != null && agent.enabled)
            {
                // ボスの動きが無効化されている場合、念のため停止させる
                agent.isStopped = true;
            }
        }
    }

    // 確率に基づくランダムなモード切り替え処理
    private void HandleRandomModeChange()
    {
        // タイマーを減らす
        modeChangeTimer -= Time.deltaTime;

        if (modeChangeTimer <= 0)
        {
            // Random.value は 0.0f から 1.0f の間の値を返す
            bool shouldFloat = Random.value < floatingChance;

            // 新しいモードを設定
            SetFloatingMode(shouldFloat);

            // タイマーをリセット
            modeChangeTimer = modeChangeInterval;
        }
    }

    // 浮遊モードの切り替え処理を分離
    private void SetFloatingMode(bool newFloatingState)
    {
        if (isFloating == newFloatingState) return; // 状態が変わらない場合は何もしない

        isFloating = newFloatingState;

        if (agent != null)
        {
            agent.enabled = !isFloating;

            if (isFloating)
            {
                // 浮遊モード開始
                //agent.isStopped = true;
                // NavMeshAgentの速度を手動移動の速度としてコピー
                currentMoveSpeed = agent.speed;
                Debug.Log("Boss: ランダム切り替えにより **浮遊モード** を開始しました！");
            }
            else
            {
                // 通常モード復帰
                Debug.Log("Boss: ランダム切り替えにより **通常モード** に戻ります。");
                // 地面に戻ったら、NavMeshAgentの速度を手動移動時の速度にリセット
                agent.speed = currentMoveSpeed;
            }
        }
    }

    // NavMeshAgentを使用した移動ロジック
    private void HandleNavMeshMovement()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer > keepDistance)
        {
            // 追跡
            agent.SetDestination(playerTransform.position);
            agent.isStopped = false;
        }
        else
        {
            // 停止
            agent.isStopped = true;
            Debug.Log("Boss: プレイヤーと一定距離を保って停止 (NavMesh)");
        }
    }

    // NavMeshAgentを使用しない手動移動ロジック
    private void HandleManualMovement()
    {
        // Y座標を無視した水平距離を計算
        Vector3 currentPos = transform.position;
        Vector3 playerPos = playerTransform.position;
        Vector3 flatCurrentPos = new Vector3(currentPos.x, 0, currentPos.z);
        Vector3 flatPlayerPos = new Vector3(playerPos.x, 0, playerPos.z);

        float distanceToPlayer = Vector3.Distance(flatCurrentPos, flatPlayerPos);

        Vector3 targetPosition = flatCurrentPos;

        if (distanceToPlayer > keepDistance)
        {
            // 追跡: プレイヤーの水平位置へ向かう
            Vector3 direction = (flatPlayerPos - flatCurrentPos).normalized;
            targetPosition = currentPos + direction * currentMoveSpeed * Time.deltaTime;
        }
        else
        {
            // 停止: 現在の位置を維持
            // ターゲット位置は現在の水平位置のまま
            Debug.Log("Boss: プレイヤーと一定距離を保って停止 (浮遊)");
            targetPosition = currentPos;
        }

        // ターゲット位置のY座標を現在のものに設定し直す
        targetPosition.y = currentPos.y;

        // ボスの位置を更新
        transform.position = targetPosition;

        // プレイヤーの方向を向く
        Vector3 lookDirection = playerPos - currentPos;
        lookDirection.y = 0; // Y軸回転は無視
        if (lookDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDirection), Time.deltaTime * 5f);
        }
    }

    // ランダムな移動速度を定期的に設定する
    private void HandleRandomSpeed()
    {
        // タイマーを減らす
        speedChangeTimer -= Time.deltaTime;

        // タイマーが0以下になったら速度を変更
        if (speedChangeTimer <= 0)
        {
            float newSpeed;

            // Random.value (0.0f～1.0f) を使って、50%の確率でどちらかを選ぶ
            if (Random.value < 0.5f)
            {
                // 50%の確率で minSpeed を選ぶ
                newSpeed = minSpeed;
            }
            else
            {
                // 残り50%の確率で maxSpeed を選ぶ
                newSpeed = maxSpeed;
            }

            // NavMeshAgentの速度に設定
            agent.speed = newSpeed;

            // 次の速度変更までのタイマーをリセット
            speedChangeTimer = speedChangeInterval;

            Debug.Log($"Boss: 移動速度を **{newSpeed:F2}** に変更しました。");
        }
    }

    // 浮遊高の制御 (Y軸の移動のみ)
    private void HandleFloatingHeight()
    {
        // 浮遊中なら groundY + floatHeight、解除中なら groundY へ向かう
        float targetY = isFloating ? groundY + floatHeight : groundY;

        // Y座標をスムーズに移動させる
        float newY = Mathf.MoveTowards(transform.position.y, targetY, floatSpeed * Time.deltaTime);

        // 新しい位置を設定
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);

        // 地面に戻る処理が完了し、かつ浮遊モードがオフになったとき
        if (!isFloating && Mathf.Approximately(newY, groundY))
        {
            if (agent != null && !agent.enabled)
            {
                agent.enabled = true; // NavMeshAgentを再開
            }
        }
    }
}