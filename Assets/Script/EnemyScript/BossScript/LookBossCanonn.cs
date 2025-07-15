using UnityEngine;

public class LookBossCanonn : MonoBehaviour
{
    public Transform target;                 // �^�[�Q�b�g
    public float rotationSpeed = 5f;
    public BossEnemyDead bossEnemyDaed;
    public BossShotBullet bossShotBullet;

    // Look����p
    public float disableLookDuration = 2.0f; // ���O�㉽�bLook���~�߂邩
    private float lookDisableTimer = 0f;     // �c���~����

    void Update()
    {
        if (bossEnemyDaed.BossDead) return;

        // Look�̈ꎞ��~���Ԃ��c���Ă���΁A���炷����
        if (lookDisableTimer > 0f)
        {
            lookDisableTimer -= Time.deltaTime;
            return; // ���̃t���[����Look���X�L�b�v
        }

        // �v���C���[�̕�������������
        Vector3 direction = target.position - transform.position;
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    // �e�������O�E����ɌĂяo��
    public void TemporarilyDisableLook()
    {
        lookDisableTimer = disableLookDuration;
    }

    public void OnTriggerExit(Collider collider)
    {
        if (collider.gameObject.tag == "Player")
        {
            // ���������ɖ߂�
            transform.rotation = Quaternion.identity;
        }
    }
}
