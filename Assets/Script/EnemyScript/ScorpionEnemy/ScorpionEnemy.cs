using UnityEngine;
using UnityEngine.AI;
using System.Collections; // �R���[�`�����g�p���邽�ߕK�v

public class ScorpionEnemy : MonoBehaviour
{
    // --- HP�ݒ� ---
    [Header("�w���X�ݒ�")]
    public float maxHealth = 100f; // �ő�HP
    private float currentHealth;   // ���݂�HP
    private bool isDead = false;   // ���S�t���O

    // �V�K�ǉ�: �����G�t�F�N�g��Prefab
    [Header("�G�t�F�N�g�ݒ�")]
    public GameObject explosionPrefab;

    // ���S�A�j���[�V�������� (Inspector�Őݒ�)
    [Header("�A�j���[�V�����ݒ�")]
    public float deathAnimationDuration = 3.0f;

    // --- ���J�p�����[�^ ---
    [Header("�^�[�Q�b�g�ݒ�")]
    public Transform playerTarget;             // Player��Transform�������ɐݒ�
    public float detectionRange = 15f;         // Player�����o����͈�
    public Transform beamOrigin;               // �r�[���̔��ˌ��ƂȂ�Transform (�T�\���̔��̐�Ȃ�)

    [Range(0, 180)] // ����p�iDegree�j
    public float attackAngle = 30f;            // �U���\�Ȑ��ʎ���p�i�S�p�j

    [Header("�U���ݒ�")]
    public float attackRate = 1f;              // 1�b�ԂɍU������� 
    public GameObject beamPrefab;              // ���˂���r�[����Prefab
    public float beamSpeed = 30f;              // �r�[���̑��x

    // ? �C��: �ǂ̃^�O�������Œ�` (�U�������ɂ��g�p)
    private const string WALL_TAG = "Wall";

    [Header("�d���ݒ�")]
    public float hardStopDuration = 2f;        // �U����̍d�����ԁi�b�j

    [Header("�ړ��ݒ�")]
    public float rotationSpeed = 5f;             // Player�ǐՎ��̉�]���x
    public float wanderRadius = 10f;             // �����_���ړ��̍ő唼�a
    public float destinationThreshold = 1.5f;    // �ړI�n���B�ƌ��Ȃ�����
    public float maxIdleTime = 5f;             // �V�����ړI�n��ݒ肷��܂ł̍ő�Î~���ԁi�b�j

    // ?? �V�K�ǉ�: �ǉ���̂��߂̐ݒ�
    [Header("�Փˉ��ݒ� (NavMesh�p)")]
    public float wallAvoidanceDistance = 1.5f; // NavMesh Agent�̐i�s�����̃`�F�b�N����
    public LayerMask obstacleLayer;             // ��Q���ƂȂ郌�C���[ (Wall��Default�Ȃ�)


    // --- �����ϐ� ---
    private float nextAttackTime = 0f;          // ���ɍU���\�Ȏ���
    private float hardStopEndTime = 0f;         // �d������������鎞��
    private NavMeshAgent agent;                 // NavMeshAgent�R���|�[�l���g
    private float lastMoveTime = 0f;            // �Ō�Ɉړ���������
    private Animator animator;                  // Animator�R���|�[�l���g�ւ̎Q��

    private void Awake()
    {
        currentHealth = maxHealth;

        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError("NavMeshAgent component��������܂���B�G��NavMeshAgent���A�^�b�`���Ă��������B");
            enabled = false;
        }

        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogWarning("Animator component��������܂���B�G��Animator���A�^�b�`���Ă��������B");
        }

        // Player�^�[�Q�b�g�̎������o (AWAKE�ɒǉ�)
        if (playerTarget == null)
        {
            GameObject playerObject = GameObject.FindWithTag("Player");
            if (playerObject != null)
            {
                playerTarget = playerObject.transform;
            }
        }

        lastMoveTime = Time.time;
        Wander();
    }

    private void Update()
    {
        // �f�o�b�O�p�R�[�h: O�L�[��HP��0�ɂ���
        if (Input.GetKeyDown(KeyCode.O))
        {
            TakeDamage(maxHealth);
            return;
        }

        // ���S���A�d�����A�܂��̓^�[�Q�b�g���Ȃ��ꍇ�͏������X�L�b�v
        if (isDead || playerTarget == null || Time.time < hardStopEndTime)
        {
            if (agent != null && agent.enabled) agent.isStopped = true;
            return;
        }

        if (agent == null || !agent.enabled) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);

        // --- �ړ���Ԃ̃`�F�b�N�ƍX�V ---
        if (agent.velocity.sqrMagnitude > 0.01f)
        {
            lastMoveTime = Time.time;

            // ?? �V�K�ǉ�: �ړ����ɕǂɋ߂Â������Ă��Ȃ����`�F�b�N
            CheckForWallCollision();
        }

        // 2. Player���U���͈͓��ɂ��邩�H
        if (distanceToPlayer <= detectionRange)
        {
            agent.isStopped = true;
            LookAtPlayer();

            if (Time.time >= nextAttackTime && IsPlayerInFrontView())
            {
                AttackPlayer();
                nextAttackTime = Time.time + 1f / attackRate;
            }
        }
        else
        {
            agent.isStopped = false;

            bool needNewDestination =
                !agent.hasPath ||
                agent.remainingDistance < destinationThreshold ||
                (Time.time - lastMoveTime) >= maxIdleTime;

            if (needNewDestination)
            {
                Wander();
            }
        }
    }

    // -------------------------------------------------------------------
    //          �Փˉ������ (NavMesh�p)
    // -------------------------------------------------------------------

    /// <summary>
    /// NavMeshAgent�̐i�s�����ɕǂ��Ȃ����`�F�b�N���A����΋����I�Ɉړ��𒆒f�E�ĒT��������
    /// </summary>
    private void CheckForWallCollision()
    {
        // Agent���ړ����ŁA�܂��ړI�n�ɓ��B���Ă��Ȃ��ꍇ�̂݃`�F�b�N
        if (agent.isStopped || agent.remainingDistance <= agent.stoppingDistance)
        {
            return;
        }

        RaycastHit hit;
        // Agent�̐i�s�����ivelocity�𐳋K���������́j
        Vector3 movementDirection = agent.velocity.normalized;

        // Raycast�őO���ɕǂ����邩�`�F�b�N
        // Agent�̐i�s�����ivelocity�j���g���ă`�F�b�N���邱�ƂŁANavMeshAgent�̋O�����ǂ݂��܂��B
        if (Physics.Raycast(transform.position, movementDirection, out hit, wallAvoidanceDistance, obstacleLayer))
        {
            // Raycast�����������o���A���ꂪWALL_TAG�������Ă���ꍇ
            if (hit.collider.CompareTag(WALL_TAG))
            {
                Debug.LogWarning($"[{gameObject.name}] **�ړ������̖ڂ̑O�ɕǂ����o**�INavMeshAgent�̂��蔲����h�~���A�V�����ړI�n��T���܂��B");

                // �����I�Ɉړ����~
                agent.isStopped = true;

                // �V�����ړI�n��T���iWander���W�b�N���Ď��s�j
                Wander();
            }
        }
    }

    // -------------------------------------------------------------------
    //          �w���X�Ǝ��S���� (�ύX�Ȃ�)
    // -------------------------------------------------------------------

    /// <summary>
    /// �O������_���[�W���󂯎�邽�߂̌��J���\�b�h
    /// </summary>
    public void TakeDamage(float damageAmount)
    {
        if (isDead) return;

        currentHealth -= damageAmount;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// ���S����
    /// </summary>
    private void Die()
    {
        if (isDead) return;

        isDead = true;
        Debug.Log(gameObject.name + "�͔j�󂳂�܂����I");

        // 1. Animator��Dead�p�����[�^��true�ɐݒ肵�ăA�j���[�V�������J�n
        if (animator != null)
        {
            animator.SetBool("Dead", true);
        }

        // 2. NavMeshAgent���~
        if (agent != null && agent.enabled)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        // 3. ���S�A�j���[�V�����̍Đ���ɔ����E�폜���s���R���[�`�����J�n
        StartCoroutine(DeathSequence(deathAnimationDuration));
    }

    /// <summary>
    /// ���S�A�j���[�V�������I������̂�҂��A�����G�t�F�N�g���Đ����Ă���I�u�W�F�N�g���폜����R���[�`��
    /// </summary>
    private IEnumerator DeathSequence(float duration)
    {
        yield return new WaitForSeconds(duration);

        if (explosionPrefab != null)
        {
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        }
        Destroy(gameObject);
    }

    // -------------------------------------------------------------------
    //          ���̑����[�e�B���e�B (�ύX�Ȃ�)
    // -------------------------------------------------------------------

    /// <summary>
    /// Player���G�l�~�[�̑O������p���ɂ��邩���`�F�b�N����
    /// </summary>
    private bool IsPlayerInFrontView()
    {
        Vector3 directionToTarget = playerTarget.position - transform.position;
        directionToTarget.y = 0;

        Vector3 forward = transform.forward;
        forward.y = 0;

        float angle = Vector3.Angle(forward, directionToTarget);

        return angle <= attackAngle / 2f;
    }

    /// <summary>
    /// �h���[���{�̂̌�����Player�̕����֌�����i�X���[�Y�ȉ�]�j
    /// </summary>
    private void LookAtPlayer()
    {
        Vector3 targetDirection = playerTarget.position - transform.position;
        targetDirection.y = 0;

        Quaternion targetRotation = Quaternion.LookRotation(targetDirection);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            Time.deltaTime * rotationSpeed
        );
    }

    /// <summary>
    /// NavMeshAgent���g���Ď��͂������_���Ɉړ�����V�����ړI�n��ݒ肷��
    /// </summary>
    private void Wander()
    {
        Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
        randomDirection += transform.position;

        NavMeshHit hit;

        if (NavMesh.SamplePosition(randomDirection, out hit, wanderRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
            lastMoveTime = Time.time;
        }
    }

    /// <summary>
    /// �r�[���𔭎˂���
    /// </summary>
    private void AttackPlayer()
    {
        if (beamOrigin == null || beamPrefab == null)
        {
            Debug.LogError("�r�[���̔��ˌ��܂���Prefab���ݒ肳��Ă��܂���B");
            return;
        }

        Vector3 directionToPlayer = playerTarget.position - beamOrigin.position;
        Quaternion beamTargetRotation = Quaternion.LookRotation(directionToPlayer);

        GameObject beam = Instantiate(beamPrefab, beamOrigin.position, beamTargetRotation);

        Rigidbody rb = beam.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = beam.transform.forward * beamSpeed;
        }
        else
        {
            Debug.LogWarning("�r�[��Prefab��Rigidbody������܂���B�ړ����W�b�N��ǉ����Ă��������B");
        }

        hardStopEndTime = Time.time + hardStopDuration;
    }

    // �͈͂��������邽�߂�Gizmo (�G�f�B�^�ł̂ݕ\��)
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        if (Application.isEditor && transform != null)
        {
            // 1. ����p�̉���
            Quaternion leftRayRotation = Quaternion.AngleAxis(-attackAngle / 2, Vector3.up);
            Quaternion rightRayRotation = Quaternion.AngleAxis(attackAngle / 2, Vector3.up);

            Vector3 leftRayDirection = leftRayRotation * transform.forward;
            Vector3 rightRayDirection = rightRayRotation * transform.forward;

            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, leftRayDirection * detectionRange);
            Gizmos.DrawRay(transform.position, rightRayDirection * detectionRange);

            // 2. Wandering Radius �̉���
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, wanderRadius);

            // 3. ?? �V�K�ǉ�: �ړ����̕ǉ��Raycast�̉���
            if (agent != null && agent.enabled && agent.velocity.sqrMagnitude > 0.01f)
            {
                Vector3 movementDirection = agent.velocity.normalized;

                // �ǌ��oRay���}�[���^�F�ŕ\��
                Gizmos.color = Color.magenta;
                Gizmos.DrawRay(transform.position, movementDirection * wallAvoidanceDistance);
            }
        }
    }
}