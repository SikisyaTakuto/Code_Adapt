using UnityEngine;

public class HeavyMissile : MonoBehaviour
{
    [Header("Missile Settings")]
    [Tooltip("�~�T�C�����^����_���[�W�ʁB")]
    public float damageAmount = 100f; // ���̃~�T�C�����^����_���[�W
    [Tooltip("�~�T�C���̈ړ����x�B")]
    public float missileSpeed = 15f; // �~�T�C���̑��x
    [Tooltip("�~�T�C�����^�[�Q�b�g��ǔ����鎞�ԁB")]
    public float trackingDuration = 3.0f; // �����ǔ����鎞��
    [Tooltip("�~�T�C�������ł���܂ł̑����ԁB")]
    public float lifeTime = 5.0f; // �~�T�C���̎����i�ǔ����ԁ{���j
    [Tooltip("�~�T�C�����_���[�W��^����Ώۂ̃^�O�B")]
    public string targetTag = "Player"; // �����蔻��̑ΏۂƂ���^�O
    [Tooltip("�~�T�C���̐��񑬓x�i�ǔ��̊��炩���j�B")]
    public float turnSpeed = 5f; // �ǔ����̐��񑬓x

    private Transform target; // �ǔ�����^�[�Q�b�g�i�v���C���[�j
    private float trackingTimer; // �ǔ����Ԍv���p

    void Awake()
    {
        // �v���C���[�I�u�W�F�N�g���^�O�Ō������ă^�[�Q�b�g�ɐݒ�
        GameObject playerObject = GameObject.FindGameObjectWithTag(targetTag);
        if (playerObject != null)
        {
            target = playerObject.transform;
        }
        else
        {
            Debug.LogWarning($"�~�T�C���̃^�[�Q�b�g ({targetTag}) ��������܂���B�~�T�C���͒ǔ����܂���B");
        }
    }

    void Start()
    {
        trackingTimer = trackingDuration; // �ǔ��^�C�}�[��������
        Destroy(gameObject, lifeTime); // �~�T�C���̎�����ݒ�
    }

    void Update()
    {
        // �ǔ����ԓ����^�[�Q�b�g�����݂���ꍇ
        if (trackingTimer > 0 && target != null)
        {
            // �^�[�Q�b�g�����֏��X�ɉ�]
            Vector3 directionToTarget = (target.position - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(directionToTarget);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * turnSpeed);

            trackingTimer -= Time.deltaTime; // �^�C�}�[�����炷
        }

        // �~�T�C����O���Ɉړ�������
        transform.position += transform.forward * missileSpeed * Time.deltaTime;
    }

    void OnTriggerEnter(Collider other)
    {
        // �Փ˂����I�u�W�F�N�g�̃^�O���^�[�Q�b�g�^�O�ƈ�v���邩�m�F
        if (other.CompareTag(targetTag))
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damageAmount);
                Debug.Log($"�~�T�C���� {other.name} �� {damageAmount} �_���[�W��^���܂����B");
            }
            // �~�T�C���͏Փ˂��������
            Destroy(gameObject);
        }
    }

    // �f�o�b�O�\���p
    void OnDrawGizmos()
    {
        if (target != null && trackingTimer > 0)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, target.position);
        }
    }
}