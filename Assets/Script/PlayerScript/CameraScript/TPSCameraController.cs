using UnityEngine;
using System.Linq;
using UnityEngine.UI; // RectTransformを使用するため追加

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
    public float height = 2.0f;     // ターゲットからの高さ

    [Header("Camera Control")]
    [Tooltip("マウス入力によるカメラの回転速度（感度）。")]
    public float rotationSpeed = 3.0f; // カメラの回転速度（マウス速度）

    [Tooltip("ターゲット位置と回転に移動する際のスムーズさ（値が大きいほど速い）。")]
    public float smoothSpeed = 10.0f; // カメラの移動と回転のスムーズさ

    [Tooltip("垂直方向（上下）のカメラの角度制限。Xが最小値（下）、Yが最大値（上）。")]
    public Vector2 pitchMinMax = new Vector2(-40, 85); // 垂直方向のカメラ角度制限

    // === ロックオン機能 ===
    [Header("Lock-On Settings (Camera)")]
    [Tooltip("ロックオン時のカメラの回転速度。")]
    public float lockOnRotationSpeed = 15f;

    [Tooltip("ロックオンの最大距離。プレイヤーからこの範囲内の敵を検出します。")]
    public float maxLockOnRange = 30f; // プレイヤーの周りの検出範囲

    [Tooltip("ロックオン維持のための最大距離（元の範囲より少し広めに設定）。")]
    public float maxLockOnKeepRange = 40f;

    [Tooltip("敵オブジェクトのレイヤーマスク。")]
    public LayerMask enemyLayer;

    [Tooltip("ロックオン試行時、敵がプレイヤーの前方方向から何度までの角度範囲内にいる必要があるか (片側)。例: 60で合計120度。")]
    public float lockOnAngleLimit = 60f; // ロックオン可能なプレイヤーの前方角度制限

    [Tooltip("ロックオン切り替え時や解除時に、注視点をスムーズに移行させる時間。")]
    [SerializeField]
    private float _changeDuration = 0.3f; // ロック切り替え時間

    // === ロックオン機能 (時間制限) ===
    [Header("Lock-On Settings (Timer)")]
    [Tooltip("ロックオンが自動的に解除されるまでの時間 (秒)。")]
    public float lockOnDuration = 3.0f; // ロックオン持続時間（3秒）

    // 💡 UPDATE: UI設定 - RectTransformに変更
    [Header("Lock-On UI Settings (Screen UI)")]
    [Tooltip("ロックオン時に画面上に表示するUI要素 (RectTransform) をアタッチ")]
    public RectTransform lockOnUIRect;

    // === ロックオンの切り替えスムーズ化のための内部変数 ===
    private float _timer = 0f;
    private Vector3 _lookTargetPosition = Vector3.zero;
    private Vector3 _latestTargetPosition = Vector3.zero;
    private Transform _lockOnTarget = null;

    // 💡 NEW: ロックオンの残り時間
    private float _lockOnTimer = 0f;

    // 💡 UPDATE: ロックオン中フラグ
    private bool _isLockOnActive = false;

    /// <summary>現在のロックオンターゲットを設定・取得します。設定時にスムーズな切り替えを開始します。</summary>
    public Transform LockOnTarget
    {
        get { return _lockOnTarget; }
        set
        {
            if (_lockOnTarget != value)
            {
                // 1. 古いUIの非表示
                if (_lockOnTarget != null)
                {
                    // 💡 UPDATE: UIを非表示にする
                    if (lockOnUIRect != null)
                    {
                        lockOnUIRect.gameObject.SetActive(false);
                    }
                }

                _latestTargetPosition = _lookTargetPosition;
                _lockOnTarget = value;
                _timer = 0f; // タイマーをリセットしてLerpを開始

                // 2. 新しいUIの表示と設定
                if (_lockOnTarget != null)
                {
                    _lockOnTimer = lockOnDuration;

                    // 💡 UPDATE: UIを表示する
                    if (lockOnUIRect != null)
                    {
                        // UIを初期位置に配置してからアクティブにする
                        if (!lockOnUIRect.gameObject.activeSelf)
                        {
                            lockOnUIRect.gameObject.SetActive(true);
                        }
                        // UIの位置更新はLateUpdateで行います
                    }
                }
            }
        }
    }
    // ===================================

    // === 衝突設定 ===
    [Header("Collision Settings")]
    [Tooltip("カメラが衝突をチェックするレイヤー。壁や地面などを設定します。")]
    public LayerMask collisionLayers; // カメラが衝突をチェックするレイヤー（壁や地面など）

    // 💡 天井レイヤー
    [Tooltip("カメラの上昇を制限するためにチェックする天井のレイヤー。")]
    public LayerMask ceilingLayer;

    [Tooltip("衝突が発生した際、カメラを押し戻す距離のオフセット。")]
    public float collisionOffset = 0.2f; // 衝突時にカメラが押し戻されるオフセット

    [Tooltip("カメラが壁をすり抜けないようにするための、カメラの仮想的な半径。")]
    public float cameraRadius = 0.3f; // カメラの仮想的な半径

    // 💡 (追加) Y軸制限のオフセット
    [Tooltip("天井衝突判定で、カメラの位置Y座標を天井からどれだけ下に制限するか。")]
    public float ceilingYOffset = 0.5f;

    // カメラの現在の角度
    private float _yaw = 0.0f;     // 左右の回転角度 (Y軸)
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
        if (target != null)
        {
            // 注視点の初期化 (ターゲットの少し上)
            _lookTargetPosition = target.position + Vector3.up * 1.5f;
            _latestTargetPosition = _lookTargetPosition;
        }

        // 💡 NEW: 初期化時にUIを非表示
        if (lockOnUIRect != null)
        {
            lockOnUIRect.gameObject.SetActive(false);
        }
    }

    private void InitializeCursor()
    {
        //Cursor.lockState = CursorLockMode.Locked;
        //Cursor.visible = false;
        _cursorLockedInitially = true;
    }

    private void InitializeCameraAngles()
    {
        if (target != null)
        {
            _yaw = target.eulerAngles.y;
            _pitch = transform.eulerAngles.x;
            if (_pitch > 180) _pitch -= 360;
        }
    }

    void Update()
    {
        if (!_isFixedViewMode && target != null && Time.timeScale > 0)
        {
            HandleLockOnTimer(); // 💡 NEW: ロックオンタイマーの管理
            HandleLockOnInput(); // LockOnTargetの選択と_isLockOnActiveの更新
            HandleTargetSwitching(); // ロックオン中のターゲット切り替え
        }
    }

    // 💡 NEW: ロックオンの持続時間を管理するメソッド
    private void HandleLockOnTimer()
    {
        if (_lockOnTarget != null)
        {
            _lockOnTimer -= Time.deltaTime;
            if (_lockOnTimer <= 0)
            {
                LockOnTarget = null; // 時間切れでロックオン解除
            }
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        // 注視点の更新 (ターゲットの頭付近を注視)
        Vector3 defaultLookPosition = target.position + Vector3.up * height;
        Vector3 targetLookPosition = _lockOnTarget != null ? _lockOnTarget.position + Vector3.up * 1.5f : defaultLookPosition;
        UpdateLookTargetPosition(targetLookPosition);

        if (_isFixedViewMode)
        {
            HandleFixedViewMode();
        }
        else
        {
            HandleTPSViewMode();
        }

        // 💡 NEW: ロックオンUIの位置を更新
        UpdateLockOnUIPosition();
    }

    private void UpdateLookTargetPosition(Vector3 targetPosition)
    {
        if (_timer < _changeDuration)
        {
            _timer += Time.deltaTime;
            _lookTargetPosition = Vector3.Lerp(_latestTargetPosition, targetPosition, _timer / _changeDuration);
        }
        else
        {
            _lookTargetPosition = targetPosition;
        }
    }

    private void HandleTargetSwitching()
    {
        // ロックオン中（_lockOnTargetがnullでない）かつマウスホイール入力がある場合のみターゲット切り替えを許可
        if (_lockOnTarget == null) return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0) SwitchTarget(scroll > 0);
    }

    private void SwitchTarget(bool switchRight)
    {
        Vector3 playerPos = target.position;
        Vector3 toCurrentTarget = (_lockOnTarget.position - playerPos).normalized;

        Collider[] colliders = Physics.OverlapSphere(playerPos, maxLockOnRange, enemyLayer);
        Transform nextTarget = null;
        float minAngle = float.MaxValue;

        foreach (var col in colliders)
        {
            if (col.transform == _lockOnTarget) continue;

            Vector3 toPotentialTarget = (col.transform.position - playerPos).normalized;
            float angle = Vector3.Angle(toCurrentTarget, toPotentialTarget);
            float signedAngle = Vector3.SignedAngle(toCurrentTarget, toPotentialTarget, Vector3.up);

            // ロックオンターゲット切り替えロジック
            if ((switchRight && signedAngle > 0 && angle < minAngle) ||
                (!switchRight && signedAngle < 0 && angle < minAngle))
            {
                minAngle = angle;
                nextTarget = col.transform;
            }
        }

        if (nextTarget != null) LockOnTarget = nextTarget;
    }

    private void HandleLockOnInput()
    {
        bool rightClickDown = Input.GetMouseButtonDown(1);
        bool rightClickUp = Input.GetMouseButtonUp(1);

        // 💡 UPDATE: 右クリックを押した瞬間
        if (rightClickDown)
        {
            // ターゲットが設定されていない場合のみ、新規にロックオンターゲットを検索
            if (_lockOnTarget == null)
            {
                Collider[] colliders = Physics.OverlapSphere(target.position, maxLockOnRange, enemyLayer);

                if (colliders.Length > 0)
                {
                    // プレイヤーの前方、かつ角度制限内の有効なターゲットを抽出
                    var validTargets = colliders
                        .Where(col =>
                        {
                            Vector3 toTarget = col.transform.position - target.position;
                            float angle = Vector3.Angle(target.forward, toTarget);
                            return angle <= lockOnAngleLimit;
                        });

                    // 最も近いターゲットを選択 (角度の近さを優先)
                    Transform nearestTarget = validTargets
                        .OrderBy(col => Vector3.Angle(target.forward, col.transform.position - target.position))
                        .ThenBy(col => Vector3.Distance(target.position, col.transform.position))
                        .FirstOrDefault()?.transform;

                    if (nearestTarget != null)
                    {
                        LockOnTarget = nearestTarget;
                        // _lockOnTimer は LockOnTarget プロパティ内でリセットされる
                    }
                }
            }
        }

        // 💡 UPDATE: ロックオン維持のためのチェックと解除
        if (_lockOnTarget != null)
        {
            // ロックオン維持のためのチェック（ターゲットの有効性、距離）
            if (!_lockOnTarget.gameObject.activeInHierarchy || _lockOnTarget.gameObject.GetComponent<Collider>() == null ||
                Vector3.Distance(target.position, _lockOnTarget.position) > maxLockOnKeepRange)
            {
                LockOnTarget = null; // ターゲットが範囲外/無効なら解除
            }

            // 右クリックを離したら、ロックオンを即座に解除
            if (rightClickUp)
            {
                LockOnTarget = null;
            }
        }

        // ロックオンが有効な状態を反映
        _isLockOnActive = _lockOnTarget != null;
    }

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
        // 1. ロックオン中か否かによって、目標回転を計算
        Quaternion targetRotation;

        // 💡 UPDATE: _lockOnTargetが存在する場合にターゲットを追従
        if (_lockOnTarget != null)
        {
            // ロックオン中はターゲットの方向を目標回転とする
            Vector3 directionToTarget = (_lookTargetPosition - transform.position).normalized;
            targetRotation = Quaternion.LookRotation(directionToTarget);

            // ヨーとピッチを、目標回転の角度に合わせる
            _yaw = targetRotation.eulerAngles.y;
            _pitch = targetRotation.eulerAngles.x;
            if (_pitch > 180) _pitch -= 360; // 0-360 から -180-180 への変換
        }
        else
        {
            // 通常時: マウス入力から回転を計算
            targetRotation = CalculateRotationFromInput();
        }

        // 2. 目標位置の計算
        Vector3 targetPosition = CalculateTargetPosition(targetRotation);

        // 💡 Y座標制限チェックを行い、目標位置と回転を修正
        targetPosition = CheckCeilingYConstraint(targetPosition);

        // 制限された位置に基づき、目標回転を再計算
        targetRotation = CalculateRotationFromPosition(targetPosition);

        // 3. 衝突判定と位置の調整
        Vector3 finalPosition = ApplyCollisionCheck(targetPosition);

        // 4. カメラの位置と回転をLerpでスムーズに補間
        // ロックオン中の場合は lockOnRotationSpeed を使用
        float currentSmoothSpeed = _lockOnTarget != null ? lockOnRotationSpeed : smoothSpeed;
        float finalRotationSmoothSpeed = currentSmoothSpeed;

        // ロックオン中の回転減衰ロジック
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
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * finalRotationSmoothSpeed);
    }

    private Quaternion CalculateRotationFromInput()
    {
        // ロックオン中はマウス入力を無視
        if (_lockOnTarget == null)
        {
            _yaw += Input.GetAxis("Mouse X") * rotationSpeed;
            _pitch -= Input.GetAxis("Mouse Y") * rotationSpeed;
            _pitch = Mathf.Clamp(_pitch, pitchMinMax.x, pitchMinMax.y);
        }
        return Quaternion.Euler(_pitch, _yaw, 0);
    }

    private Vector3 CalculateTargetPosition(Quaternion rotation)
    {
        Vector3 camCenter = target.position + Vector3.up * height;
        return camCenter - rotation * Vector3.forward * distance;
    }

    private Quaternion CalculateRotationFromPosition(Vector3 targetPosition)
    {
        Vector3 camCenter = target.position + Vector3.up * height;
        Vector3 toTarget = targetPosition - camCenter;

        Vector3 flatVector = new Vector3(toTarget.x, 0, toTarget.z);
        float horizontalDistance = flatVector.magnitude;
        float verticalDistance = toTarget.y;

        // atan2を使って、この位置に到達するためのピッチ角度を求める
        float restrictedPitchRad = Mathf.Atan2(verticalDistance, horizontalDistance);
        _pitch = restrictedPitchRad * Mathf.Rad2Deg;

        // 角度制限を再適用
        _pitch = Mathf.Clamp(_pitch, pitchMinMax.x, pitchMinMax.y);

        return Quaternion.Euler(_pitch, _yaw, 0);
    }


    /// <summary>
    /// カメラの目標位置のY座標を、天井のY座標に基づいて制限します。
    /// </summary>
    private Vector3 CheckCeilingYConstraint(Vector3 targetPosition)
    {
        if (ceilingLayer == 0) return targetPosition; // ceilingLayerが設定されていない場合はスキップ

        Vector3 playerPos = target.position;

        RaycastHit hit;
        // プレイヤーの頭上付近から真下にRayを飛ばし、天井を探す
        Vector3 rayStart = playerPos + Vector3.up * (height * 2f);
        float rayLength = 50f;

        // 真下にRaycast
        if (Physics.Raycast(rayStart, Vector3.down, out hit, rayLength, ceilingLayer))
        {
            float ceilingY = hit.point.y;

            // カメラが超えてはならないY座標 = 天井のY座標 - オフセット
            float maxCameraY = ceilingY - ceilingYOffset;

            // 目標カメラ位置のY座標が、制限を超えているかチェック
            if (targetPosition.y > maxCameraY)
            {
                // 制限を超えている場合、Y座標を制限値にクランプする
                targetPosition.y = maxCameraY;
            }
        }

        return targetPosition;
    }

    /// <summary>
    /// カメラ衝突判定を SphereCast で行い、壁抜けを修正します。
    /// </summary>
    private Vector3 ApplyCollisionCheck(Vector3 initialPosition)
    {
        Vector3 currentTargetPos = target.position + Vector3.up * height;
        RaycastHit hit;

        float travelDistance = Vector3.Distance(currentTargetPos, initialPosition);
        Vector3 direction = (initialPosition - currentTargetPos).normalized;

        if (Physics.SphereCast(currentTargetPos, cameraRadius, direction, out hit, travelDistance, collisionLayers))
        {
            float newDistance = hit.distance;

            // 衝突点から cameraRadius + collisionOffset 分だけ手前にカメラを配置する
            float desiredDistance = newDistance - collisionOffset;

            // カメラがターゲットの中心を通り過ぎないように最小距離を確保
            desiredDistance = Mathf.Max(desiredDistance, 0.1f);

            Vector3 finalPosition = currentTargetPos + direction * desiredDistance;

            return finalPosition;
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
        _fixedViewSmoothSpeed = smoothSpeedValue;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>
    /// カメラを通常のTPS追従モードに戻します。
    /// </summary>
    public void ResetToTPSView(float smoothSpeedValue)
    {
        _fixedViewSmoothSpeed = smoothSpeedValue;
        _isFixedViewMode = false;

        if (_cursorLockedInitially)
        {
            //Cursor.lockState = CursorLockMode.Locked;
            //Cursor.visible = false;
        }
    }

    /// <summary>
    /// プレイヤーの向きをカメラの水平方向の向きに合わせるためのメソッド
    /// </summary>
    public void RotatePlayerToCameraDirection()
    {
        // 💡 UPDATE: ロックオン中（_lockOnTarget != null）はプレイヤー回転を無効化
        if (target == null || _isFixedViewMode || _lockOnTarget != null) return;

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

    // 💡 NEW: ロックオンUIの位置を更新するメソッド
    private void UpdateLockOnUIPosition()
    {
        // ロックオン中で、かつUI参照が設定されている場合のみ実行
        if (_lockOnTarget != null && lockOnUIRect != null)
        {
            Camera mainCam = Camera.main;
            if (mainCam == null) return;

            // 敵のワールド座標をスクリーン座標に変換
            // 敵の注視点 (敵の頭付近を想定) を使用
            Vector3 targetWorldPosition = _lockOnTarget.position + Vector3.up * 1.5f;

            // ワールド座標をスクリーン座標に変換
            Vector3 screenPos = mainCam.WorldToScreenPoint(targetWorldPosition);

            // 画面の後ろにいる場合はUIを非表示にする
            if (screenPos.z < 0)
            {
                lockOnUIRect.gameObject.SetActive(false);
                return;
            }

            // UIを有効にする (LockOnTargetのsetterで既に処理されているが、再確認)
            if (!lockOnUIRect.gameObject.activeSelf)
            {
                lockOnUIRect.gameObject.SetActive(true);
            }

            // RectTransformの位置をスクリーン座標に設定
            lockOnUIRect.position = screenPos;
        }
        else if (lockOnUIRect != null && lockOnUIRect.gameObject.activeSelf)
        {
            // ロックオンが解除された場合はUIを非表示にする
            lockOnUIRect.gameObject.SetActive(false);
        }
    }
}