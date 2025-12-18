using UnityEngine;

public class DoorController : MonoBehaviour
{
    // public Transform player; // 手動アサインは不要になるので削除または非表示に
    private Transform playerTransform;

    public float openDistance = 3f;   // 開く距離
    public float openHeight = 3f;     // 開く高さ
    public float speed = 3f;          // 開閉スピード

    private Vector3 closedPos;        // 閉じた位置
    private Vector3 openPos;          // 開いた位置

    void Start()
    {
        closedPos = transform.position;
        openPos = closedPos + Vector3.up * openHeight;

        // ★追加: "Player" タグのオブジェクトを探して Transform を取得
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
        else
        {
            Debug.LogError("シーン内に 'Player' タグがついたオブジェクトが見つかりません！");
        }
    }

    void Update()
    {
        // プレイヤーが見つかっていない場合は処理しない
        if (playerTransform == null) return;

        float distance = Vector3.Distance(playerTransform.position, transform.position);
        bool isPlayerNear = distance < openDistance;

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