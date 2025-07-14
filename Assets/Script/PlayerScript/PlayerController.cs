using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq; // OrderBy���g�����߂ɒǉ�
using System; // Action���g�����߂ɒǉ�

public class PlayerController : MonoBehaviour
{
    // --- �x�[�X�ƂȂ�\�͒l (�ύX�s��) ---
    [Header("Base Stats")]
    public float baseMoveSpeed = 15.0f;
    public float baseBoostMultiplier = 2.0f;
    public float baseVerticalSpeed = 10.0f;
    public float baseEnergyConsumptionRate = 15.0f;
    public float baseEnergyRecoveryRate = 10.0f;
    public float baseMeleeAttackRange = 2.0f;
    public float baseMeleeDamage = 10.0f; // ��{�̋ߐڃ_���[�W
    public float baseBeamDamage = 50.0f; // ��{�̃r�[���_���[�W
    public float baseBitAttackEnergyCost = 20.0f; // ��{�̃r�b�g�U���G�l���M�[����
    public float baseBeamAttackEnergyCost = 30.0f; // ���ǉ��F��{�̃r�[���U���G�l���M�[����

    // --- ���݂̔\�͒l (ArmorController�ɂ���ĕύX�����) ---
    [Header("Current Stats (Modified by Armor)")]
    public float moveSpeed;
    public float boostMultiplier;
    public float verticalSpeed;
    public float energyConsumptionRate;
    public float energyRecoveryRate;
    public float meleeAttackRange;
    public float meleeDamage;
    public float beamDamage;
    public float bitAttackEnergyCost;
    public float beamAttackEnergyCost; // ���ǉ��F���݂̃r�[���U���G�l���M�[����

    // ��s�@�\�̗L��/����
    public bool canFly = true;
    // �\�[�h�r�b�g�U���̗L��/����
    public bool canUseSwordBitAttack = false;


    // �d�͂̋���
    public float gravity = -9.81f;
    // �n�ʔ���̃��C���[�}�X�N
    public LayerMask groundLayer;

    // --- �G�l���M�[�Q�[�W�֘A�̕ϐ� ---
    public float maxEnergy = 100.0f;
    public float currentEnergy;
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
    public float meleeAttackRadius = 1.0f; // �ߐڍU���̗L�����a (SphereCast�p)
    public float meleeAttackCooldown = 0.5f; // �ߐڍU���̃N�[���_�E������
    private float lastMeleeAttackTime = -Mathf.Infinity; // �Ō�ɋߐڍU������������
    private int currentMeleeCombo = 0; // ���݂̋ߐڍU���R���{�i�K
    public int maxMeleeCombo = 5; // �ߐڍU���̍ő�R���{�i�K
    public float comboResetTime = 1.0f; // �R���{�����Z�b�g�����܂ł̎���
    private float lastMeleeInputTime; // �Ō�ɋߐڍU�����͂�����������
    public float autoLockOnMeleeRange = 5.0f; // �ߐڍU���̎������b�N�I���͈�
    public bool preferLockedMeleeTarget = true; // �ߐڍU�����Ƀ��b�N�I���\�ȓG��D�悷�邩
    private Transform currentLockedMeleeTarget; // ���݃��b�N�I�����Ă���ߐڍU���^�[�Q�b�g

    // ���ǉ�: �ߐڍU�����̓ːi���x�Ɠːi����
    public float meleeDashSpeed = 20.0f; // �ߐڍU�����̓ːi���x
    public float meleeDashDistance = 2.0f; // �ߐڍU�����̓ːi���� (meleeAttackRange�Ɠ������������߂ɐݒ肷��Ɨǂ�)
    public float meleeDashDuration = 0.1f; // �ߐڍU�����̓ːi�ɂ����鎞��


    // --- �r�[���U���֘A�̕ϐ� ---
    [Header("Beam Attack Settings")]
    public float beamAttackRange = 50.0f; // �r�[���̍ő�˒�����
    public float beamCooldown = 0.5f; // �r�[���U���̃N�[���_�E������
    private float lastBeamAttackTime = -Mathf.Infinity; // �Ō�Ƀr�[���U������������
    public GameObject beamEffectPrefab; // �r�[���̃G�t�F�N�gPrefab (�C��)
    public Transform beamSpawnPoint; // �r�[���̊J�n�ʒu (��: �v���C���[�̖ڂ̑O�Ȃ�)

    //�������b�N�I���r�[���֘A�̕ϐ�
    [Header("Auto Lock-on Beam Settings")]
    public float autoLockOnRange = 40.0f; // �������b�N�I���̍ő勗��
    public bool preferLockedTarget = true; // ���b�N�I���\�ȓG������ꍇ�A�������D�悷�邩
    private Transform currentLockedBeamTarget; // ���݃��b�N�I�����Ă���r�[���^�[�Q�b�g


    // --- �������̕���Prefab ---
    private GameObject currentPrimaryWeaponInstance;
    private GameObject currentSecondaryWeaponInstance;
    public Transform primaryWeaponAttachPoint; // �啐������t����Transform
    public Transform secondaryWeaponAttachPoint; // ����������t����Transform

    // ���ǉ�: �`���[�g���A���p
    public bool canReceiveInput = true; // �v���C���[�����͂ł��邩�ǂ����̃t���O
    public Action onWASDMoveCompleted; // WASD�ړ��������ɔ��΂���C�x���g
    public Action onJumpCompleted; // �W�����v�������ɔ��΂���C�x���g
    public Action onDescendCompleted; // �~���������ɔ��΂���C�x���g
    public Action onMeleeAttackPerformed; // �ߐڍU�����s���ɔ��΂���C�x���g
    public Action onBeamAttackPerformed; // �r�[���U�����s���ɔ��΂���C�x���g
    public Action onBitAttackPerformed; // ����U�����s���ɔ��΂���C�x���g
    public Action<int> onArmorModeChanged; // �A�[�}�[���[�h�ύX���ɔ��΂���C�x���g (�����̓��[�h�ԍ�)

    private float _wasdMoveTimer = 0f;
    private float _jumpTimer = 0f;
    private float _descendTimer = 0f;
    private bool _hasMovedWASD = false; // WASD����x�ł����͂��ꂽ��
    private bool _hasJumped = false; // �X�y�[�X�L�[����x�ł������ꂽ��
    private bool _hasDescended = false; // Alt�L�[����x�ł������ꂽ��


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

        // �����\�͒l�����݂̔\�͒l�ɐݒ�
        moveSpeed = baseMoveSpeed;
        boostMultiplier = baseBoostMultiplier;
        verticalSpeed = baseVerticalSpeed;
        energyConsumptionRate = baseEnergyConsumptionRate;
        energyRecoveryRate = baseEnergyRecoveryRate;
        meleeAttackRange = baseMeleeAttackRange;
        meleeDamage = baseMeleeDamage;
        beamDamage = baseBeamDamage;
        bitAttackEnergyCost = baseBitAttackEnergyCost;
        beamAttackEnergyCost = baseBeamAttackEnergyCost; // ���ǉ��F�����l��ݒ�

        // PlayerArmorController���珉��������邽�߁A�����ł̓f�t�H���g�̕���͑������Ȃ�
    }

    void Update()
    {
        if (!canReceiveInput) // ���ǉ�: ���͎�t�������Ȃ珈�����X�L�b�v
        {
            // �U�����Œ莞�Ԃ̃^�C�}�[�͐i�߂�
            if (isAttacking)
            {
                HandleAttackState();
            }
            return;
        }

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
            onMeleeAttackPerformed?.Invoke(); // ���ǉ�: �C�x���g����
        }
        // �z�C�[�������݂Ńr�b�g�U�� (�o�����X�A�[�}�[�̂�)
        else if (Input.GetMouseButtonDown(2) && canUseSwordBitAttack) // 2�̓z�C�[��������
        {
            PerformBitAttack();
            onBitAttackPerformed?.Invoke(); // ���ǉ�: �C�x���g����
        }
        // �E�N���b�N�Ńr�[���U��
        else if (Input.GetMouseButtonDown(1)) // 1�͉E�N���b�N
        {
            PerformBeamAttack(); // �����Ŏ������b�N�I���̃��W�b�N���Ăяo��
            onBeamAttackPerformed?.Invoke(); // ���ǉ�: �C�x���g����
        }
        // ���ǉ�: �A�[�}�[���[�h�؂�ւ�
        else if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            onArmorModeChanged?.Invoke(1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            onArmorModeChanged?.Invoke(2);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            onArmorModeChanged?.Invoke(3);
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

        // ���ǉ�: WASD���͂̊Ď�
        if (Mathf.Abs(horizontalInput) > 0.1f || Mathf.Abs(verticalInput) > 0.1f)
        {
            _wasdMoveTimer += Time.deltaTime;
            _hasMovedWASD = true;
        }
        else
        {
            _wasdMoveTimer = 0f; // ���͂��r�؂ꂽ�烊�Z�b�g
        }

        // ��s�@�\���L���ȏꍇ�̂݃X�y�[�X/Alt�ł̏㏸���~������
        if (canFly)
        {
            if (Input.GetKey(KeyCode.Space) && currentEnergy > 0)
            {
                velocity.y = verticalSpeed;
                currentEnergy -= energyConsumptionRate * Time.deltaTime;
                isConsumingEnergy = true;
                _jumpTimer += Time.deltaTime; // ���ǉ�: �W�����v�^�C�}�[�X�V
                _hasJumped = true;
            }
            else if ((Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) && currentEnergy > 0)
            {
                velocity.y = -verticalSpeed;
                currentEnergy -= energyConsumptionRate * Time.deltaTime;
                isConsumingEnergy = true;
                _descendTimer += Time.deltaTime; // ���ǉ�: �~���^�C�}�[�X�V
                _hasDescended = true;
            }
            else if (!isGrounded)
            {
                velocity.y += gravity * Time.deltaTime;
                // ���ǉ�: �X�y�[�X/Alt�������ꂽ��^�C�}�[�����Z�b�g
                _jumpTimer = 0f;
                _descendTimer = 0f;
            }
        }
        else // ��s�@�\�������ȏꍇ�͏d�͂̉e������Ɏ󂯂�
        {
            if (!isGrounded)
            {
                velocity.y += gravity * Time.deltaTime;
            }
            else
            {
                velocity.y = -2f; // �n�ʂɒ��n������Y���x�����Z�b�g
            }
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

    // ���ǉ�: �`���[�g���A���}�l�[�W���[���^�C�}�[���`�F�b�N���邽�߂̃v���p�e�B
    public float WASDMoveTimer => _wasdMoveTimer;
    public float JumpTimer => _jumpTimer;
    public float DescendTimer => _descendTimer;
    public bool HasMovedWASD => _hasMovedWASD;
    public bool HasJumped => _hasJumped;
    public bool HasDescended => _hasDescended;

    public void ResetInputTracking()
    {
        _wasdMoveTimer = 0f;
        _jumpTimer = 0f;
        _descendTimer = 0f;
        _hasMovedWASD = false;
        _hasJumped = false;
        _hasDescended = false;
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
    /// ���͂̓G�����b�N�I������ (�r�[���p)
    /// </summary>
    /// <returns>���b�N�I�������G��Transform�B������Ȃ����null�B</returns>
    Transform FindBeamTarget()
    {
        // �v���C���[��Transform��position�����SphereCast
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, autoLockOnRange, enemyLayer);

        if (hitColliders.Length == 0)
        {
            return null; // �G�����Ȃ�
        }

        // �ł��߂��G��������
        Transform closestEnemy = null;
        float minDistance = Mathf.Infinity;

        foreach (Collider col in hitColliders)
        {
            if (col.transform != transform) // �v���C���[���g�����O
            {
                float distance = Vector3.Distance(transform.position, col.transform.position);
                // �J�����̎��E�ɓ����Ă��邩�A�܂��͔��ɋ߂��G��D�悷��Ȃǂ̃��W�b�N��ǉ��\
                // ����͈�ԋ߂��G���^�[�Q�b�g�ɂ���
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestEnemy = col.transform;
                }
            }
        }
        return closestEnemy;
    }

    /// <summary>
    /// ���͂̓G�����b�N�I������ (�ߐڍU���p)
    /// </summary>
    /// <returns>���b�N�I�������G��Transform�B������Ȃ����null�B</returns>
    Transform FindMeleeTarget()
    {
        // �v���C���[��Transform��position�����OverlapSphere
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, autoLockOnMeleeRange, enemyLayer);

        if (hitColliders.Length == 0)
        {
            return null; // �G�����Ȃ�
        }

        // �ł��߂��G��������
        Transform closestEnemy = null;
        float minDistance = Mathf.Infinity;

        foreach (Collider col in hitColliders)
        {
            if (col.transform != transform) // �v���C���[���g�����O
            {
                float distance = Vector3.Distance(transform.position, col.transform.position);
                // �ߐڍU���Ȃ̂ŁA�P�Ɉ�ԋ߂��G�ŗǂ����Ƃ��������A
                // �����I�ɂ̓v���C���[�̐��ʕ����̓G��D�悷��ȂǁA��蕡�G�ȃ��W�b�N�������\
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestEnemy = col.transform;
                }
            }
        }
        return closestEnemy;
    }


    /// <summary>
    /// ���͂̓G�����b�N�I������ (�r�b�g�U���p)
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
        attackFixedDuration = 0.3f; // �ߐڍU���̌Œ莞�Ԃ�Z�߂ɐݒ� (�A�j���[�V�����ɍ��킹�Ē���)

        currentLockedMeleeTarget = null; // ���b�N�I���^�[�Q�b�g�����Z�b�g
        if (preferLockedMeleeTarget)
        {
            currentLockedMeleeTarget = FindMeleeTarget();
        }

        // ���C���_1: ���b�N�I���^�[�Q�b�g������ꍇ�A������̕�������������D��
        if (currentLockedMeleeTarget != null)
        {
            Vector3 lookAtTarget = currentLockedMeleeTarget.position;
            lookAtTarget.y = transform.position.y; // Y���͌Œ�
            transform.LookAt(lookAtTarget);
            Debug.Log($"�ߐڍU��: ���b�N�I���^�[�Q�b�g ({currentLockedMeleeTarget.name}) �֌������čU���I");

            // ���ǉ�: �G�Ɍ������ēːi����R���[�`�����J�n
            StartCoroutine(MeleeDashToTarget(currentLockedMeleeTarget.position));
        }
        else
        {
            // ���b�N�I���^�[�Q�b�g�����Ȃ��ꍇ�A�J�����̌������ێ�
            if (tpsCamController != null)
            {
                tpsCamController.RotatePlayerToCameraDirection();
            }
            // ���ǉ�: �^�[�Q�b�g�����Ȃ��ꍇ�A���݌����Ă�������֒Z���ːi
            StartCoroutine(MeleeDashInCurrentDirection());
        }

        // �ߐڍU���͈͓̔��̓G�����o
        Vector3 attackOrigin = transform.position + transform.forward * meleeAttackRange * 0.5f; // �v���C���[�̑O���������ꂽ�ʒu����
        Collider[] hitColliders = Physics.OverlapSphere(attackOrigin, meleeAttackRadius, enemyLayer);

        foreach (Collider hitCollider in hitColliders)
        {
            EnemyHealth enemyHealth = hitCollider.GetComponent<EnemyHealth>();
            if (enemyHealth == null)
            {
                enemyHealth = hitCollider.GetComponentInParent<EnemyHealth>();
            }

            if (enemyHealth != null)
            {
                float damage = meleeDamage + (currentMeleeCombo - 1) * (meleeDamage * 0.5f); // ��: �x�[�X�_���[�W�ɃR���{�{�[�i�X�����Z
                enemyHealth.TakeDamage(damage);
                Debug.Log($"{hitCollider.name} �� {damage} �_���[�W��^���܂����B(�R���{ {currentMeleeCombo})");
            }
        }
    }

    /// <summary>
    /// �r�[���U�������s����
    /// ���b�N�I���\�ȓG������΂������D�悵�A�Ȃ���΃J�����̕����֔���
    /// </summary>
    void PerformBeamAttack()
    {
        // �N�[���_�E�����A�܂��̓G�l���M�[�s���A�܂��͊��ɍU�����̏ꍇ�͎��s���Ȃ�
        if (Time.time < lastBeamAttackTime + beamCooldown || currentEnergy < beamAttackEnergyCost || isAttacking)
        {
            if (currentEnergy < beamAttackEnergyCost)
            {
                Debug.Log("Not enough energy for Beam Attack!");
            }
            return;
        }

        if (beamSpawnPoint == null)
        {
            Debug.LogWarning("Beam Spawn Point is not assigned. Cannot perform beam attack without a valid origin for effect.");
            return;
        }

        if (tpsCamController == null)
        {
            Debug.LogWarning("TPSCameraController is not assigned. Cannot perform camera-aligned beam attack.");
            return;
        }

        currentEnergy -= beamAttackEnergyCost;
        UpdateEnergyUI();
        lastBeamAttackTime = Time.time;

        Debug.Log("�r�[���U���I");

        // ���b�N�I���^�[�Q�b�g������
        currentLockedBeamTarget = null; // ���b�N�I���^�[�Q�b�g�����Z�b�g
        if (preferLockedTarget)
        {
            currentLockedBeamTarget = FindBeamTarget();
        }

        Vector3 rayOrigin;
        Vector3 rayDirection;

        if (currentLockedBeamTarget != null)
        {
            // ���b�N�I���^�[�Q�b�g������ꍇ�A�^�[�Q�b�g�̕����փr�[�����΂�
            // �v���C���[�̌������^�[�Q�b�g�̐��������Ɍ�����
            Vector3 targetFlatPos = currentLockedBeamTarget.position;
            targetFlatPos.y = transform.position.y;
            transform.LookAt(targetFlatPos);

            // beamSpawnPoint ����^�[�Q�b�g�����ւ�Ray��ݒ�
            rayOrigin = beamSpawnPoint.position;
            rayDirection = (currentLockedBeamTarget.position - beamSpawnPoint.position).normalized;

            Debug.Log($"�r�[��: ���b�N�I���^�[�Q�b�g ({currentLockedBeamTarget.name}) �֔��ˁI");
        }
        else
        {
            // ���b�N�I���^�[�Q�b�g�����Ȃ��ꍇ�A�J�����̕����փr�[�����΂��i�����̓���j
            // �v���C���[�̌������J�����̐��������ɍ��킹��
            tpsCamController.RotatePlayerToCameraDirection();

            // �J��������Ray���擾���A����Ray�̕�����Raycast���s��
            Ray cameraRay = tpsCamController.GetCameraRay();
            rayOrigin = cameraRay.origin;
            rayDirection = cameraRay.direction;

            Debug.Log("�r�[��: �J�����̕����֔��ˁI");
        }

        GameObject beamInstance = null; // �r�[���G�t�F�N�g�̃C���X�^���X��ێ�����ϐ�
        RaycastHit hit;

        // Raycast�œG�����o
        if (Physics.Raycast(rayOrigin, rayDirection, out hit, beamAttackRange, enemyLayer))
        {
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

            // �r�[���G�t�F�N�g��beamSpawnPoint���甭�˂��ARay�̐i�s��������������
            if (beamEffectPrefab != null)
            {
                beamInstance = Instantiate(beamEffectPrefab, beamSpawnPoint.position, Quaternion.LookRotation(rayDirection));
            }
        }
        else
        {
            // �����q�b�g���Ȃ������ꍇ�A�r�[���G�t�F�N�g��beamSpawnPoint���甭�˂��ARay�̐i�s��������������
            if (beamEffectPrefab != null)
            {
                beamInstance = Instantiate(beamEffectPrefab, beamSpawnPoint.position, Quaternion.LookRotation(rayDirection));
            }
        }

        // ���������r�[���G�t�F�N�g����莞�Ԍ�ɔj������
        if (beamInstance != null)
        {
            Destroy(beamInstance, 0.5f); // ��: 0.5�b��ɏ���
        }

        // �r�[���U�����͈ꎞ�I�Ƀv���C���[�̓������Œ�
        isAttacking = true;
        attackTimer = 0.0f;
        attackFixedDuration = 0.2f; // ��: �r�[�����˂̃A�j���[�V��������
    }

    /// <summary>
    /// �U�����̃v���C���[�̏�Ԃ������i�����Œ�A�����̈ێ��Ȃǁj
    /// </summary>
    void HandleAttackState()
    {
        // �U���A�j���[�V������G�t�F�N�g�̍Đ����Ƀv���C���[�̓������Œ�
        // �r�b�g�U�����̓��b�N�I�������G�Ɍ�����
        if (canUseSwordBitAttack && Input.GetMouseButtonDown(2) && lockedEnemies.Count > 0 && lockedEnemies[0] != null)
        {
            Vector3 lookAtTarget = lockedEnemies[0].position;
            lookAtTarget.y = transform.position.y;
            Quaternion targetRotation = Quaternion.LookRotation(lookAtTarget - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }
        // �ߐڍU�����́A���b�N�I�����Ă���G������΂�����������A���Ȃ���΃J�����̌������ێ�
        else if (Input.GetMouseButtonDown(0)) // �ߐڍU�����̏ꍇ
        {
            if (currentLockedMeleeTarget != null)
            {
                // ���b�N�I���^�[�Q�b�g�����݂���ꍇ�AY���Œ�Ń^�[�Q�b�g�̕�������
                Vector3 lookAtTarget = currentLockedMeleeTarget.position;
                lookAtTarget.y = transform.position.y;
                Quaternion targetRotation = Quaternion.LookRotation(lookAtTarget - transform.position);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
            }
            // else: ���b�N�I�����Ă��Ȃ���΁AUpdate�ŏ�ɃJ���������������Ă���̂œ��ʏ����͕s�v
        }
        // �r�[���U�����́A���b�N�I�����Ă���G������΂�����������A���Ȃ���΃J�����̌������ێ�
        else if (Input.GetMouseButtonDown(1)) // �r�[���U�����̏ꍇ
        {
            if (currentLockedBeamTarget != null)
            {
                // ���b�N�I���^�[�Q�b�g�����݂���ꍇ�AY���Œ�Ń^�[�Q�b�g�̕�������
                Vector3 lookAtTarget = currentLockedBeamTarget.position;
                lookAtTarget.y = transform.position.y;
                Quaternion targetRotation = Quaternion.LookRotation(lookAtTarget - transform.position);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
            }
            else if (tpsCamController != null)
            {
                tpsCamController.RotatePlayerToCameraDirection(); // ���b�N�I�����Ă��Ȃ���΃J���������ɋ���
            }
        }


        attackTimer += Time.deltaTime;
        if (attackTimer >= attackFixedDuration)
        {
            isAttacking = false;
            // �e�U���̃��b�N�I���^�[�Q�b�g���N���A
            lockedEnemies.RemoveAll(t => t == null); // �r�b�g�U���p
            currentLockedBeamTarget = null; // �r�[���U���p
            currentLockedMeleeTarget = null; // �ߐڍU���p

            Debug.Log("Attack sequence finished.");
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

    /// <summary>
    /// ����𑕔�����
    /// </summary>
    public void EquipWeapons(WeaponData primaryWeaponData, WeaponData secondaryWeaponData)
    {
        // �����̕����j��
        if (currentPrimaryWeaponInstance != null) Destroy(currentPrimaryWeaponInstance);
        if (currentSecondaryWeaponInstance != null) Destroy(currentSecondaryWeaponInstance);

        // �啐��̑���
        if (primaryWeaponData != null && primaryWeaponData.weaponPrefab != null && primaryWeaponAttachPoint != null)
        {
            currentPrimaryWeaponInstance = Instantiate(primaryWeaponData.weaponPrefab, primaryWeaponAttachPoint);
            currentPrimaryWeaponInstance.transform.localPosition = Vector3.zero;
            currentPrimaryWeaponInstance.transform.localRotation = Quaternion.identity;
            Debug.Log($"Primary Weapon Equipped: {primaryWeaponData.weaponName}");
        }

        // ������̑���
        if (secondaryWeaponData != null && secondaryWeaponData.weaponPrefab != null && secondaryWeaponAttachPoint != null)
        {
            currentSecondaryWeaponInstance = Instantiate(secondaryWeaponData.weaponPrefab, secondaryWeaponAttachPoint);
            currentSecondaryWeaponInstance.transform.localPosition = Vector3.zero;
            currentSecondaryWeaponInstance.transform.localRotation = Quaternion.identity;
            Debug.Log($"Secondary Weapon Equipped: {secondaryWeaponData.weaponName}");
        }
    }


    // �f�o�b�O�\���p (Gizmos)
    void OnDrawGizmosSelected()
    {
        // �ߐڍU���͈̔͂����o��
        Gizmos.color = Color.red;
        // �ߐڍU���̎������b�N�I���͈�
        Gizmos.DrawWireSphere(transform.position, autoLockOnMeleeRange);
        // �ʏ�̋ߐڍU������͈�
        Gizmos.DrawWireSphere(transform.position + transform.forward * meleeAttackRange * 0.5f, meleeAttackRadius);


        // �r�[���U���̎˒����A���b�N�I���^�[�Q�b�g�����݂���΂�����ցA�Ȃ���΃J������Ray�Ɋ�Â��Ď��o��
        Gizmos.color = Color.blue;
        if (tpsCamController != null)
        {
            Vector3 gizmoRayOrigin;
            Vector3 gizmoRayDirection;

            if (currentLockedBeamTarget != null) // ���b�N�I���^�[�Q�b�g�����݂���ꍇ
            {
                gizmoRayOrigin = beamSpawnPoint != null ? beamSpawnPoint.position : transform.position;
                gizmoRayDirection = (currentLockedBeamTarget.position - gizmoRayOrigin).normalized;
            }
            else // ���b�N�I���^�[�Q�b�g�����݂��Ȃ��ꍇ�i�J������Ray���g�p�j
            {
                Ray cameraRay = tpsCamController.GetCameraRay();
                gizmoRayOrigin = cameraRay.origin;
                gizmoRayDirection = cameraRay.direction;
            }

            Gizmos.DrawRay(gizmoRayOrigin, gizmoRayDirection * beamAttackRange);
            Gizmos.DrawSphere(gizmoRayOrigin + gizmoRayDirection * beamAttackRange, 0.5f); // �I�_�ɋ�

            // �������b�N�I���͈͂̕\��
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, autoLockOnRange);
        }
        else if (beamSpawnPoint != null) // TPSCameraController���Ȃ��ꍇ�̃t�H�[���o�b�N
        {
            Gizmos.DrawRay(beamSpawnPoint.position, beamSpawnPoint.forward * beamAttackRange);
            Gizmos.DrawSphere(beamSpawnPoint.position + beamSpawnPoint.forward * beamAttackRange, 0.5f);
        }
        else // beamSpawnPoint���ݒ肳��Ă��Ȃ��ꍇ�̃t�H�[���o�b�N
        {
            Gizmos.DrawRay(transform.position, transform.forward * beamAttackRange);
            Gizmos.DrawSphere(transform.position + transform.forward * beamAttackRange, 0.5f);
        }
    }

    /// <summary>
    /// �ߐڍU�����Ƀ^�[�Q�b�g�Ɍ������ēːi����R���[�`��
    /// </summary>
    /// <param name="targetPosition">�ːi�ڕW�n�_</param>
    private System.Collections.IEnumerator MeleeDashToTarget(Vector3 targetPosition)
    {
        Vector3 startPosition = transform.position;
        // �^�[�Q�b�g�܂ł̋������v�Z���AmeleeDashDistance�𒴂��Ȃ��悤�ɂ���
        Vector3 direction = (targetPosition - startPosition).normalized;
        Vector3 endPosition = startPosition + direction * Mathf.Min(Vector3.Distance(startPosition, targetPosition) - meleeAttackRange * 0.5f, meleeDashDistance);
        // meleeAttackRange * 0.5f �́A�G�́u���S�v�ɓːi����̂ł͂Ȃ��A�U���͈͂̓͂���O�Ŏ~�܂�悤�ɒ���

        float elapsedTime = 0f;

        while (elapsedTime < meleeDashDuration)
        {
            // CharacterController.Move ���g���Ĉړ�
            // CharacterController �̓R���W�����Ɏ����I�ɔ������邽�߁A�P�Ɉړ��x�N�g����^����
            Vector3 currentMove = Vector3.Lerp(startPosition, endPosition, elapsedTime / meleeDashDuration) - transform.position;
            controller.Move(currentMove);

            elapsedTime += Time.deltaTime;
            yield return null;
        }
        // �ːi�I�����ɍŏI�I�Ȉʒu�Ɋm���ɓ��B������iCharacterController.Move �̓�����A���S�Ɉ�v���Ȃ��ꍇ�����邽�߁j
        controller.Move(endPosition - transform.position);
    }

    /// <summary>
    /// �ߐڍU�����Ɍ��݌����Ă�������֒Z���ːi����R���[�`���i�^�[�Q�b�g�����Ȃ��ꍇ�j
    /// </summary>
    private System.Collections.IEnumerator MeleeDashInCurrentDirection()
    {
        Vector3 startPosition = transform.position;
        Vector3 endPosition = startPosition + transform.forward * meleeDashDistance;

        float elapsedTime = 0f;

        while (elapsedTime < meleeDashDuration)
        {
            Vector3 currentMove = Vector3.Lerp(startPosition, endPosition, elapsedTime / meleeDashDuration) - transform.position;
            controller.Move(currentMove);

            elapsedTime += Time.deltaTime;
            yield return null;
        }
        controller.Move(endPosition - transform.position);
    }
}