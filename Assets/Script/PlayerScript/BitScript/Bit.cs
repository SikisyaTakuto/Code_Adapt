using UnityEngine;
using System.Collections;

public class Bit : MonoBehaviour
{
    private Transform target;
    private Vector3 initialSpawnPosition;
    private float launchHeight;
    private float launchDuration;
    private float attackSpeed;
    private float arcHeight;
    private LayerMask enemyLayer;
    private float damageAmount; // �r�b�g���^����_���[�W��

    private float launchTimer = 0f;
    private bool isLaunching = true;
    private bool hasDealtDamage = false; // �_���[�W����x�^�������ǂ�����ǐ�

    /// <summary>
    /// �r�b�g�����������A�^�[�Q�b�g�Ɍ������Ĕ��˂��鏀��������
    /// </summary>
    /// <param name="spawnPos">�����X�|�[���ʒu</param>
    /// <param name="targetTransform">�^�[�Q�b�g��Transform</param>
    /// <param name="height">�㏸���鍂��</param>
    /// <param name="duration">�㏸�ɂ����鎞��</param>
    /// <param name="speed">�^�[�Q�b�g�ւ̍U�����x</param>
    /// <param name="arc">�㏸�O���̃A�[�`�̍���</param>
    /// <param name="layer">�G�̃��C���[�}�X�N</param>
    /// <param name="damage">�r�b�g���^����_���[�W</param>
    public void InitializeBit(Vector3 spawnPos, Transform targetTransform, float height, float duration, float speed, float arc, LayerMask layer, float damage)
    {
        initialSpawnPosition = spawnPos;
        target = targetTransform;
        launchHeight = height;
        launchDuration = duration;
        attackSpeed = speed;
        arcHeight = arc;
        enemyLayer = layer;
        damageAmount = damage; // �_���[�W�ʂ�ݒ�

        StartCoroutine(LaunchAndAttack());
    }

    IEnumerator LaunchAndAttack()
    {
        // �㏸�t�F�[�Y
        while (launchTimer < launchDuration)
        {
            float t = launchTimer / launchDuration;
            Vector3 currentPos = Vector3.Lerp(initialSpawnPosition, initialSpawnPosition + Vector3.up * launchHeight, t);
            // �A�[�`��`�����߂̒ǉ���Y�I�t�Z�b�g
            currentPos.y += Mathf.Sin(t * Mathf.PI) * arcHeight;
            transform.position = currentPos;
            launchTimer += Time.deltaTime;
            yield return null;
        }

        isLaunching = false;
        // �^�[�Q�b�g�Ɍ������čU���t�F�[�Y
        while (target != null && target.gameObject.activeInHierarchy && !hasDealtDamage) // �_���[�W��^�����烋�[�v�𔲂���
        {
            // �^�[�Q�b�g���L���ȏꍇ�̂ݒǐ�
            Vector3 direction = (target.position - transform.position).normalized;
            transform.position += direction * attackSpeed * Time.deltaTime;

            // �^�[�Q�b�g�̕���������
            transform.LookAt(target);

            // �^�[�Q�b�g�ɏ\���ɋ߂Â������A�܂��͏Փ˂��������`�F�b�N
            // �����ł͊ȈՓI�ɋ����Ŕ���
            if (Vector3.Distance(transform.position, target.position) < 1.0f) // �K�؂ȏՓˋ�����ݒ�
            {
                DealDamageToTarget(); // �^�[�Q�b�g�Ƀ_���[�W��^����
                break; // �U������
            }
            yield return null;
        }

        // �^�[�Q�b�g��null�ɂȂ�����A�|���ꂽ��A�_���[�W��^���I������肵���ꍇ�A�r�b�g��j��
        Destroy(gameObject);
    }

    /// <summary>
    /// �^�[�Q�b�g�Ƀ_���[�W��^����
    /// </summary>
    private void DealDamageToTarget()
    {
        if (target != null && !hasDealtDamage) // �_���[�W���܂��^���Ă��Ȃ��ꍇ�̂ݎ��s
        {
            EnemyHealth enemyHealth = target.GetComponent<EnemyHealth>();
            if (enemyHealth == null)
            {
                enemyHealth = target.GetComponentInParent<EnemyHealth>();
            }

            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(damageAmount);
                Debug.Log($"Bit hit {target.name} for {damageAmount} damage.");
                hasDealtDamage = true; // �_���[�W��^�����t���O�𗧂Ă�
            }
        }
    }

    // �Փ˔��� (��萳�m�ȃ_���[�W�����̂���)
    void OnTriggerEnter(Collider other)
    {
        // ���Ƀ_���[�W��^���Ă��邩�A�㏸���̏ꍇ�͏������Ȃ�
        if (hasDealtDamage || isLaunching) return;

        // �G���C���[�̃I�u�W�F�N�g�ɏՓ˂����ꍇ
        if (((1 << other.gameObject.layer) & enemyLayer) != 0)
        {
            // �Փ˂����̂��^�[�Q�b�g���g�A�܂��̓^�[�Q�b�g�̎q�I�u�W�F�N�g�ł��邩�m�F
            // �i�r�b�g�������̓G�ɓ�����\�����l������ꍇ�A���̃��W�b�N�͒������K�v�j
            // ����́AInitializeBit�Őݒ肳�ꂽ�P��̃^�[�Q�b�g�ɂ̂݃_���[�W��^����z��
            if (other.transform == target || other.transform.IsChildOf(target))
            {
                DealDamageToTarget();
                Destroy(gameObject); // �_���[�W��^������r�b�g�͏���
            }
        }
    }
}
