using UnityEngine;
using System.Collections.Generic;

public class TpsLockOnCamera : MonoBehaviour
{
    [SerializeField] private Transform _attachTarget = null;            // カメラが追従するキャラクターのTransform
    [SerializeField] private Vector3 _attachOffset = new Vector3(0f, 2f, -5f);  // キャラクターからのカメラのオフセット位置

    [SerializeField] private Vector3 _defaultLookPosition = Vector3.zero;       // ターゲットがいない時の注視点

    [SerializeField] private float _changeDuration = 0.1f;            // ロックオンターゲット切り替え時の補間時間

    private float _timer = 0f;                                        // 補間用タイマー
    private Vector3 _lookTargetPosition = Vector3.zero;               // 現在の注視点の位置
    private Vector3 _latestTargetPosition = Vector3.zero;             // 直前の注視点の位置（補間開始地点）

    // --- フリーカメラ用変数 ---
    [SerializeField] private float mouseSensitivity = 3f;             // マウス感度
    [SerializeField] private float verticalAngleMin = -30f;           // カメラの上下回転最小角度
    [SerializeField] private float verticalAngleMax = 60f;            // カメラの上下回転最大角度
    private float rotationX = 0f;                                     // カメラのX軸回転角度（上下）
    private float rotationY = 0f;                                     // カメラのY軸回転角度（左右）

    // モード切替フラグ（trueならロックオンモード、falseならフリーモード）
    private bool isLockOnMode = false;

    // ロックオン対象管理用
    [SerializeField] private string enemyTag = "Enemy";               // 敵のタグ名（対象判定に使用）
    [SerializeField] private float lockOnRange = 20f;                 // ロックオン可能な範囲

    private List<Transform> lockOnTargets = new List<Transform>();    // ロックオン可能な対象のリスト
    private int currentTargetIndex = -1;                              // 現在のロックオン対象のインデックス
    private Transform _lookTarget = null;                             // 現在注視しているターゲット

    private void Start()
    {
        // カメラの初期角度を取得
        Vector3 angles = transform.eulerAngles;
        rotationY = angles.y;
        rotationX = angles.x;

        // 注視点を初期化
        _lookTargetPosition = _defaultLookPosition;
        _latestTargetPosition = _lookTargetPosition;
    }

    private void Update()
    {
        // 毎フレームロックオン対象を更新（範囲内の敵を探す）
        UpdateLockOnTargets();

        // Tabキーでロックオンモードとフリーカメラモードを切り替え
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            isLockOnMode = !isLockOnMode;
            Debug.Log("Camera Mode: " + (isLockOnMode ? "LockOn" : "Free"));

            if (!isLockOnMode)
            {
                // フリーカメラモードに切り替えたらターゲットを解除
                ClearLockOn();
            }
            else
            {
                // ロックオンモード開始時は最初のターゲットをセット
                SetNextTarget();
            }
        }

        // ロックオンモード時に右クリックで次のターゲットに切り替え
        if (isLockOnMode && Input.GetMouseButtonDown(1))
        {
            SetNextTarget();
        }

        if (!isLockOnMode)
        {
            // フリーカメラ操作を実行
            FreeCameraUpdate();
        }
        else
        {
            if (_lookTarget == null)
            {
                // ロックオン対象がいなくなったら自動でフリーカメラモードに戻す
                isLockOnMode = false;
                ClearLockOn();
            }
            else
            {
                // ロックオン中のターゲットとの距離をチェック
                float dist = Vector3.Distance(_attachTarget.position, _lookTarget.position);
                if (dist > lockOnRange)
                {
                    // 距離が離れすぎたらロック解除してフリーモードに戻す
                    Debug.Log("LockOn target out of range. Returning to free mode.");
                    isLockOnMode = false;
                    ClearLockOn();
                }
            }
        }
    }

    private void LateUpdate()
    {
        if (isLockOnMode)
        {
            // ロックオンカメラの位置・向きを更新
            LockOnCameraUpdate();
        }
        else
        {
            // フリーカメラの位置・向きを更新
            FreeCameraLateUpdate();
        }
    }

    /// <summary>
    /// フリーカメラの回転をマウス入力から計算する
    /// </summary>
    private void FreeCameraUpdate()
    {
        rotationY += Input.GetAxis("Mouse X") * mouseSensitivity;
        rotationX -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        rotationX = Mathf.Clamp(rotationX, verticalAngleMin, verticalAngleMax);
    }

    /// <summary>
    /// フリーカメラの位置・向きを設定する
    /// </summary>
    private void FreeCameraLateUpdate()
    {
        Quaternion rotation = Quaternion.Euler(rotationX, rotationY, 0f);
        Vector3 position = _attachTarget.position + rotation * _attachOffset;

        transform.SetPositionAndRotation(position, rotation);
    }

    /// <summary>
    /// ロックオンカメラの位置・向きを更新する
    /// </summary>
    private void LockOnCameraUpdate()
    {
        Vector3 targetPosition = _lookTarget != null ? _lookTarget.position : _defaultLookPosition;

        // ロックオンターゲットの位置へ滑らかに補間
        if (_timer < _changeDuration)
        {
            _timer += Time.deltaTime;
            _lookTargetPosition = Vector3.Lerp(_latestTargetPosition, targetPosition, _timer / _changeDuration);
        }
        else
        {
            _lookTargetPosition = targetPosition;
        }

        // ターゲット方向ベクトル
        Vector3 targetVector = _lookTargetPosition - _attachTarget.position;
        // ターゲット方向を向く回転
        Quaternion targetRotation = targetVector != Vector3.zero ? Quaternion.LookRotation(targetVector) : transform.rotation;

        // カメラ位置はキャラクター位置にオフセットを掛けた位置
        Vector3 position = _attachTarget.position + targetRotation * _attachOffset;
        // カメラの向きはターゲット方向を向く
        Quaternion rotation = Quaternion.LookRotation(_lookTargetPosition - position);

        transform.SetPositionAndRotation(position, rotation);
    }

    /// <summary>
    /// ロックオン対象リストを更新（範囲内の敵を検索）
    /// </summary>
    private void UpdateLockOnTargets()
    {
        // 指定タグの敵をすべて取得
        GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);
        lockOnTargets.Clear();

        foreach (var enemy in enemies)
        {
            float dist = Vector3.Distance(_attachTarget.position, enemy.transform.position);
            // 一定範囲内ならロックオン候補に追加
            if (dist <= lockOnRange)
            {
                lockOnTargets.Add(enemy.transform);
            }
        }

        // 現在のターゲットがリストに含まれなければロック解除
        if (_lookTarget != null && !lockOnTargets.Contains(_lookTarget))
        {
            ClearLockOn();
        }
    }

    /// <summary>
    /// ロックオン対象リストから次のターゲットをセットする
    /// </summary>
    private void SetNextTarget()
    {
        if (lockOnTargets.Count == 0)
        {
            // 対象なしならロック解除
            ClearLockOn();
            return;
        }

        // インデックスを1つ進めてループ
        currentTargetIndex++;
        if (currentTargetIndex >= lockOnTargets.Count)
        {
            currentTargetIndex = 0;
        }

        ChangeTarget(lockOnTargets[currentTargetIndex]);
    }

    /// <summary>
    /// ロックオンターゲットを変更する
    /// </summary>
    /// <param name="target">新しいターゲット</param>
    private void ChangeTarget(Transform target)
    {
        _latestTargetPosition = _lookTargetPosition;
        _lookTarget = target;
        _timer = 0f;
        isLockOnMode = true;

        Debug.Log($"LockOn Target: {_lookTarget.name}");
    }

    /// <summary>
    /// ロックオン解除処理
    /// </summary>
    private void ClearLockOn()
    {
        _lookTarget = null;
        currentTargetIndex = -1;
        _timer = 0f;
        _latestTargetPosition = _defaultLookPosition;
        _lookTargetPosition = _defaultLookPosition;
    }
}
