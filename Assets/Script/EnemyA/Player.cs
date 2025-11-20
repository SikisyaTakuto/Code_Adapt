using UnityEngine;

//[RequireComponent(typeof(Rigidbody))] // Rigidbodyコンポーネントが必須であることを指定
public class Player : MonoBehaviour
{
    [Header("移動設定")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 7f;

    private Rigidbody rb;
    private bool isGrounded = true; // 接地判定フラグ

    void Start()
    {
        // 起動時にRigidbodyコンポーネントを取得
        rb = GetComponent<Rigidbody>();

        // プレイヤーの回転が物理演算の影響を受けないように設定（望ましくない傾きを防ぐ）
        rb.freezeRotation = true;
    }

    [System.Obsolete]
    void Update()
    {
        // ユーザー入力を取得
        float moveX = Input.GetAxis("Horizontal"); // A/Dキー
        float moveZ = Input.GetAxis("Vertical");   // W/Sキー

        // **1. 移動処理 (FixedUpdateで実行するのが望ましいが、ここではUpdateで簡易的に)**
        // 現在のY軸速度を維持しつつ、新しい水平速度を設定
        Vector3 moveVelocity = new Vector3(moveX, 0, moveZ).normalized * moveSpeed;

        // 進行方向を考慮したワールド座標への変換
        // このスクリプトがアタッチされたオブジェクトの向きを基準に移動ベクトルを変換
        Vector3 worldMove = transform.TransformDirection(moveVelocity);

        // 最終的なRigidbodyの速度を設定
        rb.velocity = new Vector3(worldMove.x, rb.velocity.y, worldMove.z);

        // **2. ジャンプ処理**
        if (Input.GetButtonDown("Jump") && isGrounded) // Spaceキーが押され、接地している場合
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse); // 瞬間的な力を加える
            isGrounded = false; // ジャンプ中は接地フラグを下ろす
        }
    }

    // 衝突判定を使用して接地フラグを更新
    private void OnCollisionEnter(Collision collision)
    {
        // Y軸方向の上向き（地面）からの衝突であるか確認
        if (collision.gameObject.CompareTag("Ground")) // 地面となるオブジェクトに"Ground"タグを設定してください
        {
            isGrounded = true;
        }
    }
}