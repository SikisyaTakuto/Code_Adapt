using UnityEngine;

public class PropellerRotator : MonoBehaviour
{
    [Header("回転速度")]
    public float rotationSpeed = 1000f; // 度/秒

    void Update()
    {
        // ローカルのZ軸を中心に回転させる
        transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
    }
}