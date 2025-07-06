using UnityEngine;
using UnityEngine.UI; // UI�v�f���������߂ɒǉ�
using System.Collections.Generic; // List���g�����߂ɒǉ�
using System.Linq; // OrderBy���g�����߂ɒǉ�

public class PlayerController : MonoBehaviour
{
    // �ړ����x
    public float moveSpeed = 15.0f;
    // �u�[�X�g���̑��x�{��
    public float boostMultiplier = 2.0f;
    // �㏸/���~���x
    public float verticalSpeed = 10.0f;
    // �d�͂̋���
    public float gravity = -9.81f;
    // �n�ʔ���̃��C���[�}�X�N
    public LayerMask groundLayer;

    // --- �G�l���M�[�Q�[�W�֘A�̕ϐ� ---
    public float maxEnergy = 100.0f;
    public float currentEnergy;
    public float energyConsumptionRate = 15.0f;
    public float energyRecoveryRate = 10.0f;
    public float recoveryDelay = 1.0f;
    private float lastEnergyConsumptionTime;

    // UI��Slider�ւ̎Q�� (�C��: �G�l���M�[�Q�[�W�̕\���p)
    public Slider energySlider;

    private CharacterController controller;
    private Vector3 velocity; // Y�������̑��x���Ǘ����邽�߂̕ϐ�

    // TPS�J�����R���g���[���[�ւ̎Q��
    private TPSCameraController tpsCamController;

    // --- �r�b�g�U���֘A�̕ϐ� ---
    [Header("Bit Attack Settings")]
    public GameObject bitPrefab; // �ˏo����r�b�g��Prefab
    public float bitLaunchHeight = 5.0f; // �r�b�g���v���C���[�̌�납��㏸���鍂��
    public float bitLaunchDuration = 0.5f; // �r�b�g���㏸����܂ł̎���
    public float bitAttackSpeed = 20.0f; // �r�b�g���G�Ɍ������Ĕ�ԑ��x
    public float bitAttackEnergyCost = 20.0f; // �r�b�g�U��1�񂠂���̃G�l���M�[���� (�ϐ�����ύX)
    public float lockOnRange = 30.0f; // �G�����b�N�I���ł���ő勗��
    public LayerMask enemyLayer; // �G�̃��C���[
    public int maxLockedEnemies = 6; // ���b�N�ł���G�̍ő吔

    private List<Transform> lockedEnemies = new List<Transform>(); // ���b�N���ꂽ�G�̃��X�g
    private bool isAttacking = false; // �U�����t���O (�v���C���[�̓������Œ肷�邽��)
    private float attackTimer = 0.0f; // �U���A�j���[�V�������Ԃ̌p�����ԃ^�C�}�[ (�K�v�ɉ�����)
    public float attackFixedDuration = 0.8f; // �U�����Ƀv���C���[���Œ肳��鎞��

    // --- �r�b�g�̃X�|�[���ʒu�𕡐��ݒ� ---
    public List<Transform> bitSpawnPoints = new List<Transform>();
    public float bitArcHeight = 2.0f; // �㏸�O���̃A�[�`�̍���

    // --- �ߐڍU���֘A�̕ϐ� ---
    [Header("Melee Attack Settings")]
    public float meleeAttackRange = 2.0f; // �ߐڍU���̗L���͈�
    public float meleeAttackRadius = 1.0f; // �ߐڍU���̗L�����a (SphereCast�p)
    public float meleeAttackCooldown = 0.5f; // �ߐڍU���̃N�[���_�E������
    private float lastMeleeAttackTime = -Mathf.Infinity; // �Ō�ɋߐڍU������������
    private int currentMeleeCombo = 0; // ���݂̋ߐڍU���R���{�i�K
    public int maxMeleeCombo = 5; // �ߐڍU���̍ő�R���{�i�K
    public float comboResetTime = 1.0f; // �R���{�����Z�b�g�����܂ł̎���
    private float lastMeleeInputTime; // �Ō�ɋߐڍU�����͂�����������

    // --- �r�[���U���֘A�̕ϐ� ---
    [Header("Beam Attack Settings")]
    public float beamAttackRange = 50.0f; // �r�[���̍ő�˒�����
    public float beamDamage = 50.0f; // �r�[���̃_���[�W
    public float beamEnergyCost = 10.0f; // �r�[���U��1�񂠂���̃G�l���M�[����
    public float beamCooldown = 0.5f; // �r�[���U���̃N�[���_�E������
    private float lastBeamAttackTime = -Mathf.Infinity; // �Ō�Ƀr�[���U������������
    public GameObject beamEffectPrefab; // �r�[���̃G�t�F�N�gPrefab (�C��)
    public Transform beamSpawnPoint; // �r�[���̊J�n�ʒu (��: �v���C���[�̖ڂ̑O�Ȃ�)

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (controller == null)
        {
            Debug.LogError("PlayerController: CharacterController��������܂���B���̃X�N���v�g��CharacterController���K�v�ł��B");
            enabled = false;
        }

        tpsCamController = FindObjectOfType<TPSCameraController>();
        if (tpsCamController == null)
        {
            Debug.LogError("PlayerController: TPSCameraController��������܂���B�J�����R���g���[�����V�[���ɑ��݂��邩�A�������A�^�b�`����Ă��邩�m�F���Ă��������B");
        }

        currentEnergy = maxEnergy;
        UpdateEnergyUI();

        if (bitSpawnPoints.Count == 0)
        {
            Debug.LogWarning("PlayerController: bitSpawnPoints���ݒ肳��Ă��܂���BHierarchy�ɋ�̃Q�[���I�u�W�F�N�g���쐬���A���̃��X�g�Ƀh���b�O���h���b�v���Ă��������B");
        }
        if (beamSpawnPoint == null)
        {
            Debug.LogWarning("PlayerController: Beam Spawn Point���ݒ肳��Ă��܂���BHierarchy�ɋ�̃Q�[���I�u�W�F�N�g���쐬���A���̃t�B�[���h�Ƀh���b�O���h���b�v���Ă��������B");
        }
    }

    void Update()
    {
        // �U�����̓v���C���[�̓������Œ�
        if (isAttacking)
        {
            HandleAttackState(); // �U�����̃v���C���[�̏�Ԃ�����
            return; // �U�����͑��̈ړ��������X�L�b�v
        }

        // �J�����̐��������ɍ��킹�ăv���C���[�̌����𒲐� (�U�����ȊO)
        if (tpsCamController != null)
        {
            tpsCamController.RotatePlayerToCameraDirection();
        }

        // --- �U�����͏��� ---
        // ���N���b�N�ŋߐڍU��
        if (Input.GetMouseButtonDown(0)) // 0�͍��N���b�N
        {
            PerformMeleeAttack();
        }
        // �z�C�[�������݂Ńr�b�g�U��
        else if (Input.GetMouseButtonDown(2)) // 2�̓z�C�[��������
        {
            PerformBitAttack();
        }
        // �E�N���b�N�Ńr�[���U��
        else if (Input.GetMouseButtonDown(1)) // 1�͉E�N���b�N
        {
            PerformBeamAttack();
        }

        // �R���{�^�C�}�[�̃��Z�b�g
        if (Time.time - lastMeleeInputTime > comboResetTime)
        {
            currentMeleeCombo = 0;
        }

        bool isGrounded = controller.isGrounded;

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        Vector3 moveDirection = Vector3.zero;
        if (tpsCamController != null)
        {
            Quaternion cameraHorizontalRotation = Quaternion.Euler(0, tpsCamController.transform.eulerAngles.y, 0);
            moveDirection = cameraHorizontalRotation * (Vector3.right * horizontalInput + Vector3.forward * verticalInput);
        }
        else
        {
            moveDirection = transform.right * horizontalInput + transform.forward * verticalInput;
        }
        moveDirection.Normalize();

        bool isConsumingEnergy = false;

        float currentSpeed = moveSpeed;
        if ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && currentEnergy > 0)
        {
            currentSpeed *= boostMultiplier;
            currentEnergy -= energyConsumptionRate * Time.deltaTime;
            isConsumingEnergy = true;
        }
        moveDirection *= currentSpeed;

        if (Input.GetKey(KeyCode.Space) && currentEnergy > 0)
        {
            velocity.y = verticalSpeed;
            currentEnergy -= energyConsumptionRate * Time.deltaTime;
            isConsumingEnergy = true;
        }
        else if ((Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) && currentEnergy > 0)
        {
            velocity.y = -verticalSpeed;
            currentEnergy -= energyConsumptionRate * Time.deltaTime;
            isConsumingEnergy = true;
        }
        else if (!isGrounded)
        {
            velocity.y += gravity * Time.deltaTime;
        }

        if (currentEnergy <= 0)
        {
            currentEnergy = 0;
            if (moveDirection.magnitude > moveSpeed)
            {
                moveDirection = moveDirection.normalized * moveSpeed;
            }
            if (velocity.y > 0) velocity.y = 0;
        }

        if (isConsumingEnergy)
        {
            lastEnergyConsumptionTime = Time.time;
        }
        else if (Time.time >= lastEnergyConsumptionTime + recoveryDelay)
        {
            currentEnergy += energyRecoveryRate * Time.deltaTime;
        }

        currentEnergy = Mathf.Clamp(currentEnergy, 0, maxEnergy);

        UpdateEnergyUI();

        Vector3 finalMove = moveDirection + new Vector3(0, velocity.y, 0);
        controller.Move(finalMove * Time.deltaTime);
    }

    /// <summary>
    /// UI�̃G�l���M�[�Q�[�W�iSlider�j���X�V���郁�\�b�h
    /// </summary>
    void UpdateEnergyUI()
    {
        if (energySlider != null)
        {
            energySlider.value = currentEnergy / maxEnergy;
        }
    }

    /// <summary>
    /// ���͂̓G�����b�N�I������
    /// </summary>
    void LockOnEnemies()
    {
        lockedEnemies.Clear(); // ���b�N�I�����X�g���N���A

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, lockOnRange, enemyLayer);

        // �������߂����Ƀ\�[�g���āAmaxLockedEnemies�̐��܂Ń��b�N�I��
        var sortedEnemies = hitColliders.OrderBy(col => Vector3.Distance(transform.position, col.transform.position))
                                        .Take(maxLockedEnemies);

        foreach (Collider col in sortedEnemies)
        {
            if (col.transform != transform) // �v���C���[���g�����b�N�I�����Ȃ��悤��
            {
                lockedEnemies.Add(col.transform);
                Debug.Log($"Locked on: {col.name}");
            }
        }

        if (lockedEnemies.Count > 0)
        {
            // �v���C���[�̌�������ԋ߂����b�N�I���G�̕����ɋ����I�Ɍ�����
            Vector3 lookAtTarget = lockedEnemies[0].position;
            lookAtTarget.y = transform.position.y; // Y���͌Œ�
            transform.LookAt(lookAtTarget);
        }
    }

    /// <summary>
    /// �r�b�g�U�������s����
    /// </summary>
    void PerformBitAttack()
    {
        // bitSpawnPoints���ݒ肳��Ă��Ȃ��ꍇ�͍U���𒆎~
        if (bitSpawnPoints.Count == 0)
        {
            Debug.LogWarning("Bit spawn points are not set up in the Inspector. Cannot perform bit attack.");
            return;
        }

        // ���b�N�ł���G�̐��imaxLockedEnemies�j���̃G�l���M�[���K�v�ɂȂ�悤�ɒ���
        if (currentEnergy < bitAttackEnergyCost * maxLockedEnemies) // �ϐ����ύX
        {
            Debug.Log($"Not enough energy for Bit Attack! Need {bitAttackEnergyCost * maxLockedEnemies} energy.");
            return;
        }

        LockOnEnemies(); // �U���O�ɓG�����b�N�I��

        if (lockedEnemies.Count == 0)
        {
            Debug.Log("No enemies to lock on. Bit attack cancelled.");
            return;
        }

        currentEnergy -= bitAttackEnergyCost * lockedEnemies.Count; // ���b�N�����G�̐��ɉ����ăG�l���M�[���� (�ϐ����ύX)
        UpdateEnergyUI();

        isAttacking = true; // �U�����t���O�𗧂Ă�
        attackTimer = 0.0f; // �^�C�}�[�����Z�b�g

        // ���b�N�����G�̐��A�܂���bitSpawnPoints�̐��܂Ńr�b�g���ˏo�i���Ȃ����ɍ��킹��j
        int bitsToSpawn = Mathf.Min(lockedEnemies.Count, bitSpawnPoints.Count);

        for (int i = 0; i < bitsToSpawn; i++)
        {
            // �e�r�b�g�̃X�|�[���ʒu��bitSpawnPoints����擾
            Transform spawnPoint = bitSpawnPoints[i];

            // i�Ԗڂ̃r�b�g��i�Ԗڂ̃��b�N�I���G�ɕR�t���� (�G�����Ȃ��ꍇ�̓��[�v�̏�]���g�p�Ȃ�)
            Transform targetEnemy = lockedEnemies[i % lockedEnemies.Count];

            StartCoroutine(LaunchBit(spawnPoint.position, targetEnemy)); // Transform��position��n��
        }
    }

    /// <summary>
    /// �ߐڍU�������s���� (5�i�K�R���{)
    /// </summary>
    void PerformMeleeAttack()
    {
        // �N�[���_�E�����܂��͊��ɍU�����̏ꍇ�͎��s���Ȃ�
        if (Time.time < lastMeleeAttackTime + meleeAttackCooldown || isAttacking)
        {
            return;
        }

        // �R���{�i�K��i�߂�
        currentMeleeCombo = (currentMeleeCombo % maxMeleeCombo) + 1;
        Debug.Log($"�ߐڍU���I�R���{�i�K: {currentMeleeCombo}");

        lastMeleeAttackTime = Time.time;
        lastMeleeInputTime = Time.time; // �R���{���Z�b�g�^�C�}�[���X�V

        isAttacking = true; // �U�����t���O�𗧂Ă�
        attackTimer = 0.0f; // �^�C�}�[�����Z�b�g

        // �ߐڍU���͈͓̔��̓G�����o
        Vector3 attackOrigin = transform.position + transform.forward * meleeAttackRange * 0.5f; // �v���C���[�̑O���������ꂽ�ʒu����
        Collider[] hitColliders = Physics.OverlapSphere(attackOrigin, meleeAttackRadius, enemyLayer);

        foreach (Collider hitCollider in hitColliders)
        {
            // �G��Health�R���|�[�l���g��T���ă_���[�W��^����
            EnemyHealth enemyHealth = hitCollider.GetComponent<EnemyHealth>();
            if (enemyHealth == null)
            {
                enemyHealth = hitCollider.GetComponentInParent<EnemyHealth>();
            }

            if (enemyHealth != null)
            {
                // �R���{�i�K�ɂ���ă_���[�W��ω��������
                int damage = 10 + (currentMeleeCombo - 1) * 5; // 1�i�K��10�A2�i�K��15...
                enemyHealth.TakeDamage(damage);
                Debug.Log($"{hitCollider.name} �� {damage} �_���[�W��^���܂����B(�R���{ {currentMeleeCombo})");
            }
        }
    }

    /// <summary>
    /// �r�[���U�������s����
    /// </summary>
    void PerformBeamAttack()
    {
        // �N�[���_�E�����A�܂��̓G�l���M�[�s���A�܂��͊��ɍU�����̏ꍇ�͎��s���Ȃ�
        if (Time.time < lastBeamAttackTime + beamCooldown || currentEnergy < beamEnergyCost || isAttacking)
        {
            if (currentEnergy < beamEnergyCost)
            {
                Debug.Log("Not enough energy for Beam Attack!");
            }
            return;
        }

        if (beamSpawnPoint == null)
        {
            Debug.LogWarning("Beam Spawn Point is not assigned. Cannot perform beam attack.");
            return;
        }

        currentEnergy -= beamEnergyCost;
        UpdateEnergyUI();
        lastBeamAttackTime = Time.time;

        Debug.Log("�r�[���U���I");

        // �v���C���[�̌������J�����̐��������ɍ��킹��
        if (tpsCamController != null)
        {
            tpsCamController.RotatePlayerToCameraDirection();
        }

        // �r�[����Raycast�J�n�ʒu�ƕ���������
        Vector3 rayOrigin = beamSpawnPoint.position;
        Vector3 rayDirection = tpsCamController != null ? tpsCamController.transform.forward : transform.forward;

        RaycastHit hit;
        // Raycast�œG�����o
        if (Physics.Raycast(rayOrigin, rayDirection, out hit, beamAttackRange, enemyLayer))
        {
            // �q�b�g�����G�Ƀ_���[�W
            EnemyHealth enemyHealth = hit.collider.GetComponent<EnemyHealth>();
            if (enemyHealth == null)
            {
                enemyHealth = hit.collider.GetComponentInParent<EnemyHealth>();
            }

            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(beamDamage);
                Debug.Log($"{hit.collider.name} �Ƀr�[���� {beamDamage} �_���[�W��^���܂����B");
            }

            // �q�b�g�ʒu�Ƀr�[���G�t�F�N�g�𐶐� (���������)
            if (beamEffectPrefab != null)
            {
                // �r�[���̊J�n�_����q�b�g�_�܂ł̊ԂɃG�t�F�N�g�𒲐����邱�Ƃ��\
                Instantiate(beamEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
            }
        }
        else
        {
            // �����q�b�g���Ȃ������ꍇ�ł��A�r�[���̏I�_�ɃG�t�F�N�g�𐶐�����Ȃǂ̏���
            if (beamEffectPrefab != null)
            {
                Instantiate(beamEffectPrefab, rayOrigin + rayDirection * beamAttackRange, Quaternion.identity);
            }
        }

        // �r�[���U�����͈ꎞ�I�Ƀv���C���[�̓������Œ�
        isAttacking = true;
        attackTimer = 0.0f;
        // �r�[���U���̃A�j���[�V������G�t�F�N�g���������Ԃɍ��킹��attackFixedDuration�𒲐�
        // �������A�r�[���͏u�ԓI�ȍU���Ȃ̂ŁA�����ł͒Z�߂ɐݒ�
        attackFixedDuration = 0.2f; // ��: �r�[�����˂̃A�j���[�V��������
    }

    /// <summary>
    /// �U�����̃v���C���[�̏�Ԃ������i�����Œ�A�����̈ێ��Ȃǁj
    /// </summary>
    void HandleAttackState()
    {
        // �U���A�j���[�V������G�t�F�N�g�̍Đ����Ƀv���C���[�̓������Œ�
        // �r�b�g�U�����̓��b�N�I�������G�Ɍ�����
        if (Input.GetMouseButtonDown(2) && lockedEnemies.Count > 0 && lockedEnemies[0] != null) // �r�b�g�U�����̏ꍇ
        {
            Vector3 lookAtTarget = lockedEnemies[0].position;
            lookAtTarget.y = transform.position.y;
            Quaternion targetRotation = Quaternion.LookRotation(lookAtTarget - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }
        // �r�[���U�����̓J�����̌������ێ�
        else if (Input.GetMouseButtonDown(1) && tpsCamController != null) // �r�[���U�����̏ꍇ
        {
            tpsCamController.RotatePlayerToCameraDirection(); // �ēx�v���C���[���J���������ɋ���
        }


        attackTimer += Time.deltaTime;
        if (attackTimer >= attackFixedDuration)
        {
            isAttacking = false;
            // ���b�N�I�������G���j�󂳂ꂽ�\�������邽�߁A�N���[���A�b�v
            lockedEnemies.RemoveAll(t => t == null);
            // ���b�N�I��������UI�\���ȂǁA�K�v�ȏ�����ǉ�
            Debug.Log("Attack sequence finished.");
            // attackFixedDuration ���f�t�H���g�ɖ߂����A�e�U���̃��\�b�h���Őݒ肷��
            attackFixedDuration = 0.8f; // �����Ńf�t�H���g�ɖ߂���
        }
    }

    /// <summary>
    /// �r�b�g���ˏo���A�G�Ɍ������Ĕ�΂��R���[�`��
    /// </summary>
    /// <param name="initialSpawnPosition">�r�b�g�̏����X�|�[���ʒu�i���[���h���W�j</param>
    /// <param name="target">�r�b�g���������^�[�Q�b�g</param>
    System.Collections.IEnumerator LaunchBit(Vector3 initialSpawnPosition, Transform target)
    {
        if (bitPrefab != null)
        {
            GameObject bitInstance = Instantiate(bitPrefab, initialSpawnPosition, Quaternion.identity);
            Bit bitScript = bitInstance.GetComponent<Bit>();

            if (bitScript != null)
            {
                bitScript.InitializeBit(initialSpawnPosition, target, bitLaunchHeight, bitLaunchDuration, bitAttackSpeed, bitArcHeight, enemyLayer);
            }
            else
            {
                Debug.LogWarning("Bit Prefab does not have a 'Bit' script attached!");
            }
        }
        else
        {
            Debug.LogError("Bit Prefab is not assigned!");
        }
        yield return null; // �R���[�`���Ƃ��ċ@�\�����邽�߂ɍŒ�1�t���[���҂�
    }

    // �f�o�b�O�\���p (Gizmos)
    void OnDrawGizmosSelected()
    {
        // �ߐڍU���͈̔͂����o��
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + transform.forward * meleeAttackRange * 0.5f, meleeAttackRadius);

        // �r�[���U���̎˒������o��
        Gizmos.color = Color.blue;
        if (beamSpawnPoint != null)
        {
            Gizmos.DrawRay(beamSpawnPoint.position, tpsCamController != null ? tpsCamController.transform.forward * beamAttackRange : transform.forward * beamAttackRange);
            Gizmos.DrawSphere(beamSpawnPoint.position + (tpsCamController != null ? tpsCamController.transform.forward : transform.forward) * beamAttackRange, 0.5f); // �I�_�ɋ�
        }
        else
        {
            Gizmos.DrawRay(transform.position, transform.forward * beamAttackRange);
            Gizmos.DrawSphere(transform.position + transform.forward * beamAttackRange, 0.5f);
        }
    }
}