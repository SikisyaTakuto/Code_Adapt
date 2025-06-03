using Unity.Cinemachine;
using UnityEngine;

public class FlyEnemyMove : MonoBehaviour
{
    [SerializeField] CinemachineSplineCart cinemachineSplineCart;
    [SerializeField] float cartSpeed;
   // public Transform target;

    void Update()
    {
       // Vector3 targetPos = target.position;
       // targetPos.y = transform.position.y;

        if (cinemachineSplineCart != null)
        {
            // スプラインの長さを取得
            float splineLength = cinemachineSplineCart.Spline.CalculateLength();

            // 経路上の現在位置（0〜1）を時間経過で進める
            float deltaDistance = cartSpeed * Time.deltaTime;
            float deltaT = deltaDistance / splineLength;

            cinemachineSplineCart.SplinePosition += deltaT;
        }
    }

    // Playerが近づいた場合
    public void OnTriggerEnter(Collider collider)
    {
        // Playerが範囲内に入ったとき
        if (collider.gameObject.tag == "Player")
        {
            // 動きを止める
            cartSpeed = 0f;
        }
    }

    // Playerが離れた場合
    public void OnTriggerExit(Collider collider)
    {
        // Playerが範囲外に出たとき
        if (collider.gameObject.tag == "Player")
        {
            // 動き始める
            cartSpeed = 2000f;
        }
    }
}
