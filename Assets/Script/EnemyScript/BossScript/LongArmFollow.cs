using UnityEngine;

public class LongArmFollow : MonoBehaviour
{
    public Transform player; // プレイヤーの座標
    public float followSpeed = 2f;
    public Vector3 offset = new Vector3(0, 1, 2); // 腕を伸ばす方向

    void Update()
    {
        if (player == null) return;

        // ターゲットの位置をプレイヤーの少し前に設定
        Vector3 targetPos = player.position + offset;

        // ゆっくり追いかけさせることで、長い腕が「しなる」ように動く
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * followSpeed);
    }
}