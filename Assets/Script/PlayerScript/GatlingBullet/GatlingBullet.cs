using System.Collections;
using System.Collections.Generic; // Listを使うために追加
using UnityEngine;

public class GatlingBullet : MonoBehaviour
{
    public float speed = 100f;
    public float damage = 10f;
    public float lifeTime = 2f; // これが「消えるまでの時間」
    public GameObject hitEffect;

    private Vector3 direction;
    // 多重ヒット防止用：一度当たった対象を記録する
    private List<GameObject> _hitObjects = new List<GameObject>();

    public void Launch(Vector3 dir)
    {
        direction = dir.normalized;
        transform.rotation = Quaternion.LookRotation(direction);

        // 指定された寿命(lifeTime)が来たら自動で消滅する
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        transform.position += direction * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        // 1. プレイヤー自身や自分の弾、すでに当たったオブジェクトを無視
        if (other.CompareTag("Player") || other.CompareTag("PlayerBullet") ||
            other.gameObject.layer == LayerMask.NameToLayer("Player") ||
            other.gameObject.layer == LayerMask.NameToLayer("PlayerBullet") ||
            _hitObjects.Contains(other.gameObject)) // ★追加：多重ヒット防止
        {
            return;
        }

        // 2. 弾丸同士の衝突を無視
        if (other.GetComponent<GatlingBullet>() != null) return;

        // 3. ヒットリストに追加（これでこの弾はこの敵を「貫通」し、二度とダメージを与えない）
        _hitObjects.Add(other.gameObject);

        // 4. ダメージ判定
        bool isHit = ApplyBulletDamage(other);

        // 5. 消滅させずに貫通させるロジック
        if (isHit || !other.isTrigger)
        {
            // 当たった場所にエフェクトだけ出す
            if (hitEffect != null)
            {
                Instantiate(hitEffect, transform.position, Quaternion.identity);
            }

            // ★修正：ここでは Destroy を呼ばないことで「貫通」させる
            // 弾は Launch で設定した lifeTime が経過するまで飛び続けます
        }
    }

    private bool ApplyBulletDamage(Collider hitCollider)
    {
        GameObject target = hitCollider.gameObject;
        bool isHit = false;

        // ボスおよび各敵コンポーネントの判定（GetComponentInParentで親も探す）
        var boss = target.GetComponentInParent<ElsController>();
        if (boss != null)
        {
            boss.TakeDamage(damage);
            isHit = true;
        }
        else if (target.GetComponentInParent<SoldierSandbagEnemy>() is var sandbag && sandbag != null) { sandbag.TakeDamage(damage); isHit = true; }
        else if (target.GetComponentInParent<SoldierMoveEnemy>() is var s1 && s1 != null) { s1.TakeDamage(damage); isHit = true; }
        else if (target.GetComponentInParent<SoliderEnemy>() is var s2 && s2 != null) { s2.TakeDamage(damage); isHit = true; }
        else if (target.GetComponentInParent<TutorialEnemyController>() is var s3 && s3 != null) { s3.TakeDamage(damage); isHit = true; }
        else if (target.GetComponentInParent<ScorpionEnemy>() is var s4 && s4 != null) { s4.TakeDamage(damage); isHit = true; }
        else if (target.GetComponentInParent<SuicideEnemy>() is var s5 && s5 != null) { s5.TakeDamage(damage); isHit = true; }
        else if (target.GetComponentInParent<DroneEnemy>() is var s6 && s6 != null) { s6.TakeDamage(damage); isHit = true; }
        else if (target.GetComponentInParent<VoxBodyPart>() is var bodyPart && bodyPart != null) { bodyPart.TakeDamage(damage); isHit = true; }
        else if (target.GetComponentInParent<VoxPart>() is var part && part != null) { part.TakeDamage(damage); isHit = true; }

        if (isHit) Debug.Log($"Gatling pierced through: {target.name} - Damage: {damage}");
        return isHit;
    }
}