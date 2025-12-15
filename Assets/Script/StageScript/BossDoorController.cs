using UnityEngine;

public class BossDoorController : MonoBehaviour
{
    public float openHeight = 3f;       // 扉が開く高さ
    public float speed = 3f;            // 開閉スピード
    public string playerTag = "Player"; // プレイヤーのタグ

    private Vector3 closedPos;          // 元の位置
    private Vector3 openPos;            // 開いた位置
    private bool isOpening = false;     // 開いている最中か
    private bool isClosing = false;     // 閉じている最中か

    void Start()
    {
        closedPos = transform.position;
        openPos = closedPos + Vector3.up * openHeight;
    }

    void Update()
    {
        // Lキーで開く(レバー)
        if (Input.GetKeyDown(KeyCode.L)) 
        { 
            isOpening = true; 
            isClosing = false;
        }

        // 開く処理
        if (isOpening)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                openPos,
                speed * Time.deltaTime
            );

            if (Vector3.Distance(transform.position, openPos) < 0.01f)
            {
                isOpening = false;
            }
        }

        // 閉じる処理
        if (isClosing)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                closedPos,
                speed * Time.deltaTime
            );

            if (Vector3.Distance(transform.position, closedPos) < 0.01f)
            {
                isClosing = false;
            }
        }
    }

    // 外部から呼ばれる「開く」メソッド
    public void OpenDoor()
    {
        Debug.Log("a");
        isOpening = true;
        isClosing = false;
    }

    // プレイヤーが扉のコライダーに触れたら閉まる
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            isOpening = false;
            isClosing = true;
        }
    }
}
