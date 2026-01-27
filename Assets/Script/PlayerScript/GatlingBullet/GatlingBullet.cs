using UnityEngine;

public class GatlingBullet : MonoBehaviour
{
    public float speed = 100f;        // 弾速
    public float damage = 10f;       // 1発あたりのダメージ
    public float lifeTime = 2f;      // 自然消滅までの時間
    public GameObject hitEffect;     // 着弾エフェクト

    private Vector3 direction;
    private float spawnTime;

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
        // 1. プレイヤー（自分自身）の全パーツ・全トリガーを最優先で無視
        if (other.CompareTag("Player") || other.name == "Player" || other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            return; // ログすら出さずに即終了
        }

        // 2. デバッグログ（プレイヤー以外に当たった時だけ出す）
        Debug.Log($"[Bullet Hit Debug] Name: {other.name} | IsTrigger: {other.isTrigger}");

        // 3. 弾丸同士の衝突を無視
        if (other.GetComponent<GatlingBullet>() != null) return;

        // 4. ダメージ判定
        bool isHit = ApplyBulletDamage(other);

        // 5. 消滅ロジック
        // 敵にヒットした、または「トリガーではない物理的な壁や地面」に当たった場合のみ消える
        if (isHit || !other.isTrigger)
        {
            if (hitEffect != null)
            {
                Instantiate(hitEffect, transform.position, Quaternion.identity);
            }
            Destroy(gameObject);
        }
    }

    private bool ApplyBulletDamage(Collider hitCollider)
    {
        GameObject target = hitCollider.gameObject;
        bool isHit = false;

        // --- 1. ElsController (ボス) への判定を追加 ---
        // 子オブジェクト（腕や足など）のコライダーに当たっても親の本体を探せるように GetComponentInParent を使います
        var boss = target.GetComponentInParent<ElsController>();
        if (boss != null)
        {
            boss.TakeDamage(damage);
            isHit = true;
        }
        // --- 2. その他の敵への判定 (else if で繋ぐ) ---
        else if (target.GetComponentInParent<SoldierMoveEnemy>() is var s1 && s1 != null) { s1.TakeDamage(damage); isHit = true; }
        else if (target.GetComponentInParent<SoliderEnemy>() is var s2 && s2 != null) { s2.TakeDamage(damage); isHit = true; }
        else if (target.GetComponentInParent<TutorialEnemyController>() is var s3 && s3 != null) { s3.TakeDamage(damage); isHit = true; }
        else if (target.GetComponentInParent<ScorpionEnemy>() is var s4 && s4 != null) { s4.TakeDamage(damage); isHit = true; }
        else if (target.GetComponentInParent<SuicideEnemy>() is var s5 && s5 != null) { s5.TakeDamage(damage); isHit = true; }
        else if (target.GetComponentInParent<DroneEnemy>() is var s6 && s6 != null) { s6.TakeDamage(damage); isHit = true; }
        else if (target.GetComponentInParent<VoxBodyPart>() is var bodyPart && bodyPart != null) { bodyPart.TakeDamage(damage); isHit = true; }
        else if (target.GetComponentInParent<VoxPart>() is var part && part != null) { part.TakeDamage(damage); isHit = true; }

        if (isHit)
        {
            Debug.Log($"Gatling hit: {target.name} - Damage: {damage}");
        }

        return isHit;
    }
}