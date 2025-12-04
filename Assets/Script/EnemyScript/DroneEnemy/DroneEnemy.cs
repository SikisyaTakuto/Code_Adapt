using UnityEngine;
using System.Collections; // �R���[�`�����g�p���邽�߂ɕK�v

public class DroneEnemy : MonoBehaviour
{
    // --- HP�ݒ� ---
    [Header("�w���X�ݒ�")]
    public float maxHealth = 100f; // �ő�HP
    private float currentHealth;    // ���݂�HP
    private bool isDead = false;    // ���S�t���O

    // ?? �V�K�ǉ�: �����G�t�F�N�g��Prefab
    [Header("�G�t�F�N�g�ݒ�")]
    public GameObject explosionPrefab;

    // --- ���J�p�����[�^ ---
    [Header("�^�[�Q�b�g�ݒ�")]
    public Transform playerTarget;              // Player��Transform�������ɐݒ�
    public float detectionRange = 15f;          // Player�����o����͈�
    public Transform beamOrigin;                // �e�̔��ˌ��ƂȂ�Transform

    [Range(0, 180)]
    public float attackAngle = 30f;             // �U���\�Ȑ��ʎ���p�i�S�p�j

    [Header("�U���ݒ�")]
    public float attackRate = 5f;               // �e�ƒe�̊Ԃ̊Ԋu�v�Z�Ɏg�p (��: 1/5 = 0.2�b�Ԋu)
    public GameObject beamPrefab;               // ���˂���e��Prefab
    public float beamSpeed = 40f;               // �e�̑��x

    [Header("�o�[�X�g�U���ݒ�")]
    public int bulletsPerBurst = 5;
    public float burstCooldownTime = 2f;

    [Header("�d���ݒ�")]
    public float hardStopDuration = 0.5f;

    [Header("���V�ړ��ݒ�")]
    public float rotationSpeed = 5f;             // Player�ǐՎ��̉�]���x�i�h���[���{�̗p�j
    public float gunRotationSpeed = 20f;
    public float hoverAltitude = 5f;
    public float driftSpeed = 1f;
    public float driftRange = 5f;
    public float altitudeCorrectionSpeed = 2f;

    // ?? �V�K�ǉ�: ��Q������̂��߂̐ݒ�
    [Header("��Q�����ݒ�")]
    public LayerMask obstacleLayer;              // ��Q���ƂȂ郌�C���[
    public float avoidanceCheckDistance = 3f;    // �O���`�F�b�N����
    public float wallHitResetRange = 1f;         // �ǂɐڐG�����ƌ��Ȃ����� (�Փ˂�h�����߂ɑ傫�߂�)

    // --- �����ϐ� ---
    private float nextAttackTime = 0f;
    private float hardStopEndTime = 0f;
    private Vector3 currentDriftTarget;

    private bool isAttacking = false;

    private void Awake()
    {
        currentHealth = maxHealth;

        SetNewDriftTarget();
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
            return;
        }

        // ?? �V�K�ǉ�: �ړ��O�ɑO���`�F�b�N�ƖڕW�n�_�̃��Z�b�g
        CheckForObstaclesAndResetTarget();

        // ?? �e�������Player�Ɍ�����
        RotateGunToPlayer();

        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);

        // 2. Player���U���͈͓��ɂ��邩�H
        if (distanceToPlayer <= detectionRange)
        {
            // �h���[���{�̂�Player�Ɍ�����
            LookAtPlayer();

            // �U�����łȂ���΁A�o�[�X�g�U�����J�n
            if (!isAttacking && IsPlayerInFrontView())
            {
                StartCoroutine(BurstAttackSequence());
            }
        }

        // ��ɋ󒆂ŕ��V�ړ�
        DriftHover();
    }

    // -------------------------------------------------------------------
    //                       �h���[���{�̂̉�] (Y���̂�)
    // -------------------------------------------------------------------

    /// <summary>
    /// �h���[���{�̂̌�����Player�̕����֌�����i�X���[�Y�ȉ�]�j
    /// </summary>
    private void LookAtPlayer()
    {
        // ... (���̃R�[�h�ƕύX�Ȃ��B�h���[���{�̂�Y����]) ...
        Vector3 targetDirection = playerTarget.position - transform.position;
        targetDirection.y = 0; // �󒆓G�Ȃ̂ŁA������]�̂�

        Quaternion targetRotation = Quaternion.LookRotation(targetDirection);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            Time.deltaTime * rotationSpeed
        );
    }

    // -------------------------------------------------------------------
    //                       �e���̉�]���� (�V�K�ǉ�)
    // -------------------------------------------------------------------

    /// <summary>
    /// �e�� (beamOrigin) ��Player��Transform�֌����ĉ�]������i�S����]�j
    /// </summary>
    private void RotateGunToPlayer()
    {
        if (beamOrigin == null || playerTarget == null) return;

        // Player�̈ʒu����e���̈ʒu�������āA�����x�N�g�����擾
        Vector3 targetDirection = playerTarget.position - beamOrigin.position;

        // �ڕW�Ƃ����] (Player�̕�������)
        Quaternion targetRotation = Quaternion.LookRotation(targetDirection);

        // �X���[�Y�ɉ�]������
        beamOrigin.rotation = Quaternion.Slerp(
            beamOrigin.rotation,
            targetRotation,
            Time.deltaTime * gunRotationSpeed
        );
    }

    // -------------------------------------------------------------------
    //                       �U������ (�o�[�X�g�V�X�e��)
    // -------------------------------------------------------------------

    private IEnumerator BurstAttackSequence()
    {
        isAttacking = true;

        float shotDelay = 0.5f / attackRate;

        // 1. �o�[�X�g�U��
        for (int i = 0; i < bulletsPerBurst; i++)
        {
            AttackSingleBullet();

            yield return new WaitForSeconds(shotDelay);
        }

        // 2. �o�[�X�g��̃N�[���^�C��
        yield return new WaitForSeconds(burstCooldownTime);

        isAttacking = false;
    }

    private void AttackSingleBullet()
    {
        if (beamOrigin == null || beamPrefab == null)
        {
            Debug.LogError("���ˌ��܂���Prefab���ݒ肳��Ă��܂���B");
            return;
        }

        // ?? �e��������Player�̕����������Ă��邽�߁AbeamOrigin.forward�𒼐ڎg�p
        Quaternion bulletRotation = beamOrigin.rotation;

        GameObject bullet = Instantiate(beamPrefab, beamOrigin.position, bulletRotation);

        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = bullet.transform.forward * beamSpeed;
        }
        else
        {
            Debug.LogWarning("�ePrefab��Rigidbody������܂���B");
        }
    }

    // -------------------------------------------------------------------
    //                       �󒆈ړ����� (�C���E�ǉ�)
    // -------------------------------------------------------------------

    /// <summary>
    /// �h���[���̈ړ��ڕW����Q�����ɂȂ������`�F�b�N���A�Փ˂������Ȃ�ڕW�����Z�b�g
    /// </summary>
    private void CheckForObstaclesAndResetTarget()
    {
        // currentDriftTarget�ւ̃x�N�g��
        Vector3 directionToTarget = (currentDriftTarget - transform.position);

        // 1. Raycast�ŖڕW�n�_�̕����ɏ�Q�������邩�`�F�b�N
        if (Physics.Raycast(transform.position, directionToTarget.normalized, out RaycastHit hit, avoidanceCheckDistance, obstacleLayer))
        {
            // �^�[�Q�b�g�̕������ǁI�V�����^�[�Q�b�g��ݒ�
            Debug.Log("?? �ڕW���� (" + hit.collider.name + ") �ɕǂ����o�B�ڕW�����Z�b�g���܂��B", gameObject);
            SetNewDriftTarget();
            return;
        }

        // 2. �ڕW�n�_���̂��ǂ̒���ǂ̉��ɂȂ��Ă��Ȃ������`�F�b�N (OverlapSphere)
        if (Physics.CheckSphere(currentDriftTarget, wallHitResetRange, obstacleLayer))
        {
            Debug.Log("?? ���݂̖ڕW�n�_���ǂ̒��ɐݒ肳��Ă��邽�߁A�ڕW�����Z�b�g���܂��B", gameObject);
            SetNewDriftTarget();
            return;
        }

        // 3. (�ی�): �h���[�����g�̑O�����ǂɐڐG���Ă��邩�`�F�b�N
        // �h���[�����i�s�����ɕǂ������Ă���Ƒz�肵�ă`�F�b�N
        if (Physics.Raycast(transform.position, transform.forward, avoidanceCheckDistance * 0.5f, obstacleLayer))
        {
            Debug.Log("?? �h���[�����O�̐i�s�������ǂɂԂ����Ă��܂��B�ڕW�����Z�b�g���܂��B", gameObject);
            SetNewDriftTarget();
        }
    }

    private void DriftHover()
    {
        Vector3 currentPos = transform.position;

        // 1. ���x�␳ (Y���̈ړ�)
        float targetY = hoverAltitude;
        float newY = Mathf.Lerp(currentPos.y, targetY, Time.deltaTime * altitudeCorrectionSpeed);

        // 2. ���������̈ړ� (X/Z���̕��V)
        Vector3 horizontalTarget = new Vector3(currentDriftTarget.x, newY, currentDriftTarget.z);

        transform.position = Vector3.MoveTowards(
            currentPos,
            horizontalTarget,
            Time.deltaTime * driftSpeed
        );

        // 3. �ڕW�n�_�ɓ��B������V�����ڕW��ݒ�
        if (Vector3.Distance(new Vector3(currentPos.x, 0, currentPos.z), new Vector3(currentDriftTarget.x, 0, currentDriftTarget.z)) < 0.5f)
        {
            SetNewDriftTarget();
        }
    }

    private void SetNewDriftTarget()
    {
        Vector3 newTarget;
        int attempts = 0;
        const int maxAttempts = 10; // ���[�v�̖�������h��

        // �Փ˂��Ȃ��ڕW�n�_��������܂ŌJ��Ԃ�
        do
        {
            Vector2 randomCircle = Random.insideUnitCircle * driftRange;

            newTarget = new Vector3(
                transform.position.x + randomCircle.x,
                hoverAltitude,
                transform.position.z + randomCircle.y
            );

            attempts++;

            // ?? �C��: �V�����ڕW�n�_����Q�����ɂȂ����`�F�b�N
            // CheckSphere�ŖڕW�n�_���ӂɏ�Q�����Ȃ����m�F����
        } while (Physics.CheckSphere(newTarget, wallHitResetRange, obstacleLayer) && attempts < maxAttempts);


        if (attempts >= maxAttempts)
        {
            Debug.LogWarning("�ڕW�n�_��������̂Ɏ��s���܂����B���ݒn���ӂ��ێ����܂��B", gameObject);
            // ������Ȃ������ꍇ�́A���݂̈ʒu��ڕW�Ƃ��āA�ړ����~������
            currentDriftTarget = transform.position;
        }
        else
        {
            currentDriftTarget = newTarget;
            // Y���W�𖳎����Č��݂̈ʒu����̃x�N�g�����v�Z
            Vector3 horizontalDirection = new Vector3(currentDriftTarget.x, transform.position.y, currentDriftTarget.z) - transform.position;
            // �������ڕW�n�_�֌������ăh���[���̌�����␳����
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(horizontalDirection), Time.deltaTime * rotationSpeed);
        }
    }

    // -------------------------------------------------------------------
    //                       �w���X�ƃ_���[�W����
    // -------------------------------------------------------------------

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

        // ?? �����G�t�F�N�g�̃C���X�^���X���ƍĐ�
        if (explosionPrefab != null)
        {
            // �h���[���̈ʒu�ɐ���
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        }

        // �R���[�`�����~���āA�e���A�˂����̂�h��
        StopAllCoroutines();

        // ���S��A�����Ƀh���[���{�̂̃����_���[��R���C�_�[�𖳌���
        // (�����ł͊ȒP��Destroy���g�p)
        Destroy(gameObject, 0.1f); // �G�t�F�N�g���������ꂽ�炷���Ƀh���[���{�̂��폜
    }

    // -------------------------------------------------------------------
    //                       ���̑����[�e�B���e�B
    // -------------------------------------------------------------------

    /// <summary>
    /// Player���G�l�~�[�̑O������p���ɂ��邩���`�F�b�N����
    /// </summary>
    private bool IsPlayerInFrontView()
    {
        // ... (�ύX�Ȃ�) ...
        Vector3 directionToTarget = playerTarget.position - transform.position;
        directionToTarget.y = 0;

        Vector3 forward = transform.forward;
        forward.y = 0;

        float angle = Vector3.Angle(forward, directionToTarget);

        return angle <= attackAngle / 2f;
    }


    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // ?? �V�K�ǉ�: ����`�F�b�N�����ƃ^�[�Q�b�g�̕�����Ray��\��
        if (Application.isEditor && transform != null)
        {
            // ���o�͈͂̉~���\�� (�U������p)
            Quaternion leftRayRotation = Quaternion.AngleAxis(-attackAngle / 2, Vector3.up);
            Quaternion rightRayRotation = Quaternion.AngleAxis(attackAngle / 2, Vector3.up);

            Vector3 leftRayDirection = leftRayRotation * transform.forward;
            Vector3 rightRayDirection = rightRayRotation * transform.forward;

            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, leftRayDirection * detectionRange);
            Gizmos.DrawRay(transform.position, rightRayDirection * detectionRange);

            // ���V�͈͂ƖڕW�n�_�̕\��
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, driftRange);
            Gizmos.DrawSphere(currentDriftTarget, 0.5f);

            // ?? �V�K�ǉ�: ����`�F�b�N��Raycast�\��
            Vector3 directionToTarget = (currentDriftTarget - transform.position).normalized;
            Gizmos.color = Color.magenta;
            Gizmos.DrawRay(transform.position, directionToTarget * avoidanceCheckDistance);

            // ?? �V�K�ǉ�: �ڕW�n�_�̏�Q���`�F�b�N�͈͕\��
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(currentDriftTarget, wallHitResetRange);
        }
    }

    public override bool Equals(object obj)
    {
        return obj is DroneEnemy enemy &&
               base.Equals(obj) &&
               nextAttackTime == enemy.nextAttackTime;
    }

    public override int GetHashCode()
    {
        return System.HashCode.Combine(base.GetHashCode(), nextAttackTime);
    }
}