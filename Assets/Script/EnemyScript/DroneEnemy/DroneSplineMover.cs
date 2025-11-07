using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 設定されたウェイポイント（スプラインパス）に沿ってドローンを移動させます。
/// </summary>
public class DroneSplineMover : MonoBehaviour
{
    [Header("Spline Settings")]
    [Tooltip("移動経路となるTransform（空のGameObjectなど）のリスト")]
    public List<Transform> waypoints = new List<Transform>();
    [Tooltip("パス上の移動速度")]
    public float moveSpeed = 5.0f;
    [Tooltip("次のウェイポイントへ到達したと見なす距離")]
    public float arrivalDistance = 0.5f;
    [Tooltip("ウェイポイント間の移動時にドローンが向く速度")]
    public float rotationSpeed = 5.0f;

    [Header("Looping")]
    [Tooltip("パスの最後まで到達した後、最初に戻るか")]
    public bool isLooping = true;

    private int _currentWaypointIndex = 0;
    private bool _isMovingForward = true;

    void Start()
    {
        if (waypoints == null || waypoints.Count == 0)
        {
            Debug.LogError("ウェイポイントが設定されていません。ドローンは移動しません。");
            enabled = false;
        }
        // ドローンを最初のウェイポイントに配置 (オプション)
        if (waypoints.Count > 0)
        {
            transform.position = waypoints[0].position;
        }
    }

    void Update()
    {
        if (waypoints.Count == 0) return;

        MoveAlongPath();
    }

    private void MoveAlongPath()
    {
        // 現在の目標地点を取得
        Vector3 targetPosition = waypoints[_currentWaypointIndex].position;

        // 1. 移動: 目標地点へ向かって移動
        Vector3 direction = (targetPosition - transform.position).normalized;
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            moveSpeed * Time.deltaTime
        );

        // 2. 回転: 目標地点の方向へ滑らかに向く
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }

        // 3. ウェイポイントへの到達チェック
        if (Vector3.Distance(transform.position, targetPosition) < arrivalDistance)
        {
            UpdateWaypointIndex();
        }
    }

    private void UpdateWaypointIndex()
    {
        if (_isMovingForward)
        {
            _currentWaypointIndex++;
            if (_currentWaypointIndex >= waypoints.Count)
            {
                if (isLooping)
                {
                    // ループ: 最初に戻る
                    _currentWaypointIndex = 0;
                }
                else
                {
                    // ピンポン: 方向を反転
                    _isMovingForward = false;
                    _currentWaypointIndex = waypoints.Count - 2; // 最後の要素の1つ前に戻る
                    if (_currentWaypointIndex < 0) _currentWaypointIndex = 0;
                }
            }
        }
        else // _isMovingForward == false (ピンポンモードで逆行中)
        {
            _currentWaypointIndex--;
            if (_currentWaypointIndex < 0)
            {
                _isMovingForward = true;
                _currentWaypointIndex = 1; // 最初の要素の1つ次に進む
                if (_currentWaypointIndex >= waypoints.Count) _currentWaypointIndex = 0;
            }
        }
    }
}