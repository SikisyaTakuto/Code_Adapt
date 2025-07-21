using UnityEngine;
using UnityEngine.AI; // NavMeshAgent���g�p���邽�߂ɕK�v
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))] // NavMeshAgent�R���|�[�l���g���K�{�ł��邱�Ƃ�����
[RequireComponent(typeof(EnemyHealth))] // EnemyHealth�R���|�[�l���g���K�{�ł��邱�Ƃ�����
public class EnemyController : MonoBehaviour
{
    [Header("Health Settings")]
    [Tooltip("�G�̍ő�HP�BEnemyHealth�X�N���v�g�Őݒ肳��܂����A�O�̂��߂�����ł��m�F�ł��܂��B")]
    public float maxHealth = 30f; // EnemyHealth�Ɠ��������邩�AEnemyHealth�ōŏI�ݒ肷��
    private EnemyHealth enemyHealth; // EnemyHealth�X�N���v�g�ւ̎Q��

    [Header("Attack Settings")]
    [Tooltip("�G�̍U���́B�v���C���[�ɗ^����_���[�W�B")]
    public float attackDamage = 100f; // �G�̍U����
    [Tooltip("�r�[���U���̃N�[���_�E�����ԁi�b�j�B")]
    public float beamAttackCooldown = 3.0f; // �r�[���U���̃N�[���_�E��
    [Tooltip("�r�[���U���̎˒������B")]
    public float beamAttackRange = 20f; // �r�[���U���̎˒�����
    [Tooltip("�r�[�������������ʒu��Transform�B")]
    public Transform beamSpawnPoint; // �r�[�����o��ꏊ
    [Tooltip("�r�[���̃G�t�F�N�g/Prefab�B")]
    public GameObject beamPrefab; // �r�[���̃G�t�F�N�g�i�I�v�V�����j
    [Tooltip("�r�[���G�t�F�N�g�̎������ԁB")]
    public float beamDuration = 0.5f; // �r�[���G�t�F�N�g�̕\������
    [Tooltip("�^�[�Q�b�g�ƂȂ�v���C���[�̃^�O�B")]
    public string playerTag = "Player"; // �v���C���[�I�u�W�F�N�g�̃^�O
    [Tooltip("�U�����J�n���Ă���r�[���𔭎˂���܂ł̏������ԁi�b�j�B���̊ԓG�͒�~����B")]
    public float attackPreparationTime = 1.0f; // ���ǉ��F�U���O�̒�~����

    private bool canAttack = true; // �U���N�[���_�E���Ǘ��p
    private bool isAttacking = false; // ���ǉ��F�U�����t���O

    [Header("Movement Settings")]
    [Tooltip("�����_���ړ��̊�ƂȂ钆�S�_�B")]
    public Vector3 walkPointCenter; // �����_���ړ��̒��S�_
    [Tooltip("�����_���ړ��͈̔́i���a�j�B")]
    public float walkPointRange = 10f; // �����_���ړ��͈̔�
    [Tooltip("NavMeshAgent�̈ړ����x�B")]
    public float moveSpeed = 3.5f; // �ړ����x
    [Tooltip("�ړI�n�ɓ��B�����ƌ��Ȃ������B")]
    public float destinationThreshold = 1.0f; // �ړI�n�ɓ��B�����ƌ��Ȃ�����

    private NavMeshAgent agent; // NavMeshAgent�R���|�[�l���g
    private Vector3 currentDestination; // ���݂̖ړI�n

    [Header("State Settings")]
    [Tooltip("�G���A�N�e�B�u���ǂ����B")]
    public bool isActivated = false; // �G���s�����J�n���邩�ǂ���

    private Transform playerTransform; // �v���C���[��Transform

    void Awake()
    {
        enemyHealth = GetComponent<EnemyHealth>();
        agent = GetComponent<NavMeshAgent>();

        // NavMeshAgent�̑��x��ݒ�
        agent.speed = moveSpeed;

        // EnemyHealth��maxHealth�����̃X�N���v�g�̐ݒ�ŏ㏑���i�܂��͓����j
        enemyHealth.maxHealth = maxHealth;
        // EnemyHealth��currentHealth��Awake�ŏ����������̂ł����ł͕s�v

        // �v���C���[�I�u�W�F�N�g���^�O�Ō���
        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObject != null)
        {
            playerTransform = playerObject.transform;
        }
        else
        {
            Debug.LogWarning($"�v���C���[�I�u�W�F�N�g�Ƀ^�O '{playerTag}' ��������܂���B�G�͍U���s�����s���܂���B");
        }
    }

    void Start()
    {
        // �Q�[���J�n���ɍŏ��̖ړI�n��ݒ�
        SetNewRandomDestination();
        if (!isActivated)
        {
            // ��A�N�e�B�u�Ȃ�ړ����~
            agent.isStopped = true;
        }
    }

    void Update()
    {
        if (!isActivated || enemyHealth.currentHealth <= 0) return; // ��A�N�e�B�u�܂���HP��0�ȉ��Ȃ牽�����Ȃ�

        // ���ύX�_: �U�����͈ړ����W�b�N�����s���Ȃ�
        if (isAttacking)
        {
            // �U�����̓v���C���[�̕�������������
            if (playerTransform != null)
            {
                Vector3 lookAtPlayer = playerTransform.position;
                lookAtPlayer.y = transform.position.y; // Y���͌Œ肵�Đ�������������]
                transform.LookAt(lookAtPlayer);
            }
            return; // �U�����͂���ȍ~��Update�������X�L�b�v
        }

        // �v���C���[���˒��������ɂ��邩�`�F�b�N
        if (playerTransform != null && Vector3.Distance(transform.position, playerTransform.position) <= beamAttackRange)
        {
            // �v���C���[�̕�������
            Vector3 lookAtPlayer = playerTransform.position;
            lookAtPlayer.y = transform.position.y; // Y���͌Œ肵�Đ�������������]
            transform.LookAt(lookAtPlayer);

            // �U���N�[���_�E�����ł͂Ȃ���
            if (canAttack)
            {
                agent.isStopped = true; // �U�����͈ړ����~
                StartCoroutine(BeamAttackRoutine());
            }
        }
        else
        {
            agent.isStopped = false; // �v���C���[���˒��O�Ȃ�ړ����ĊJ
            // �ړI�n�ɓ��B�������A�܂��ړI�n���ݒ肳��Ă��Ȃ��ꍇ
            if (!agent.pathPending && agent.remainingDistance < destinationThreshold)
            {
                SetNewRandomDestination();
            }
        }
    }

    /// <summary>
    /// �����_���ȖړI�n�𐶐����ANavMeshAgent�ɐݒ肷��
    /// </summary>
    void SetNewRandomDestination()
    {
        // �����_���ȕ����𐶐�
        Vector3 randomDirection = Random.insideUnitSphere * walkPointRange;
        randomDirection += walkPointCenter; // ���S�_����ɂ���

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, walkPointRange, NavMesh.AllAreas))
        {
            currentDestination = hit.position;
            agent.SetDestination(currentDestination);
            Debug.Log($"�V�����ړI�n��ݒ�: {currentDestination}");
        }
        else
        {
            // �L����NavMesh��̈ʒu��������Ȃ��ꍇ�A�Ď��s
            Debug.LogWarning("NavMesh��ŗL���ȃ����_���Ȉʒu��������܂���ł����B�Ď��s���܂��B");
            // �����҂��Ă���Ď��s���邩�A�ʂ̃��W�b�N������
            // ����͎��̃t���[���ōēxUpdate���Ă΂��̂ŁA�����ōĎ��s����邱�Ƃ�����
        }
    }

    /// <summary>
    /// �r�[���U���̃R���[�`��
    /// </summary>
    IEnumerator BeamAttackRoutine()
    {
        canAttack = false; // �U�����J�n������N�[���_�E��
        isAttacking = true; // ���ǉ��F�U�����t���O�𗧂Ă�
        agent.isStopped = true; // �ړ����m���ɒ�~������

        // �U���O�̏������ԁi���b�Ԓ�~���镔���j
        Debug.Log("�U��������...");
        yield return new WaitForSeconds(attackPreparationTime); // �����Ő��b�Ԓ�~����

        // �r�[���G�t�F�N�g�̐����ƕ\��
        if (beamPrefab != null && beamSpawnPoint != null)
        {
            GameObject beamInstance = Instantiate(beamPrefab, beamSpawnPoint.position, beamSpawnPoint.rotation);
            // �r�[���������L�΂��ăv���C���[�ɓ͂��悤�ɂ��邱�Ƃ��\�i��: transform.forward * beamAttackRange�j
            // Instantiate��A�K�v�ł���΃r�[���̃X�P�[��������𒲐�
            Destroy(beamInstance, beamDuration); // ��莞�Ԍ�Ƀr�[���G�t�F�N�g��j��
            Debug.Log("�r�[�����ˁI");
        }

        // �v���C���[�ւ̃_���[�W����
        if (playerTransform != null)
        {
            PlayerHealth playerHealth = playerTransform.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage);
                Debug.Log($"�G���v���C���[�� {attackDamage} �_���[�W��^���܂����B");
            }
            else
            {
                Debug.LogWarning("�v���C���[��PlayerHealth�R���|�[�l���g��������܂���B");
            }
        }

        // �U���A�j���[�V������SE�Đ��Ȃǂ�����΂����ɒǉ�

        yield return new WaitForSeconds(beamAttackCooldown); // �N�[���_�E���ҋ@
        canAttack = true; // �N�[���_�E���I��
        isAttacking = false; // ���ǉ��F�U�����t���O�����Z�b�g
        agent.isStopped = false; // �U��������������ړ����ĊJ
    }

    /// <summary>
    /// �G���A�N�e�B�u�ɂ��郁�\�b�h�i�`���[�g���A���X�N���v�g����Ăяo�����Ƃ�z��j
    /// </summary>
    public void ActivateEnemy(Vector3 centerPoint, float range)
    {
        isActivated = true;
        agent.isStopped = false; // �ړ����ĊJ
        walkPointCenter = centerPoint; // �`���[�g���A���Őݒ肳�ꂽ�͈͂��g�p
        walkPointRange = range;
        SetNewRandomDestination(); // �A�N�e�B�u�ɂȂ����炷���ɍŏ��̖ړI�n��ݒ�
        Debug.Log("�G���A�N�e�B�u�ɂȂ�܂����B");
    }

    // �f�o�b�O�\���p�i�V�[���r���[�ł̂ݕ\���j
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        // �����_���ړ��͈̔͂����̂ŕ\��
        Gizmos.DrawWireSphere(walkPointCenter, walkPointRange);

        Gizmos.color = Color.red;
        // �r�[���U���̎˒����������̂ŕ\��
        if (playerTransform != null)
        {
            Gizmos.DrawWireSphere(transform.position, beamAttackRange);
        }
    }
}
