using UnityEngine;
using UnityEngine.AI;

public class TireRotator : MonoBehaviour
{
    [Header("参照")]
    public NavMeshAgent agent; // 親についているAgentをアサイン

    [Header("回転設定")]
    public float tireRadius = 0.5f; // タイヤの半径（回転速度の計算に使用）

    void Update()
    {
        if (agent == null || !agent.enabled) return;

        // エージェントの現在の速度（秒速）を取得
        float speed = agent.velocity.magnitude;

        // 移動距離から回転角度を計算 (角度 = 距離 / 半径 * 180 / π)
        // 速度がプラスでもマイナスでも進んでいる方向に回るように計算
        float rotationDegree = (speed * Time.deltaTime / tireRadius) * Mathf.Rad2Deg;

        // X軸を中心に回転
        transform.Rotate(Vector3.right, rotationDegree);
    }
}