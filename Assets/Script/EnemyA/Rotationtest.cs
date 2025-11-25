using UnityEngine;

public class RotationTest : MonoBehaviour
{
    private Transform player;

    void Start()
    {
        // Playerを見つける
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
    }

    void Update()
    {
        if (player == null) return;

        // プレイヤーの方向を計算
        Vector3 direction = (player.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));

        // 💡 プレイヤーの方向へ即座に向く（強制回転）
        transform.rotation = lookRotation;

        Debug.Log("RotationTest: 強制回転中");
    }
}