using UnityEngine;

public class DoorController : MonoBehaviour
{
    public Transform player;          // プレイヤー
    public float openDistance = 3f;   // 開く距離
    public float openHeight = 3f;     // 開く高さ
    public float speed = 3f;          // 開閉スピード

    private Vector3 closedPos;        // 閉じた位置
    private Vector3 openPos;          // 開いた位置
    private bool isPlayerNear;        // プレイヤーが近いかどうか

    void Start()
    {
        closedPos = transform.position;
        openPos = closedPos + Vector3.up * openHeight;
    }

    void Update()
    {
        float distance = Vector3.Distance(player.position, transform.position);
        isPlayerNear = distance < openDistance;

        if (isPlayerNear)
        {
            // 開く（openPos まで）
            transform.position = Vector3.MoveTowards(transform.position, openPos, speed * Time.deltaTime);
        }
        else
        {
            // 閉じる（closedPos まで）
            transform.position = Vector3.MoveTowards(transform.position, closedPos, speed * Time.deltaTime);
        }
    }
}
