using Unity.Cinemachine;
using UnityEngine;

public class FlyEnemyMove : MonoBehaviour
{
    [SerializeField] CinemachineSplineCart cinemachineSplineCart;
    [SerializeField] float cartSpeed;

    // Update is called once per frame
    void Update()
    {
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
}
