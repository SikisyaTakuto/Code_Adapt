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
    public float attackDamage = 10f; // �G�̍U����
    [Tooltip("�r�[���U���̃N�[���_�E�����ԁi�b�j�B")]
    public float beamAttackCooldown = 3.0f; // �r�[���U���̃N�[���_�E��
    [Tooltip("�r�[���U���̎˒������B")]
    public float beamAttackRange = 20f; // �r�[���U���̎˒�����
    [Tooltip("�r�[�������������ʒu��Transform�B")]
    public Transform beamSpawnPoint; // �r�[�����o��ꏊ (Raycast�̊J�n�_�Ƃ��Ă��g�p)
    [Tooltip("�r�[���̃G�t�F�N�g/Prefab�B")]
    public GameObject beamPrefab; // �r�[���̃G�t�F�N�g�i�I�v�V�����j
    [Tooltip("�r�[���G�t�F�N�g�̎������ԁB")]
    public float beamDuration = 0.5f; // �r�[���G�t�F�N�g�̕\������
    [Tooltip("�^�[�Q�b�g�ƂȂ�v���C���[�̃^�O�B")]
    public string playerTag = "Player"; // �v���C���[�I�u�W�F�N�g�̃^�O
    [Tooltip("�U�����J�n���Ă���r�[���𔭎˂���܂ł̏������ԁi�b�j�B���̊ԓG�͒�~����B")]
    public float attackPreparationTime = 1.0f; // �U���O�̒�~����
    [Tooltip("�r�[���̐��̑����B")] // ���ǉ�
    public float beamWidth = 0.5f; // ���ǉ�
    [Tooltip("�r�[���̐��̐F�i�J�n�F�j�B")] // ���ǉ�
    public Color beamStartColor = Color.red; // ���ǉ�
    [Tooltip("�r�[���̐��̐F�i�I���F�j�B")] // ���ǉ�
    public Color beamEndColor = Color.magenta; // ���ǉ�

    private bool canAttack = true; // �U���N�[���_�E���Ǘ��p
    private bool isAttacking = false; // �U�����t���O
    private GameObject currentBeamVisualizer; // ���ǉ�: ���ݕ\�����̃r�[���̎��o���I�u�W�F�N�g�ւ̎Q��

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

        // ���ǉ�: �G��HP��0�ɂȂ����Ƃ��Ƀr�[���G�t�F�N�g���������邽�߂̃C�x���g�w��
        if (enemyHealth != null)
        {
            enemyHealth.onDeath += HandleEnemyDeath;
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
        if (!isActivated || enemyHealth.currentHealth <= 0)
        {
            // HP��0�ȉ��ɂȂ����ꍇ�A�r�[���G�t�F�N�g�������I�ɏ���
            if (enemyHealth.currentHealth <= 0)
            {
                if (currentBeamVisualizer != null)
                {
                    Destroy(currentBeamVisualizer);
                    currentBeamVisualizer = null;
                }
            }
            return; // ��A�N�e�B�u�܂���HP��0�ȉ��Ȃ牽�����Ȃ�
        }

        // �U�����͈ړ����W�b�N�����s���Ȃ�
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
        isAttacking = true; // �U�����t���O�𗧂Ă�
        agent.isStopped = true; // �ړ����m���ɒ�~������

        // �U���O�̏������ԁi���b�Ԓ�~���镔���j
        Debug.Log("�U��������...");
        yield return new WaitForSeconds(attackPreparationTime); // �����Ő��b�Ԓ�~����

        // �G��HP��0�ȉ��̏ꍇ�A�r�[�����������ɏI��
        if (enemyHealth.currentHealth <= 0)
        {
            isAttacking = false;
            agent.isStopped = false;
            canAttack = true; // �N�[���_�E�������Z�b�g
            yield break;
        }

        Vector3 rayOrigin = beamSpawnPoint.position;
        Vector3 rayDirection = (playerTransform.position - rayOrigin).normalized; // �v���C���[�̕�����Ray���΂�
        Vector3 beamEndPoint = rayOrigin + rayDirection * beamAttackRange; // �r�[���̃f�t�H���g�I�_

        RaycastHit hit;
        PlayerHealth playerHealth = null; // �q�b�g����PlayerHealth��ێ�����ϐ�

        if (Physics.Raycast(rayOrigin, rayDirection, out hit, beamAttackRange))
        {
            Debug.Log($"Raycast���q�b�g���܂���: {hit.collider.name}, �^�O: {hit.collider.tag}");
            beamEndPoint = hit.point; // �q�b�g�����ʒu���r�[���̏I�_�Ƃ���

            if (hit.collider.CompareTag(playerTag))
            {
                playerHealth = hit.collider.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(attackDamage);
                    Debug.Log($"�G���v���C���[�� {attackDamage} �_���[�W��^���܂����B�iRaycast�j");
                }
                else
                {
                    Debug.LogWarning("Raycast���v���C���[�Ƀq�b�g���܂������APlayerHealth�R���|�[�l���g��������܂���B");
                }
            }
        }
        else
        {
            Debug.Log("Raycast�͉����q�b�g���܂���ł����B");
        }

        // �r�[���̎��o���iLine Renderer�j
        // �����̃r�W���A���C�U�[������Δj�����Ă���V��������
        if (currentBeamVisualizer != null)
        {
            Destroy(currentBeamVisualizer);
            currentBeamVisualizer = null;
        }
        currentBeamVisualizer = new GameObject("EnemyBeamVisualizer");
        LineRenderer lineRenderer = currentBeamVisualizer.AddComponent<LineRenderer>();
        lineRenderer.startWidth = beamWidth;
        lineRenderer.endWidth = beamWidth;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default")); // �W���̃V�F�[�_�[
        lineRenderer.startColor = beamStartColor;
        lineRenderer.endColor = beamEndColor;
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, rayOrigin);
        lineRenderer.SetPosition(1, beamEndPoint);

        // �r�[���G�t�F�N�g�̐����ƕ\���i�I�v�V�����j
        GameObject beamEffectInstance = null;
        if (beamPrefab != null)
        {
            beamEffectInstance = Instantiate(beamPrefab, rayOrigin, Quaternion.identity);
            beamEffectInstance.transform.LookAt(beamEndPoint); // �G�t�F�N�g���r�[���̕����Ɍ�����
            beamEffectInstance.transform.parent = currentBeamVisualizer.transform; // ���C�������_���[�̎q�ɂ���i�܂Ƃ߂Ĕj�����邽�߁j
        }

        // �w�莞�ԕ\��������A�j��
        yield return new WaitForSeconds(beamDuration);
        if (currentBeamVisualizer != null) // �R���[�`�����ɓG���|���ꂽ�ꍇ�ɔ�����null�`�F�b�N
        {
            Destroy(currentBeamVisualizer); // Line Renderer�Ǝq�I�u�W�F�N�g�i�G�t�F�N�g�j���܂Ƃ߂Ĕj��
            currentBeamVisualizer = null;
        }

        // �U���A�j���[�V������SE�Đ��Ȃǂ�����΂����ɒǉ�

        yield return new WaitForSeconds(beamAttackCooldown); // �N�[���_�E���ҋ@
        canAttack = true; // �N�[���_�E���I��
        isAttacking = false; // �U�����t���O�����Z�b�g
        agent.isStopped = false; // �U��������������ړ����ĊJ
    }

    /// <summary>
    /// �G��HP��0�ɂȂ����Ƃ��ɌĂяo�����n���h���B
    /// </summary>
    private void HandleEnemyDeath()
    {
        Debug.Log("�G���|����܂����I�r�[���G�t�F�N�g���������܂��B");
        if (currentBeamVisualizer != null)
        {
            Destroy(currentBeamVisualizer);
            currentBeamVisualizer = null;
        }
        // �G���|���ꂽ��ANavMeshAgent���~
        if (agent != null && agent.enabled)
        {
            agent.isStopped = true;
            agent.enabled = false; // �G�[�W�F���g�𖳌������Ĉړ������S�ɒ�~
        }
        // �K�v�ɉ����āA�G�̃��f�����\���ɂ���A�p�[�e�B�N���G�t�F�N�g���Đ�����Ȃǂ̏�����ǉ�
        // ��: gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        // ���ǉ�: �X�N���v�g���j�������Ƃ��ɃC�x���g�w�ǂ���������
        if (enemyHealth != null)
        {
            enemyHealth.onDeath -= HandleEnemyDeath;
        }
    }

    /// <summary>
    /// �G���A�N�e�B�u�ɂ��郁�\�b�h�i�`���[�g���A���X�N���v�g����Ăяo�����Ƃ�z��j
    /// </summary>
    public void ActivateEnemy(Vector3 centerPoint, float range)
    {
        isActivated = true;
        if (agent != null && !agent.enabled) // �G�[�W�F���g�������ɂȂ��Ă���ꍇ�A�L���ɂ���
        {
            agent.enabled = true;
        }
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

        // Raycast�̊J�n�ʒu�ƕ��������o��
        if (beamSpawnPoint != null && playerTransform != null)
        {
            Gizmos.color = Color.cyan;
            Vector3 rayOrigin = beamSpawnPoint.position;
            Vector3 rayDirection = (playerTransform.position - rayOrigin).normalized;
            Gizmos.DrawLine(rayOrigin, rayOrigin + rayDirection * beamAttackRange);
            Gizmos.DrawSphere(rayOrigin, 0.1f); // Ray�̎n�_�ɏ����ȋ���`��
        }
    }
}
