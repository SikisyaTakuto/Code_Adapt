using UnityEngine;
using UnityEngine.AI; // NavMesh Agentを使うために必要

public class NavigatorController : MonoBehaviour
{
    // NavMesh Agentコンポーネントへの参照
    private NavMeshAgent navMeshAgent;

    [Header("移動目標地点")]
    // 移動させたい目標地点のTransform（例: Playerの位置や、特定のチュートリアルポイント）
    public Transform targetDestination;

    void Start()
    {
        // 必須：自身のゲームオブジェクトからNavMeshAgentコンポーネントを取得
        navMeshAgent = GetComponent<NavMeshAgent>();

        // NavMeshAgentがアタッチされているか確認
        if (navMeshAgent == null)
        {
            Debug.LogError("NavMeshAgentコンポーネントがアタッチされていません！");
        }
    }

    void Update()
    {
        // 目標地点が設定されていて、NavMeshAgentが有効な場合
        if (targetDestination != null && navMeshAgent != null && navMeshAgent.isActiveAndEnabled)
        {
            // NavMesh Agentに目標地点を設定し、自動で移動を開始させる
            navMeshAgent.SetDestination(targetDestination.position);
        }
    }

    // 外部から目標地点を設定するためのパブリックメソッド
    public void SetNewDestination(Transform newTarget)
    {
        targetDestination = newTarget;
    }
}