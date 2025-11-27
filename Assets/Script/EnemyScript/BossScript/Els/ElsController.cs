using UnityEngine;
using UnityEngine.AI; // NavMeshAgentを使うために必要

public class ElsController : MonoBehaviour
{
    // ボスの動きを制御するためのフラグ
    public bool isActivated = false;

    // プレイヤーのTransform
    public Transform playerTransform;

    // プレイヤーと保ちたい最小距離
    public float keepDistance = 10f;

    // ランダムな速度設定のための変数
    [Header("Random Speed Settings")]
    public float minSpeed = 3f;     // 最小速度
    public float maxSpeed = 8f;     // 最大速度
    public float speedChangeInterval = 3f; // 速度を切り替える間隔 (秒)

    private float speedChangeTimer; // 速度切り替え用のタイマー

    // ボスのNavMeshAgentコンポーネント
    private NavMeshAgent agent;

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

        // 減速度を下げて、停止時に滑らかに（慣性のような動き）する
        agent.acceleration = 8f; // デフォルト(6)より少し大きく、あるいは小さくして調整

        // 方向転換を緩やかにする
        agent.angularSpeed = 360f; // デフォルト(1200)より低くして調整

        // タイマーを初期化し、即座に速度が設定されるようにする
        speedChangeTimer = speedChangeInterval;
    }

    void Update()
    {
        if (isActivated && playerTransform != null)
        {
            // プレイヤーとボスの現在の距離を計算
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

            // プレイヤーとの距離をチェックし、追跡/停止を切り替える
            if (distanceToPlayer > keepDistance)
            {
                // 距離が離れすぎている場合 (設定距離より大きい)
                // プレイヤーの場所を目的地に設定し、追いかける
                agent.SetDestination(playerTransform.position);

                // ボスを動かす (NavMeshAgentが自動で移動)
                agent.isStopped = false;

                //Debug.Log("Boss: プレイヤーを追跡中");
            }
            else
            {
                // 設定距離内にある場合ボスを停止させる
                agent.isStopped = true;

                Debug.Log("Boss: プレイヤーと一定距離を保って停止");
            }

            // ランダムな速度の切り替えロジック
            HandleRandomSpeed();
        }
        else if (agent != null)
        {
            // ボスの動きが無効化されている場合、念のため停止させる
            agent.isStopped = true;
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
}