using UnityEngine;

public class TPSCamera : MonoBehaviour
{
    public Transform target;
    public float distance = 5f;
    public float sensitivity = 2f;
    public float lookAtHeightOffset = 1.5f; // íçéãì_ÇÃçÇÇ≥Çí«â¡
    private float angleX = 0f;
    private float angleY = 0f;

    void Update()
    {
        angleX += Input.GetAxis("Mouse X") * sensitivity;
        angleY -= Input.GetAxis("Mouse Y") * sensitivity;
        angleY = Mathf.Clamp(angleY, -20f, 80f);

        Quaternion rotation = Quaternion.Euler(angleY, angleX, 0);
        Vector3 targetLookAt = target.position + Vector3.up * lookAtHeightOffset; // çÇÇ≥í≤êÆ
        Vector3 position = targetLookAt - (rotation * Vector3.forward * distance);

        transform.position = position;
        transform.LookAt(targetLookAt); // í≤êÆå„ÇÃíçéãì_Ç…LookAt
    }
}
