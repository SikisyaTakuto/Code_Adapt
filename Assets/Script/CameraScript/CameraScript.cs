using UnityEngine;

public class CameraScript : MonoBehaviour
{
    public Transform player;               // �v���C���[�̈ʒu�i�J�����̉�]���S�j
    public float mouseSensitivity = 2f;   // �}�E�X���x
    public float distanceFromPlayer = 5f; // �v���C���[����J�����܂ł̋���
    public float verticalAngleMin = -30f; // �J�����㉺��]�̍ŏ��p�x�i�����������j
    public float verticalAngleMax = 60f;  // �J�����㉺��]�̍ő�p�x�i����������j

    public float lockOnRange = 15f;       // ���b�N�I���Ώی��o����
    public string enemyTag = "Enemy";     // �G�̃^�O��

    private float rotationX = 0f;         // ������]�p�x�i�㉺�j
    private float rotationY = 0f;         // ������]�p�x�i���E�j

    private Transform lockedTarget = null;    // ���݃��b�N�I�����̓G��Transform
    private int lockOnIndex = -1;              // ���b�N�I�����̓G�̃C���f�b�N�X
    private Transform[] targets;               // ���b�N�I���\�ȓG�ꗗ

    void Start()
    {
        // �J�����̏�����]�p�x���擾
        Vector3 angles = transform.eulerAngles;
        rotationY = angles.y;
        rotationX = angles.x;
    }

    void Update()
    {
        if (lockedTarget != null)
        {
            // ���b�N�I�����̓G���J��������Ղ��Ă��Ȃ����m�F
            Vector3 targetHeadPos = lockedTarget.position + Vector3.up * 1.5f; // �G�̓��������_��
            Vector3 camToTarget = targetHeadPos - transform.position;

            RaycastHit hit;
            // �J�����ʒu����G������Raycast�i�Օ�������j
            if (Physics.Raycast(transform.position, camToTarget.normalized, out hit, lockOnRange))
            {
                if (hit.transform != lockedTarget)
                {
                    // �G�ȊO�̃I�u�W�F�N�g�ɎՂ��Ă���ꍇ�̓��b�N�I������
                    lockedTarget = null;
                    lockOnIndex = -1;
                }
            }
            else
            {
                // Raycast�œG�ɓ�����Ȃ������ꍇ�����b�N����
                lockedTarget = null;
                lockOnIndex = -1;
            }
        }

        if (lockedTarget == null)
        {
            // ���b�N�I�����Ă��Ȃ���΃}�E�X����Ŏ��_��]���J�����ʒu�X�V
            LookAround();
            UpdateCameraPosition();
        }
        else
        {
            // ���b�N�I�����͓G��Ǐ]���ăJ�����ʒu�E�������X�V
            LockOnLook();
        }

        // �E�N���b�N�Ń��b�N�I���؂�ւ�
        if (Input.GetMouseButtonDown(1))
        {
            LockOnNextTarget();
        }
    }

    // �}�E�X���͂Ŏ��R�Ɏ��_��]�����鏈��
    void LookAround()
    {
        rotationY += Input.GetAxis("Mouse X") * mouseSensitivity; // ����������]
        rotationX -= Input.GetAxis("Mouse Y") * mouseSensitivity; // ����������]�i�㉺���]���Ӂj
        rotationX = Mathf.Clamp(rotationX, verticalAngleMin, verticalAngleMax); // ��]�p�x����
    }

    // �v���C���[�𒆐S�ɃJ�����ʒu���X�V���鏈��
    void UpdateCameraPosition()
    {
        Quaternion rotation = Quaternion.Euler(rotationX, rotationY, 0);
        Vector3 offset = rotation * new Vector3(0, 0, -distanceFromPlayer); // �v���C���[�̌��ɔz�u
        transform.position = player.position + offset;
        transform.LookAt(player.position + Vector3.up * 1.5f); // �v���C���[�̓��t�߂𒍎�
    }

    // ���b�N�I���Ώۂ̓G�����o���A�J��������Ղ��Ă��Ȃ��G�����𒊏o���鏈��
    void UpdateTargets()
    {
        // �V�[�����̑S�Ă̓G���擾
        GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);

        var closeEnemies = new System.Collections.Generic.List<Transform>();
        foreach (var e in enemies)
        {
            float dist = Vector3.Distance(player.position, e.transform.position);

            if (dist <= lockOnRange) // ���b�N�I���͈͓��Ȃ�
            {
                Vector3 targetHeadPos = e.transform.position + Vector3.up * 1.5f; // �G�̓��t��
                Vector3 camToTarget = targetHeadPos - transform.position;

                RaycastHit hit;
                // �J��������G�̓���Raycast���΂��A�Օ������Ȃ����`�F�b�N
                if (Physics.Raycast(transform.position, camToTarget.normalized, out hit, lockOnRange))
                {
                    if (hit.transform == e.transform)
                    {
                        Debug.DrawLine(transform.position, targetHeadPos, Color.green, 0.1f); // �����F��
                        closeEnemies.Add(e.transform);
                    }
                    else
                    {
                        Debug.DrawLine(transform.position, hit.point, Color.yellow, 0.1f); // �Ղ��Ă�F���F
                    }
                }
                else
                {
                    Debug.DrawRay(transform.position, camToTarget.normalized * lockOnRange, Color.gray, 0.1f); // ������Ȃ��F�D�F
                }
            }
        }

        // �������߂����Ƀ\�[�g
        closeEnemies.Sort((a, b) =>
            Vector3.Distance(player.position, a.position).CompareTo(
            Vector3.Distance(player.position, b.position)));

        targets = closeEnemies.ToArray();
    }

    // ���b�N�I���\�ȓG�̒��Ŏ��̓G�Ƀ��b�N�I���؂�ւ����s������
    void LockOnNextTarget()
    {
        UpdateTargets();

        if (targets.Length == 0)
        {
            // ���b�N�I���\�ȓG�����Ȃ���΃��b�N����
            lockedTarget = null;
            lockOnIndex = -1;
            return;
        }

        lockOnIndex++;
        if (lockOnIndex >= targets.Length)
        {
            // ���ׂĂ̓G��؂�ւ����烍�b�N����
            lockedTarget = null;
            lockOnIndex = -1;
            return;
        }

        // ���b�N�I���Ώۂ��X�V
        lockedTarget = targets[lockOnIndex];

        // �y�������烍�b�N�I���J�n���̃R�����g�Ə����z
        Debug.Log($"���b�N�I���Ώۂ�ύX���܂���: {lockedTarget.name}");  // �R���\�[���Ƀ��b�N�I���Ώۂ̖��O��\��
                                                            // �����Ń��b�N�I�����̃G�t�F�N�g�Đ���UI�X�V�Ȃǂ��ǉ��\
    }

    // ���b�N�I�����̓G�ɃJ�����������鏈��
    void LockOnLook()
    {
        if (lockedTarget == null) return;

        // �G�̈ʒu�𒆐S�ɁA�v���C���[�Ƃ̋�����ۂ悤�ɃJ������z�u
        Vector3 targetHeadPos = lockedTarget.position + Vector3.up * 1.5f;
        Vector3 toEnemy = (targetHeadPos - player.position).normalized;

        // �v���C���[�̔w�ʂł͂Ȃ��A�G�𒆐S�ɂ����p�x����ǔ�����悤�ɃJ������z�u
        Vector3 cameraPos = player.position - toEnemy * distanceFromPlayer + Vector3.up * 2f;
        transform.position = cameraPos;

        // �J�����͏�ɓG�𒍎�
        transform.LookAt(targetHeadPos);

        // �v���C���[���G�̕����ɉ�]������
        Vector3 lookDir = (lockedTarget.position - player.position).normalized;
        lookDir.y = 0;
        if (lookDir.sqrMagnitude > 0.01f)
            player.rotation = Quaternion.Slerp(player.rotation, Quaternion.LookRotation(lookDir), 0.2f);
    }
}
