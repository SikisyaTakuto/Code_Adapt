using Unity.Cinemachine;
using UnityEngine;
using System.Collections;

public class FlyEnemyMove : MonoBehaviour
{
    [SerializeField] CinemachineSplineCart cinemachineSplineCart;
    [SerializeField] float cartSpeed;

    // 死亡した場合のスクリプト
    public EnemyDaed enemyDaed;

    void Start()
    {

    }

    void Update()
    {
        if (!enemyDaed.Dead)
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
        else
        {
            // その場で死亡する
            StartCoroutine(ZeroSpeed());
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
            cartSpeed = 1000f;
        }
    }

    private IEnumerator ZeroSpeed()
    {
        yield return new WaitForSeconds(5);
        Destroy(gameObject);
    }
}
