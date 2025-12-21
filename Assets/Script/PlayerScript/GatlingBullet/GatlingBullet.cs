using UnityEngine;

public class GatlingBullet : MonoBehaviour
{
    public float speed = 100f;        // 弾速
    public float damage = 10f;       // 1発あたりのダメージ
    public float lifeTime = 2f;      // 自然消滅までの時間
    public GameObject hitEffect;     // 着弾エフェクト

    private Vector3 direction;

    // 発射時に方向を設定するメソッド
    public void Launch(Vector3 dir)
    {
        direction = dir.normalized;
        // 弾を進行方向に向ける
        transform.rotation = Quaternion.LookRotation(direction);
        // 一定時間後に削除
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        // 直進移動
        transform.position += direction * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        // プレイヤー自身やトリガーには当たらないようにする
        if (other.CompareTag("Player") || other.isTrigger) return;

        // ダメージ処理をBusterControllerのApplyDamageToEnemyと同じロジックで実行
        // ここでは簡単に敵のコンポーネントがあるかチェック
        ApplyBulletDamage(other);

        // 着弾エフェクト
        if (hitEffect != null)
        {
            Instantiate(hitEffect, transform.position, Quaternion.identity);
        }

        // 当たったら消える
        Destroy(gameObject);
    }

    private void ApplyBulletDamage(Collider col)
    {
        // BusterControllerのApplyDamageToEnemyと同様のダメージ処理
        // (VoxPartや敵のスクリプトをチェックしてダメージを与える)
        if (col.TryGetComponent<VoxPart>(out var part)) part.TakeDamage(damage);
        else if (col.TryGetComponent<VoxBodyPart>(out var body)) body.TakeDamage(damage);
        // 他の敵スクリプト(SoldierEnemy等)も必要に応じて追加
    }
}