using UnityEngine;

public class GatlingBullet : MonoBehaviour
{
    public float speed = 100f;        // 弾速
    public float damage = 400f;       // 1発あたりのダメージ
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

    private void ApplyBulletDamage(Collider hitCollider)
    {
        GameObject target = hitCollider.gameObject;
        bool isHit = false;

        // --- 指定された敵判定ロジックの統合 ---
        if (target.TryGetComponent<SoldierMoveEnemy>(out var s1)) { s1.TakeDamage(damage); isHit = true; }
        else if (target.TryGetComponent<SoliderEnemy>(out var s2)) { s2.TakeDamage(damage); isHit = true; }
        else if (target.TryGetComponent<TutorialEnemyController>(out var s3)) { s3.TakeDamage(damage); isHit = true; }
        else if (target.TryGetComponent<ScorpionEnemy>(out var s4)) { s4.TakeDamage(damage); isHit = true; }
        else if (target.TryGetComponent<SuicideEnemy>(out var s5)) { s5.TakeDamage(damage); isHit = true; }
        else if (target.TryGetComponent<DroneEnemy>(out var s6)) { s6.TakeDamage(damage); isHit = true; }
        // 本体のパーツ（胴体など）
        else if (target.TryGetComponent<VoxBodyPart>(out var bodyPart))
        {
            bodyPart.TakeDamage(damage);
            isHit = true;
        }
        // ボスのパーツ（アームなど）
        else if (target.TryGetComponent<VoxPart>(out var part))
        {
            part.TakeDamage(damage);
            isHit = true;
        }

        // ダメージが入った場合のみエフェクトを生成
        if (isHit)
        {
            Debug.Log($"Gatling hit: {target.name} - Damage: {damage}");
            if (hitEffect != null)
            {
                Instantiate(hitEffect, hitCollider.bounds.center, Quaternion.identity);
            }
        }
    }
}