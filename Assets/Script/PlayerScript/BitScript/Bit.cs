using UnityEngine;

public class Bit : MonoBehaviour
{
    private Transform target; // �^�[�Q�b�g�ƂȂ�G
    private float moveSpeed;  // �r�b�g�̈ړ����x

    // �㏸�O���̂��߂̒ǉ��ϐ�
    private Vector3 initialLaunchPosition; // �ŏ���Instantiate���ꂽ�ʒu (Player��SpawnPoint)
    private Vector3 peakLaunchPosition;    // �㏸�O���̃s�[�N�n�_
    private float currentLaunchTime;       // �㏸���̌o�ߎ���
    private float totalLaunchDuration;     // �㏸�ɂ����鍇�v����
    private float arcHeight;               // �㏸�O���̃A�[�`�̍���

    private bool isLaunching = true;       // �㏸�����ǂ����̃t���O

    // Bit���������ꂽ����Player�ɏՓ˂��Ȃ��悤�ɂ��鏈�� (��: Player���C���[�Ƃ̏Փ˂𖳎�)
    public LayerMask playerLayer; // Player�̃��C���[ (Inspector�Őݒ�)
    private LayerMask enemyLayerInternal; // Bit�X�N���v�g���Ŏg�p����EnemyLayer

    public float destroyOnHitDelay = 0.1f; // �G��j�󂵂���A�r�b�g�����ł���܂ł̒Z���x��

    void Awake()
    {
        // Bit���g�̃��C���[��Player���C���[�̏Փ˂𖳎�����
        // Bit��GameObject��Bit���C���[���ݒ肳��Ă��邱�Ƃ�O��Ƃ��܂��B

        // PlayerLayer���L���ȃ��C���[�͈͓��ɂ��邩�`�F�b�N
        // (1 << playerLayer) �̌`��Physics.IgnoreLayerCollision�ɓn�����߁A
        // playerLayer���̂̓��C���[�ԍ��i0-31�j�ł���K�v������܂��B
        // Inspector��LayerMask�Ƃ��Đݒ肳��Ă���ꍇ�A���̓����l�͒P��̃��C���[���������Ƃ�����܂��B
        // ����LayerMask�������̃��C���[���܂ޏꍇ�́A���������C���[�ԍ����擾����K�v������܂��B
        // �ʏ�͒P��̃��C���[���w�肷��̂ŁA�����ł�playerLayer.value���烌�C���[�ԍ����擾���܂��B

        // playerLayer���P��̃��C���[�Ƃ��Đݒ肳��Ă��邱�Ƃ�z��
        int playerLayerNumber = -1;
        // LayerMask���烌�C���[�ԍ����擾����
        for (int i = 0; i < 32; i++)
        {
            if (((1 << i) & playerLayer.value) != 0)
            {
                playerLayerNumber = i;
                break;
            }
        }

        if (playerLayerNumber >= 0 && playerLayerNumber <= 31)
        {
            Physics.IgnoreLayerCollision(gameObject.layer, playerLayerNumber, true);
        }
        else
        {
            Debug.LogWarning("Bit: ������Player Layer���ݒ肳��Ă��邽�߁A�Փ˖����ݒ���X�L�b�v���܂����BPlayer Layer��0����31�͈̔͂ł��邱�Ƃ��m�F���Ă��������B");
        }
    }

    /// <summary>
    /// �r�b�g�����������A�㏸�O�����J�n����
    /// </summary>
    /// <param name="playerSpawnPos">�r�b�g�̏������[���h���W</param>
    /// <param name="newTarget">�^�[�Q�b�g�ƂȂ�G</param>
    /// <param name="launchHeight">�㏸����</param>
    /// <param name="launchDuration">�㏸����</param>
    /// <param name="speed">�^�[�Q�b�g�֌��������x</param>
    /// <param name="arc">�㏸�̃A�[�`�̍���</param>
    /// <param name="eLayer">�G�̃��C���[</param>
    public void InitializeBit(Vector3 initialSpawnPos, Transform newTarget, float launchHeight, float launchDuration, float speed, float arc, LayerMask eLayer)
    {
        initialLaunchPosition = initialSpawnPos;
        // �s�[�N�ʒu�́A�����ʒu����^��Ɏw��̍����܂�
        peakLaunchPosition = initialLaunchPosition + Vector3.up * launchHeight;
        target = newTarget;
        moveSpeed = speed;
        totalLaunchDuration = launchDuration;
        arcHeight = arc;
        currentLaunchTime = 0f;
        isLaunching = true;
        enemyLayerInternal = eLayer; // EnemyLayer������ϐ��ɕۑ�

        // Bit�̏����ʒu��InitializeBit�̈����Ŏ󂯎�����ʒu�ɐݒ�
        transform.position = initialLaunchPosition;

        // Bit�̌������^�[�Q�b�g�Ɍ�����i�����i�K�Łj
        if (target != null)
        {
            transform.LookAt(target.position);
        }
    }

    void Update()
    {
        if (target == null)
        {
            // �^�[�Q�b�g�����ł����ꍇ�ȂǁA��莞�Ԍ�Ɏ��g�����ł�����
            Destroy(gameObject, 1.0f); // �^�[�Q�b�g�r�����͂����ɏ���
            return;
        }

        if (isLaunching)
        {
            currentLaunchTime += Time.deltaTime;
            float t = currentLaunchTime / totalLaunchDuration;

            if (t < 1.0f)
            {
                // 3�_�Ԃ̃x�W�F�Ȑ� (������)
                Vector3 p0 = initialLaunchPosition;
                // �s�[�N�ʒu�́A�����ʒu�ƃ^�[�Q�b�g�̒��ԓ_�ɃA�[�`�̍�����������
                // ����ɂ��A�r�b�g���v���C���[�̌�납��㏸���A�^�[�Q�b�g�̕����֌ʂ�`��
                Vector3 midPoint = Vector3.Lerp(initialLaunchPosition, target.position, 0.5f);
                Vector3 p1 = new Vector3(midPoint.x, Mathf.Max(initialLaunchPosition.y, target.position.y) + arcHeight, midPoint.z);

                transform.position = Mathf.Pow(1 - t, 2) * p0 + 2 * (1 - t) * t * p1 + Mathf.Pow(t, 2) * target.position;

                // �㏸���̓^�[�Q�b�g�̕�������
                transform.LookAt(target.position);
            }
            else
            {
                isLaunching = false; // �㏸����
            }
        }
        else // �㏸������A�^�[�Q�b�g�Ɍ������Ĉړ�
        {
            // �^�[�Q�b�g�Ɍ������Ĉړ�
            transform.position = Vector3.MoveTowards(transform.position, target.position, moveSpeed * Time.deltaTime);

            // �^�[�Q�b�g�̕�������ɒǏ] (���̐悪�G�Ɍ������悤��)
            transform.LookAt(target.position);
        }
    }

    // �G�ɓ��������ꍇ�̏���
    void OnTriggerEnter(Collider other)
    {
        // �Փ˂����̂��G���C���[�̃I�u�W�F�N�g���`�F�b�N
        // enemyLayerInternal ���g�p
        if (((1 << other.gameObject.layer) & enemyLayerInternal) != 0)
        {
            EnemyHealth enemyHealth = other.GetComponent<EnemyHealth>();
            if (enemyHealth == null)
            {
                enemyHealth = other.GetComponentInParent<EnemyHealth>();
            }

            if (enemyHealth != null)
            {
                Debug.Log($"Bit hit and destroying {other.name}!");
                enemyHealth.TakeDamage(100); // ��: 100�_���[�W��^����i�����z��j
                Destroy(gameObject, destroyOnHitDelay); // �r�b�g���g�͏����x��ď���
            }
        }
    }
}