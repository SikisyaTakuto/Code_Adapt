using UnityEngine;

public class GatlingBullet : MonoBehaviour
{
    public float speed = 100f;
    public float damage = 10f;
    public float lifeTime = 2f;
    public GameObject hitEffect;

    private Vector3 direction;

    public void Launch(Vector3 dir)
    {
        direction = dir.normalized;
        transform.rotation = Quaternion.LookRotation(direction);
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        transform.position += direction * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        // 1. プレイヤー(自分)と、プレイヤーの弾(PlayerBullet)を無視
        if (other.CompareTag("Player") || other.CompareTag("PlayerBullet") ||
            other.gameObject.layer == LayerMask.NameToLayer("Player") ||
            other.gameObject.layer == LayerMask.NameToLayer("PlayerBullet"))
        {
            return;
        }

        // 2. 弾丸同士の衝突をスクリプトチェックでも念のため無視
        if (other.GetComponent<GatlingBullet>() != null) return;

        // 3. デバッグログ（対象外の時だけ）
        Debug.Log($"[Bullet Hit Debug] Name: {other.name} | IsTrigger: {other.isTrigger}");

        // 4. ダメージ判定
        bool isHit = ApplyBulletDamage(other);

        // 5. 消滅ロジック
        // 敵にヒットした、または「トリガーではない実体（壁・床）」に当たった場合のみ消える
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

        // ボスおよび各敵コンポーネントの判定
        var boss = target.GetComponentInParent<ElsController>();
        if (boss != null)
        {
            boss.TakeDamage(damage);
            isHit = true;
        }
        else if (target.GetComponentInParent<SoldierMoveEnemy>() is var s1 && s1 != null) { s1.TakeDamage(damage); isHit = true; }
        else if (target.GetComponentInParent<SoliderEnemy>() is var s2 && s2 != null) { s2.TakeDamage(damage); isHit = true; }
        else if (target.GetComponentInParent<TutorialEnemyController>() is var s3 && s3 != null) { s3.TakeDamage(damage); isHit = true; }
        else if (target.GetComponentInParent<ScorpionEnemy>() is var s4 && s4 != null) { s4.TakeDamage(damage); isHit = true; }
        else if (target.GetComponentInParent<SuicideEnemy>() is var s5 && s5 != null) { s5.TakeDamage(damage); isHit = true; }
        else if (target.GetComponentInParent<DroneEnemy>() is var s6 && s6 != null) { s6.TakeDamage(damage); isHit = true; }
        else if (target.GetComponentInParent<VoxBodyPart>() is var bodyPart && bodyPart != null) { bodyPart.TakeDamage(damage); isHit = true; }
        else if (target.GetComponentInParent<VoxPart>() is var part && part != null) { part.TakeDamage(damage); isHit = true; }

        if (isHit) Debug.Log($"Gatling hit: {target.name} - Damage: {damage}");
        return isHit;
    }
}