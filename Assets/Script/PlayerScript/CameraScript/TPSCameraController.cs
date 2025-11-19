using UnityEngine;
using System.Linq; // LINQを使用するため追加

/// <summary>
/// ターゲットを追跡し、マウス入力で操作可能な三人称視点（TPS）カメラを制御します。
/// カメラ衝突とロックオン/ターゲット切り替え機能に対応しています。
/// </summary>
public class TPSCameraController : MonoBehaviour
{
    [Header("Target and Distance")]
    [Tooltip("カメラが追従するターゲット（通常はプレイヤー）のTransform。")]
    public Transform target; // 追従するターゲット（プレイヤーなど）

    [Tooltip("ターゲットの中心からカメラまでの理想的な距離。")]
    public float distance = 5.0f; // ターゲットからの距離

    [Tooltip("ターゲットの中心からカメラまでの相対的な高さ。")]
    public float height = 2.0f;    // ターゲットからの高さ

    [Header("Camera Control")]
    [Tooltip("マウス入力によるカメラの回転速度（感度）。")]
    public float rotationSpeed = 3.0f; // カメラの回転速度（マウス速度）

    [Tooltip("ターゲット位置と回転に移動する際のスムーズさ（値が大きいほど速い）。")]
    public float smoothSpeed = 10.0f; // カメラの移動と回転のスムーズさ

    [Tooltip("垂直方向（上下）のカメラの角度制限。Xが最小値（下）、Yが最大値（上）。")]
    public Vector2 pitchMinMax = new Vector2(-40, 85); // 垂直方向のカメラ角度制限

    // === ロックオン機能 変更部分 ===
    [Header("Lock-On Settings (Camera)")]
    [Tooltip("ロックオン時のカメラの回転速度。")]
    public float lockOnRotationSpeed = 15f;

    [Tooltip("ロックオンの最大距離。プレイヤーからこの範囲内の敵を検出します。")]
    public float maxLockOnRange = 30f; // プレイヤーの周りの検出範囲

    [Tooltip("ロックオン維持のための最大距離（元の範囲より少し広めに設定）。")]
    public float maxLockOnKeepRange = 40f;

    [Tooltip("敵オブジェクトのレイヤーマスク。")]
    public LayerMask enemyLayer; // 新しく追加

    [Tooltip("ロックオン試行時、敵がプレイヤーの前方方向から何度までの角度範囲内にいる必要があるか (片側)。例: 60で合計120度。")]
    public float lockOnAngleLimit = 60f; // ロックオン可能なプレイヤーの前方角度制限

    [Tooltip("ロックオン切り替え時や解除時に、注視点をスムーズに移行させる時間。")]
    [SerializeField]
    private float _changeDuration = 0.3f; // ロック切り替え時間 (値を調整してスムーズさを制御)

    // === ロックオンの切り替えスムーズ化のための内部変数 ===
    private float _timer = 0f; // ロック切り替えタイマー
    private Vector3 _lookTargetPosition = Vector3.zero; // 現在の注視点 (スムーズ補間後の位置)
    private Vector3 _latestTargetPosition = Vector3.zero; // ロックを移すときの最後の注視点 (補間の開始点)

    // 外部（PlayerControllerなど）から設定されるロックオンターゲット
    private Transform _lockOnTarget = null;
    /// <summary>現在のロックオンターゲットを設定・取得します。設定時にスムーズな切り替えを開始します。</summary>
    public Transform LockOnTarget
    {
        get { return _lockOnTarget; }
        set
        {
            // ターゲットが切り替わる/解除されるときにスムーズ化を開始
            if (_lockOnTarget != value)
            {
                // 現在の注視点（Lerp後の位置）を補間の開始点として記録
                _latestTargetPosition = _lookTargetPosition;
                _lockOnTarget = value;
                _timer = 0f; // タイマーをリセットしてLerpを開始
            }
        }
    }
    // ===================================

    [Header("Collision Settings")]
    [Tooltip("カメラが衝突をチェックするレイヤー。壁や地面などを設定します。")]
    public LayerMask collisionLayers; // カメラが衝突をチェックするレイヤー（壁や地面など）

    [Tooltip("衝突が発生した際、カメラを押し戻す距離のオフセット。")]
    public float collisionOffset = 0.2f; // 衝突時にカメラが押し戻されるオフセット

    // カメラの現在の角度
    private float _yaw = 0.0f;    // 左右の回転角度 (Y軸)
    private float _pitch = 0.0f; // 上下の回転角度 (X軸)

    // 固定ビューモード関連
    private bool _isFixedViewMode = false;
    private Vector3 _fixedTargetPosition;
    private Quaternion _fixedTargetRotation;
    private float _fixedViewSmoothSpeed;

    // カーソルロックの初期状態を保持
    private bool _cursorLockedInitially = false;

    void Start()
    {
        InitializeCursor();
        InitializeCameraAngles();
        // 初期注視点を設定 (ターゲットが存在しない場合はtarget.positionを初期値とする)
        if (target != null)
        {
            _lookTargetPosition = target.position + Vector3.up * 1.5f;
            _latestTargetPosition = _lookTargetPosition;
        }
    }

    /// <summary>
    /// カーソルを初期状態でロックし、非表示にする
    /// </summary>
    private void InitializeCursor()
    {
        // カーソルをロックして非表示にする
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        _cursorLockedInitially = true; // 初期状態でロックしたことを記録
    }

    /// <summary>
    /// カメラの初期角度をターゲットの向きに合わせる
    /// </summary>
    private void InitializeCameraAngles()
    {
        if (target != null)
        {
            // 初期回転を設定 (ターゲットのY軸回転を取得)
            _yaw = target.eulerAngles.y;

            // ピッチ（上下角）は初期状態で水平（0度）またはターゲットの回転から取得
            _pitch = transform.eulerAngles.x;
            if (_pitch > 180) _pitch -= 360; // 負の角度に対応
        }
    }

    void Update()
    {
        if (!_isFixedViewMode && target != null)
        {
            HandleLockOnInput();

            if (_lockOnTarget != null)
            {
                HandleTargetSwitching();
            }
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        // ロックオンターゲットがある場合、注視点をスムーズに更新する
        if (_lockOnTarget != null)
        {
            Vector3 targetLookPosition = _lockOnTarget.position + Vector3.up * 1.5f; // ターゲットの頭付近を注視
            UpdateLookTargetPosition(targetLookPosition);
        }
        else
        {
            // ロックオンが解除された場合、注視点をターゲットの中心に戻す（デフォルトの注視点として扱う）
            Vector3 defaultLookPosition = target.position + Vector3.up * 1.5f;
            UpdateLookTargetPosition(defaultLookPosition);
        }

        if (_isFixedViewMode)
        {
            HandleFixedViewMode();
        }
        else
        {
            HandleTPSViewMode();
        }
    }

    /// <summary>
    /// 注視点をスムーズに移行させます。
    /// </summary>
    /// <param name="targetPosition">最終的な目標注視点。</param>
    private void UpdateLookTargetPosition(Vector3 targetPosition)
    {
        if (_timer < _changeDuration)
        {
            _timer += Time.deltaTime;
            // _latestTargetPosition（移行開始時の位置）から targetPosition（目標位置）へ補間
            _lookTargetPosition = Vector3.Lerp(_latestTargetPosition, targetPosition, _timer / _changeDuration);
        }
        else
        {
            _lookTargetPosition = targetPosition;
        }
    }

    /// <summary>
    /// ロックオン中のターゲット切り替え（マウスホイール）を処理します。
    /// </summary>
    private void HandleTargetSwitching()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll != 0)
        {
            SwitchTarget(scroll > 0);
        }
    }

    /// <summary>
    /// ロックオンターゲットを、現在のターゲットの左右で最も近い敵に切り替えます。
    /// </summary>
    /// <param name="switchRight">trueなら右、falseなら左のターゲットを試行。</param>
    private void SwitchTarget(bool switchRight)
    {
        Vector3 playerPos = target.position;
        Vector3 toCurrentTarget = (_lockOnTarget.position - playerPos).normalized;

        Collider[] colliders = Physics.OverlapSphere(playerPos, maxLockOnRange, enemyLayer);
        Transform nextTarget = null;
        float minAngle = float.MaxValue;

        foreach (var col in colliders)
        {
            if (col.transform == _lockOnTarget) continue; // 現在のターゲットはスキップ

            Vector3 toPotentialTarget = (col.transform.position - playerPos).normalized;
            float angle = Vector3.Angle(toCurrentTarget, toPotentialTarget);

            float signedAngle = Vector3.SignedAngle(toCurrentTarget, toPotentialTarget, Vector3.up);

            if ((switchRight && signedAngle > 0 && angle < minAngle) ||
                (!switchRight && signedAngle < 0 && angle < minAngle))
            {
                minAngle = angle;
                nextTarget = col.transform;
            }
        }

        if (nextTarget != null)
        {
            LockOnTarget = nextTarget; // Setterを通じてスムーズ切り替え処理が実行される
            Debug.Log($"ロックオン切り替え: {LockOnTarget.name}");
        }
        else
        {
            Debug.Log("切り替え可能なターゲットが見つかりませんでした。");
        }
    }

    /// <summary>
    /// 右クリック入力でロックオン/解除を処理します。
    /// 範囲内の敵を検出し、最も近い敵にロックオンします。
    /// </summary>
    private void HandleLockOnInput()
    {
        if (Input.GetMouseButton(1)) // 右クリックを押し続けている間
        {
            if (_lockOnTarget == null)
            {
                // ロックオンを試みる（範囲内の敵を検出）
                Collider[] colliders = Physics.OverlapSphere(target.position, maxLockOnRange, enemyLayer);

                if (colliders.Length > 0)
                {
                    // 1. 検出された敵の中から、角度制限内にある敵をフィルタリング
                    var validTargets = colliders
                        .Where(col =>
                        {
                            Vector3 toTarget = col.transform.position - target.position;
                            float angle = Vector3.Angle(target.forward, toTarget);
                            return angle <= lockOnAngleLimit;
                        });

                    // 2. フィルタリングされた敵の中で、最もターゲットの正面に近い敵を選択
                    Transform nearestTarget = validTargets
                        .OrderBy(col => Vector3.Angle(target.forward, col.transform.position - target.position))
                        .ThenBy(col => Vector3.Distance(target.position, col.transform.position)) // 角度が同じなら距離でソート
                        .FirstOrDefault()?.transform;

                    if (nearestTarget != null)
                    {
                        LockOnTarget = nearestTarget; // Setterを通じてスムーズ切り替え処理が実行される
                        Debug.Log($"ロックオン: {LockOnTarget.name}");
                    }
                }
            }
        }
        else // 右クリックが押されていない、または離された瞬間
        {
            // 右クリックを離したらロックオンを解除
            if (_lockOnTarget != null)
            {
                Debug.Log($"ロックオン解除: {_lockOnTarget.name}");
                LockOnTarget = null; // Setterを通じてスムーズ切り替え処理が実行される
            }
        }

        // ロックオン対象がDestroyされたり、範囲外に出た場合の自動解除
        if (_lockOnTarget != null)
        {
            if (!_lockOnTarget.gameObject.activeInHierarchy || _lockOnTarget.gameObject.GetComponent<Collider>() == null ||
              Vector3.Distance(target.position, _lockOnTarget.position) > maxLockOnKeepRange)
            {
                Debug.Log($"自動解除: {_lockOnTarget.name} (非アクティブまたは範囲外)");
                LockOnTarget = null; // Setterを通じてスムーズ切り替え処理が実行される
            }
        }
    }

    /// <summary>
    /// 固定ビューモードでのカメラ制御ロジック
    /// </summary>
    private void HandleFixedViewMode()
    {
        transform.position = Vector3.Lerp(transform.position, _fixedTargetPosition, Time.unscaledDeltaTime * _fixedViewSmoothSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, _fixedTargetRotation, Time.unscaledDeltaTime * _fixedViewSmoothSpeed);
    }

    /// <summary>
    /// 通常のTPS追従モードでのカメラ制御ロジック
    /// </summary>
    private void HandleTPSViewMode()
    {
        // 1. 目標回転の計算
        Quaternion targetRotation;

        if (_lockOnTarget != null)
        {
            // --- ロックオン中: スムーズに移行した注視点 (_lookTargetPosition) を使用して回転を計算 ---
            Vector3 camCenter = target.position + Vector3.up * height;
            // 🌟 修正: _lookTarget.position ではなく、スムーズ補間された _lookTargetPosition を使用 🌟
            Vector3 lookDirection = _lookTargetPosition - camCenter;

            // 水平方向 (Y軸) の角度を計算
            Quaternion horizontalRotation = Quaternion.LookRotation(new Vector3(lookDirection.x, 0, lookDirection.z));
            _yaw = horizontalRotation.eulerAngles.y;

            // 垂直方向 (X軸) の角度を計算
            Vector3 flatLookDirection = horizontalRotation * Vector3.forward;
            float verticalAngle = Vector3.SignedAngle(flatLookDirection, lookDirection, horizontalRotation * Vector3.right);

            // 垂直角度を制限範囲内にクランプ
            _pitch = Mathf.Clamp(verticalAngle, pitchMinMax.x, pitchMinMax.y);

            // 最終的な目標回転を再構成
            targetRotation = Quaternion.Euler(_pitch, _yaw, 0);
        }
        else
        {
            // --- 通常時: マウス入力による回転を計算 ---
            targetRotation = CalculateRotationFromInput();
        }

        // 2. 目標位置の計算
        Vector3 targetPosition = CalculateTargetPosition(targetRotation);

        // 3. 衝突判定と位置の調整
        Vector3 finalPosition = ApplyCollisionCheck(targetPosition);

        // 4. カメラの位置と回転をLerpでスムーズに補間
        float currentSmoothSpeed = _lockOnTarget != null ? lockOnRotationSpeed : smoothSpeed;
        float finalRotationSmoothSpeed = currentSmoothSpeed;

        // ロックオン時、垂直角度に応じて回転スムーズさを減衰させるロジック (既存)
        if (_lockOnTarget != null)
        {
            const float dampingStartAngle = 75.0f;
            if (_pitch > dampingStartAngle)
            {
                float rotationDampingFactor = Mathf.InverseLerp(pitchMinMax.y, dampingStartAngle, _pitch);
                rotationDampingFactor *= rotationDampingFactor;
                finalRotationSmoothSpeed = currentSmoothSpeed * rotationDampingFactor;
            }
        }

        transform.position = Vector3.Lerp(transform.position, finalPosition, Time.deltaTime * currentSmoothSpeed);

        // ロックオン中はスムーズに移行した targetRotation を使用して Slerp する
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * finalRotationSmoothSpeed);
    }

    // (中略: CalculateRotationFromInput, CalculateTargetPosition, ApplyCollisionCheck, SetFixedCameraView, ResetToTPSView, RotatePlayerToCameraDirection, GetCameraRay, GetCameraCenterPoint メソッドは元のまま)

    /// <summary>
    /// マウス入力に基づいてカメラの目標回転を計算します。
    /// </summary>
    private Quaternion CalculateRotationFromInput()
    {
        _yaw += Input.GetAxis("Mouse X") * rotationSpeed;
        _pitch -= Input.GetAxis("Mouse Y") * rotationSpeed;
        _pitch = Mathf.Clamp(_pitch, pitchMinMax.x, pitchMinMax.y); // 垂直角度を制限

        return Quaternion.Euler(_pitch, _yaw, 0);
    }

    /// <summary>
    /// 目標回転と距離に基づいて、カメラの目標ワールド位置を計算します。
    /// </summary>
    private Vector3 CalculateTargetPosition(Quaternion rotation)
    {
        Vector3 camCenter = target.position + Vector3.up * height;
        return camCenter - rotation * Vector3.forward * distance;
    }

    /// <summary>
    /// カメラ衝突判定を行い、目標位置を調整します。
    /// </summary>
    /// <param name="initialPosition">初期の目標位置。</param>
    /// <returns>衝突を考慮した最終的なカメラ位置。</returns>
    private Vector3 ApplyCollisionCheck(Vector3 initialPosition)
    {
        RaycastHit hit;
        Vector3 currentTargetPos = target.position + Vector3.up * height; // ターゲットの中心を含む位置

        if (Physics.Linecast(currentTargetPos, initialPosition, out hit, collisionLayers))
        {
            // 衝突があった場合、衝突点から少し手前にカメラを配置
            return hit.point + hit.normal * collisionOffset;
        }

        return initialPosition;
    }

    /// <summary>
    /// カメラを特定の固定位置と回転に設定し、そのビューにスムーズに移動します。
    /// Time.timeScaleが0でも動作します。
    /// </summary>
    public void SetFixedCameraView(Vector3 position, Quaternion rotation, float smoothSpeedValue)
    {
        _isFixedViewMode = true;
        _fixedTargetPosition = position;
        _fixedTargetRotation = rotation;
        _fixedViewSmoothSpeed = smoothSpeedValue; // スムーズ速度を設定

        // 固定ビューモード中はカーソルを表示し、ロックを解除
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>
    /// カメラを通常のTPS追従モードに戻します。
    /// </summary>
    public void ResetToTPSView(float smoothSpeedValue)
    {
        // TPSモードに戻る際の最初のフレームでは、スムーズに戻るために目標位置を設定
        _fixedViewSmoothSpeed = smoothSpeedValue;
        _isFixedViewMode = false; // 通常のTPSロジックがLateUpdateで再開

        // 通常のTPSモードに戻る際にカーソルを元の状態に戻す
        if (_cursorLockedInitially)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    /// <summary>
    /// プレイヤーの向きをカメラの水平方向の向きに合わせるためのメソッド
    /// </summary>
    public void RotatePlayerToCameraDirection()
    {
        if (target == null || _isFixedViewMode) return;

        Quaternion playerRotation = Quaternion.Euler(0, _yaw, 0);
        float currentRotationSpeed = _lockOnTarget != null ? lockOnRotationSpeed : smoothSpeed;

        target.rotation = Quaternion.Slerp(target.rotation, playerRotation, Time.deltaTime * currentRotationSpeed);
    }

    /// <summary>
    /// カメラからRayを取得するメソッド (カメラの中心からワールドへ)
    /// </summary>
    public Ray GetCameraRay()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            Debug.LogError("Main Camera not found! Make sure your camera is tagged 'MainCamera'.");
            return new Ray(transform.position, transform.forward);
        }
        return mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
    }

    /// <summary>
    /// カメラの中心点を取得するメソッド (GetCameraRayのRayの原点と同じ)
    /// </summary>
    public Vector3 GetCameraCenterPoint()
    {
        return GetCameraRay().origin;
    }
}