using UnityEngine;

/// <summary>
/// 生成されたビームを指定時間後に破壊するスクリプト。
/// </summary>
public class BeamDestroyer : MonoBehaviour
{
    [Tooltip("ビームが自動的に破壊されるまでの時間 (秒)。")]
    public float lifetime = 5f; // 例: 5秒後に消滅させる

    private void Start()
    {
        // オブジェクトを一定時間後に破壊する
        // 衝突しなくても、この時間で画面外のビームも自動で消える
        Destroy(gameObject, lifetime);
    }

    // 衝突時の処理を追加する場合は以下を使用 (ビームにColliderとRigidbodyが必要)
    private void OnCollisionEnter(Collision collision)
    {
        // 衝突したらビームをすぐに破壊
        Destroy(gameObject);
    }

    // Triggerイベントを使用する場合は以下を使用 (ビームのColliderをIsTrigger=trueにする必要あり)
    private void OnTriggerEnter(Collider other)
    {
        // 衝突したらビームをすぐに破壊
        Destroy(gameObject);
    }
}