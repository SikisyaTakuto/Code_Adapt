using UnityEngine;
using UnityEngine.AI; // NavMeshAgentはここでは使われていないが、元のコードに合わせて残す

public class BulletDestroy : MonoBehaviour
{
    [Header("弾の設定")]
    public float lifeTime = 5f;
    public float damageAmount = 10f; //  弾が与えるダメージ量

    void Start()
    {
        // lifeTime秒後に弾を自動で消滅させる
        Destroy(gameObject, lifeTime);
    }

    // Updateはここでは使用しないため空のまま
    void Update()
    {

    }

    private void OnTriggerEnter(Collider collider)
    {
        // 1. 衝突相手が "Player" タグを持っているかチェック
        if (collider.gameObject.tag == "Player")
        {
            // 2. 衝突相手から PlayerController コンポーネメントを取得
            PlayerController player = collider.GetComponent<PlayerController>();

            if (player != null)
            {
                // 3. ?? PlayerController の TakeDamage メソッドを呼び出し、ダメージを与える
                player.TakeDamage(damageAmount);
                Debug.Log($"Playerに{damageAmount}ダメージを与えました。");
            }
            else
            {
                Debug.LogWarning("Playerタグのオブジェクトに PlayerController が見つかりません！");
            }

            // Playerに当たったら弾を消す
            Destroy(gameObject);
            Debug.Log("弾を削除");
        }
    }
}