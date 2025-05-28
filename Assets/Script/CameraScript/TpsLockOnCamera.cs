using UnityEngine;
using System.Collections.Generic;

public class TpsLockOnCamera : MonoBehaviour
{
    [SerializeField] private Transform _attachTarget = null;            // �J�������Ǐ]����L�����N�^�[��Transform
    [SerializeField] private Vector3 _attachOffset = new Vector3(0f, 2f, -5f);  // �L�����N�^�[����̃J�����̃I�t�Z�b�g�ʒu

    [SerializeField] private Vector3 _defaultLookPosition = Vector3.zero;       // �^�[�Q�b�g�����Ȃ����̒����_

    [SerializeField] private float _changeDuration = 0.1f;            // ���b�N�I���^�[�Q�b�g�؂�ւ����̕�Ԏ���

    private float _timer = 0f;                                        // ��ԗp�^�C�}�[
    private Vector3 _lookTargetPosition = Vector3.zero;               // ���݂̒����_�̈ʒu
    private Vector3 _latestTargetPosition = Vector3.zero;             // ���O�̒����_�̈ʒu�i��ԊJ�n�n�_�j

    // --- �t���[�J�����p�ϐ� ---
    [SerializeField] private float mouseSensitivity = 3f;             // �}�E�X���x
    [SerializeField] private float verticalAngleMin = -30f;           // �J�����̏㉺��]�ŏ��p�x
    [SerializeField] private float verticalAngleMax = 60f;            // �J�����̏㉺��]�ő�p�x
    private float rotationX = 0f;                                     // �J������X����]�p�x�i�㉺�j
    private float rotationY = 0f;                                     // �J������Y����]�p�x�i���E�j

    // ���[�h�ؑփt���O�itrue�Ȃ烍�b�N�I�����[�h�Afalse�Ȃ�t���[���[�h�j
    private bool isLockOnMode = false;

    // ���b�N�I���ΏۊǗ��p
    [SerializeField] private string enemyTag = "Enemy";               // �G�̃^�O���i�Ώ۔���Ɏg�p�j
    [SerializeField] private float lockOnRange = 20f;                 // ���b�N�I���\�Ȕ͈�

    private List<Transform> lockOnTargets = new List<Transform>();    // ���b�N�I���\�ȑΏۂ̃��X�g
    private int currentTargetIndex = -1;                              // ���݂̃��b�N�I���Ώۂ̃C���f�b�N�X
    private Transform _lookTarget = null;                             // ���ݒ������Ă���^�[�Q�b�g

    private void Start()
    {
        // �J�����̏����p�x���擾
        Vector3 angles = transform.eulerAngles;
        rotationY = angles.y;
        rotationX = angles.x;

        // �����_��������
        _lookTargetPosition = _defaultLookPosition;
        _latestTargetPosition = _lookTargetPosition;
    }

    private void Update()
    {
        // ���t���[�����b�N�I���Ώۂ��X�V�i�͈͓��̓G��T���j
        UpdateLockOnTargets();

        // Tab�L�[�Ń��b�N�I�����[�h�ƃt���[�J�������[�h��؂�ւ�
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            isLockOnMode = !isLockOnMode;
            Debug.Log("Camera Mode: " + (isLockOnMode ? "LockOn" : "Free"));

            if (!isLockOnMode)
            {
                // �t���[�J�������[�h�ɐ؂�ւ�����^�[�Q�b�g������
                ClearLockOn();
            }
            else
            {
                // ���b�N�I�����[�h�J�n���͍ŏ��̃^�[�Q�b�g���Z�b�g
                SetNextTarget();
            }
        }

        // ���b�N�I�����[�h���ɉE�N���b�N�Ŏ��̃^�[�Q�b�g�ɐ؂�ւ�
        if (isLockOnMode && Input.GetMouseButtonDown(1))
        {
            SetNextTarget();
        }

        if (!isLockOnMode)
        {
            // �t���[�J������������s
            FreeCameraUpdate();
        }
        else
        {
            if (_lookTarget == null)
            {
                // ���b�N�I���Ώۂ����Ȃ��Ȃ����玩���Ńt���[�J�������[�h�ɖ߂�
                isLockOnMode = false;
                ClearLockOn();
            }
            else
            {
                // ���b�N�I�����̃^�[�Q�b�g�Ƃ̋������`�F�b�N
                float dist = Vector3.Distance(_attachTarget.position, _lookTarget.position);
                if (dist > lockOnRange)
                {
                    // ���������ꂷ�����烍�b�N�������ăt���[���[�h�ɖ߂�
                    Debug.Log("LockOn target out of range. Returning to free mode.");
                    isLockOnMode = false;
                    ClearLockOn();
                }
            }
        }
    }

    private void LateUpdate()
    {
        if (isLockOnMode)
        {
            // ���b�N�I���J�����̈ʒu�E�������X�V
            LockOnCameraUpdate();
        }
        else
        {
            // �t���[�J�����̈ʒu�E�������X�V
            FreeCameraLateUpdate();
        }
    }

    /// <summary>
    /// �t���[�J�����̉�]���}�E�X���͂���v�Z����
    /// </summary>
    private void FreeCameraUpdate()
    {
        rotationY += Input.GetAxis("Mouse X") * mouseSensitivity;
        rotationX -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        rotationX = Mathf.Clamp(rotationX, verticalAngleMin, verticalAngleMax);
    }

    /// <summary>
    /// �t���[�J�����̈ʒu�E������ݒ肷��
    /// </summary>
    private void FreeCameraLateUpdate()
    {
        Quaternion rotation = Quaternion.Euler(rotationX, rotationY, 0f);
        Vector3 position = _attachTarget.position + rotation * _attachOffset;

        transform.SetPositionAndRotation(position, rotation);
    }

    /// <summary>
    /// ���b�N�I���J�����̈ʒu�E�������X�V����
    /// </summary>
    private void LockOnCameraUpdate()
    {
        Vector3 targetPosition = _lookTarget != null ? _lookTarget.position : _defaultLookPosition;

        // ���b�N�I���^�[�Q�b�g�̈ʒu�֊��炩�ɕ��
        if (_timer < _changeDuration)
        {
            _timer += Time.deltaTime;
            _lookTargetPosition = Vector3.Lerp(_latestTargetPosition, targetPosition, _timer / _changeDuration);
        }
        else
        {
            _lookTargetPosition = targetPosition;
        }

        // �^�[�Q�b�g�����x�N�g��
        Vector3 targetVector = _lookTargetPosition - _attachTarget.position;
        // �^�[�Q�b�g������������]
        Quaternion targetRotation = targetVector != Vector3.zero ? Quaternion.LookRotation(targetVector) : transform.rotation;

        // �J�����ʒu�̓L�����N�^�[�ʒu�ɃI�t�Z�b�g���|�����ʒu
        Vector3 position = _attachTarget.position + targetRotation * _attachOffset;
        // �J�����̌����̓^�[�Q�b�g����������
        Quaternion rotation = Quaternion.LookRotation(_lookTargetPosition - position);

        transform.SetPositionAndRotation(position, rotation);
    }

    /// <summary>
    /// ���b�N�I���Ώۃ��X�g���X�V�i�͈͓��̓G�������j
    /// </summary>
    private void UpdateLockOnTargets()
    {
        // �w��^�O�̓G�����ׂĎ擾
        GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);
        lockOnTargets.Clear();

        foreach (var enemy in enemies)
        {
            float dist = Vector3.Distance(_attachTarget.position, enemy.transform.position);
            // ���͈͓��Ȃ烍�b�N�I�����ɒǉ�
            if (dist <= lockOnRange)
            {
                lockOnTargets.Add(enemy.transform);
            }
        }

        // ���݂̃^�[�Q�b�g�����X�g�Ɋ܂܂�Ȃ���΃��b�N����
        if (_lookTarget != null && !lockOnTargets.Contains(_lookTarget))
        {
            ClearLockOn();
        }
    }

    /// <summary>
    /// ���b�N�I���Ώۃ��X�g���玟�̃^�[�Q�b�g���Z�b�g����
    /// </summary>
    private void SetNextTarget()
    {
        if (lockOnTargets.Count == 0)
        {
            // �ΏۂȂ��Ȃ烍�b�N����
            ClearLockOn();
            return;
        }

        // �C���f�b�N�X��1�i�߂ă��[�v
        currentTargetIndex++;
        if (currentTargetIndex >= lockOnTargets.Count)
        {
            currentTargetIndex = 0;
        }

        ChangeTarget(lockOnTargets[currentTargetIndex]);
    }

    /// <summary>
    /// ���b�N�I���^�[�Q�b�g��ύX����
    /// </summary>
    /// <param name="target">�V�����^�[�Q�b�g</param>
    private void ChangeTarget(Transform target)
    {
        _latestTargetPosition = _lookTargetPosition;
        _lookTarget = target;
        _timer = 0f;
        isLockOnMode = true;

        Debug.Log($"LockOn Target: {_lookTarget.name}");
    }

    /// <summary>
    /// ���b�N�I����������
    /// </summary>
    private void ClearLockOn()
    {
        _lookTarget = null;
        currentTargetIndex = -1;
        _timer = 0f;
        _latestTargetPosition = _defaultLookPosition;
        _lookTargetPosition = _defaultLookPosition;
    }
}
