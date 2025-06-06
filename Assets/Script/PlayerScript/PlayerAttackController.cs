using UnityEngine;
using System.Collections.Generic;

public class PlayerAttackController : MonoBehaviour
{
    public float detectRadius = 10f;          // �G�����m����͈�
    public float attackRange = 2f;            // �U���ł��鋗��
    public float moveSpeed = 5f;              // �ړ����x
    public float attackCooldown = 1.5f;       // �U���̃N�[���_�E���i�b�j

    private Transform targetEnemy = null;     // ���݃^�[�Q�b�g�ɂ��Ă���G
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
                // �G�ɋ߂Â�
                Vector3 dir = (targetEnemy.position - transform.position).normalized;
                transform.position += dir * moveSpeed * Time.deltaTime;

                // �G�̕���������
                Vector3 lookDir = targetEnemy.position - transform.position;
                lookDir.y = 0; // ��������������]
                if (lookDir != Vector3.zero)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), 0.1f);
            }
            else
            {
                // �U���\�Ȃ�U��
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
        // ���adetectRadius����Enemy�����ׂĎ擾
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
        Debug.Log("�ߐڍU���I");
        // TODO: �G�Ƀ_���[�W��^���鏈���������ɒǉ�����
    }

    // Gizmos�Ō��m�͈͂����o��
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectRadius);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
