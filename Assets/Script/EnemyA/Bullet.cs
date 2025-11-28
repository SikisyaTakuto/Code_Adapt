using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 50f;
    public float damage = 10f;
    public float lifetime = 3f;

    // 💡 破壊処理が開始されたことを示すフラグ
    private bool isBeingDestroyed = false;

    void Start()
    {
        // lifetime秒後にGameObjectを破棄（自動消滅）を予約
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        // 💡 既に破棄処理中なら、移動処理をスキップ
        if (isBeingDestroyed) return;

        // 弾丸のローカル前方(Z軸)に移動
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        // 💡 既に破棄処理中なら、二重にDestroyを呼ばないようスキップ
        if (isBeingDestroyed) return;

        // プレイヤーに当たったかチェック
        if (other.CompareTag("Player"))
        {
            Debug.Log("プレイヤーに弾丸がヒット！");
            // ダメージ処理を実装
        }

        // 💡 フラグを設定し、即時破棄を実行
        isBeingDestroyed = true;
        Destroy(gameObject);
    }
}