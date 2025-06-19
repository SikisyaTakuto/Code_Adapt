using UnityEngine;

public class TPSCamera : MonoBehaviour
{
    public Transform target;
    public float distance = 5f;
    public float sensitivity = 2f;
    public float lookAtHeightOffset = 1.5f; // �����_�̍�����ǉ�
    private float angleX = 0f;
    private float angleY = 0f;

    void Update()
    {
        angleX += Input.GetAxis("Mouse X") * sensitivity;
        angleY -= Input.GetAxis("Mouse Y") * sensitivity;
        angleY = Mathf.Clamp(angleY, -20f, 80f);

        Quaternion rotation = Quaternion.Euler(angleY, angleX, 0);
        Vector3 targetLookAt = target.position + Vector3.up * lookAtHeightOffset; // ��������
        Vector3 position = targetLookAt - (rotation * Vector3.forward * distance);

        transform.position = position;
        transform.LookAt(targetLookAt); // ������̒����_��LookAt
    }
}
