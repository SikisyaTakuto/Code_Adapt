using UnityEngine;

public class TPSCameraController : MonoBehaviour
{
    public Transform target; // �Ǐ]����^�[�Q�b�g�i�v���C���[�j
    public float distance = 5.0f; // �^�[�Q�b�g����̋���
    public float height = 2.0f;   // �^�[�Q�b�g����̍���
    public float rotationSpeed = 3.0f; // �J�����̉�]���x�i�}�E�X���x�j
    public float smoothSpeed = 10.0f; // �J�����̈ړ��E��]�̂Ȃ߂炩��

    public Vector2 pitchMinMax = new Vector2(-40, 85); // �c�����̃J�����p�x����

    public LayerMask collisionLayers; // �J�������Փ˂��`�F�b�N���郌�C���[�i�ǁA�n�ʂȂǁj
    public float collisionOffset = 0.2f; // �Փˎ��ɃJ�������ǂꂾ����O�ɂ��炷��

    private float yaw = 0.0f;   // ���E�̉�]�p�x (Y��)
    private float pitch = 0.0f; // �㉺�̉�]�p�x (X��)

    void Start()
    {
        // �J�[�\�������b�N���Ĕ�\���ɂ���
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // �����p�x��ݒ� (�v���C���[�̌����ɍ��킹��)
        if (target != null)
        {
            Vector3 relativePos = transform.position - target.position;
            Quaternion rotation = Quaternion.LookRotation(relativePos);
            yaw = rotation.eulerAngles.y;
            pitch = rotation.eulerAngles.x;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        // �}�E�X���͂ŃJ�����̊p�x���X�V
        yaw += Input.GetAxis("Mouse X") * rotationSpeed;
        pitch -= Input.GetAxis("Mouse Y") * rotationSpeed;
        pitch = Mathf.Clamp(pitch, pitchMinMax.x, pitchMinMax.y); // �㉺�p�x�𐧌�

        // �J�����̖ڕW��] (Euler angles����Quaternion�ɕϊ�)
        Quaternion targetRotation = Quaternion.Euler(pitch, yaw, 0);

        // �J�����̖ڕW�ʒu���v�Z
        Vector3 targetPosition = target.position + Vector3.up * height - targetRotation * Vector3.forward * distance;

        // �J�����̏Փ˔���
        RaycastHit hit;
        Vector3 currentTargetPos = target.position + Vector3.up * height; // �^�[�Q�b�g�̍������܂񂾈ʒu
        if (Physics.Linecast(currentTargetPos, targetPosition, out hit, collisionLayers))
        {
            // �Փ˂����ꍇ�A�Փ˓_���班����O�ɃJ������z�u
            targetPosition = hit.point + hit.normal * collisionOffset;
        }

        // �J�����̈ʒu�Ɖ�]��Lerp�ŃX���[�Y�ɕ��
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * smoothSpeed);
    }

    // �v���C���[�̌������J�����̐��������ɍ��킹�邽�߂̃��\�b�h
    public void RotatePlayerToCameraDirection()
    {
        if (target == null) return;

        // �v���C���[��Y����]�݂̂��J������Y����]�ɍ��킹��
        Quaternion playerRotation = Quaternion.Euler(0, yaw, 0);
        target.rotation = Quaternion.Slerp(target.rotation, playerRotation, Time.deltaTime * smoothSpeed);
    }
}