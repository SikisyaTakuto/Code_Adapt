using UnityEngine;
using System.Collections.Generic;

public class PlayerAttackController : MonoBehaviour
{
    public float detectRadius = 10f;          // 敵を検知する範囲
    public float attackRange = 2f;            // 攻撃できる距離
    public float moveSpeed = 5f;              // 移動速度
    public float attackCooldown = 1.5f;       // 攻撃のクールダウン（秒）

    private Transform targetEnemy = null;     // 現在ターゲットにしている敵
    private float attackTimer = 0f;

    void Update()
    {
        attackTimer -= Time.deltaTime;

        DetectClosestEnemy();

        if (targetEnemy != null)
        {
            float distance = Vector3.Distance(transform.position, targetEnemy.position);

            if (distance > attackRange)
            {
                // 敵に近づく
                Vector3 dir = (targetEnemy.position - transform.position).normalized;
                transform.position += dir * moveSpeed * Time.deltaTime;

                // 敵の方向を向く
                Vector3 lookDir = targetEnemy.position - transform.position;
                lookDir.y = 0; // 水平方向だけ回転
                if (lookDir != Vector3.zero)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), 0.1f);
            }
            else
            {
                // 攻撃可能なら攻撃
                if (attackTimer <= 0f)
                {
                    Attack();
                    attackTimer = attackCooldown;
                }
            }
        }
    }

    void DetectClosestEnemy()
    {
        // 半径detectRadius内のEnemyをすべて取得
        Collider[] hits = Physics.OverlapSphere(transform.position, detectRadius);

        float closestDist = Mathf.Infinity;
        Transform closestEnemy = null;

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                float dist = Vector3.Distance(transform.position, hit.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestEnemy = hit.transform;
                }
            }
        }

        targetEnemy = closestEnemy;
    }

    void Attack()
    {
        Debug.Log("近接攻撃！");
        // TODO: 敵にダメージを与える処理をここに追加する
    }

    // Gizmosで検知範囲を視覚化
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectRadius);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
