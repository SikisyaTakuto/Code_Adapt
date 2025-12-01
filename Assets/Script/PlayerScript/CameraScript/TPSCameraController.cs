using UnityEngine;
using UnityEngine.UI;
using System.Linq;

/// <summary>
/// ターゲットを追跡し、マウス入力で操作可能な三人称視点（TPS）カメラを制御します。
/// カメラ衝突とロックオン/ターゲット切り替え機能に対応しています。
/// </summary>
public class TPSCameraController : MonoBehaviour
{
    // === 設定: カメラ, 追従, 回転 ===
    [Header("1. Target and Camera Control")]
    [Tooltip("カメラが追従するターゲット（通常はプレイヤー）のTransform。")]
    public Transform target;
    [Tooltip("ターゲットの中心からカメラまでの理想的な距離。")]
    public float distance = 5.0f;
    [Tooltip("ターゲットの中心からカメラまでの相対的な高さ。")]
    public float height = 2.0f;
    [Tooltip("マウス入力によるカメラの回転速度（感度）。")]
    public float rotationSpeed = 3.0f;
    [Tooltip("ターゲット位置と回転に移動する際のスムーズさ（値が大きいほど速い）。")]
    public float smoothSpeed = 10.0f;
    [Tooltip("垂直方向（上下）のカメラの角度制限。Xが最小値（下）、Yが最大値（上）。")]
    public Vector2 pitchMinMax = new Vector2(-40, 85);

    // === 設定: ロックオン ===
    [Header("2. Lock-On Settings")]
    [Tooltip("ロックオン時のカメラの回転速度。")]
    public float lockOnRotationSpeed = 15f;
    [Tooltip("ロックオンの最大距離。プレイヤーからこの範囲内の敵を検出します。")]
    public float maxLockOnRange = 30f;
    [Tooltip("ロックオン維持のための最大距離（元の範囲より少し広めに設定）。")]
    public float maxLockOnKeepRange = 40f;
    [Tooltip("敵オブジェクトのレイヤーマスク。")]
    public LayerMask enemyLayer;
    [Tooltip("ロックオン試行時、敵がプレイヤーの前方方向から何度までの角度範囲内にいる必要があるか (片側)。")]
    public float lockOnAngleLimit = 60f;
    [Tooltip("ロックオンが自動的に解除されるまでの時間 (秒)。")]
    public float lockOnDuration = 3.0f;
    [SerializeField]
    [Tooltip("ロックオン切り替え時や解除時に、注視点をスムーズに移行させる時間。")]
    private float _changeDuration = 0.3f;

    // === 設定: 衝突 & UI ===
    [Header("3. Collision & UI")]
    [Tooltip("カメラが衝突をチェックするレイヤー。")]
    public LayerMask collisionLayers;
    [Tooltip("カメラの上昇を制限するためにチェックする天井のレイヤー。")]
    public LayerMask ceilingLayer;
    [Tooltip("衝突が発生した際、カメラを押し戻す距離のオフセット。")]
    public float collisionOffset = 0.2f;
    [Tooltip("カメラが壁をすり抜けないようにするための、カメラの仮想的な半径。")]
    public float cameraRadius = 0.3f;
    [Tooltip("天井衝突判定で、カメラの位置Y座標を天井からどれだけ下に制限するか。")]
    public float ceilingYOffset = 0.5f;
    [Tooltip("ロックオン時に画面上に表示するUI要素 (RectTransform) をアタッチ")]
    public RectTransform lockOnUIRect;
    [Tooltip("ロックオンUIの子にあるHPバーのSliderコンポーネントをアタッチ")]
    public Slider healthBarSlider;

    // === プライベート変数 ===
    private float _yaw = 0.0f;
    private float _pitch = 0.0f;
    private Vector3 _lookTargetPosition = Vector3.zero;
    private Vector3 _latestTargetPosition = Vector3.zero;
    private Transform _lockOnTarget = null;
    private float _timer = 0f;
    private float _lockOnTimer = 0f;
    private bool _isFixedViewMode = false;
    private Vector3 _fixedTargetPosition;
    private Quaternion _fixedTargetRotation;
    private float _fixedViewSmoothSpeed;
    private bool _cursorLockedInitially = false;
    private bool _isLockOnActive = false; // 外部からの参照用 (未使用だが維持)

    /// <summary>現在のロックオンターゲットを設定・取得します。設定時にスムーズな切り替えを開始します。</summary>
    public Transform LockOnTarget
    {
        get => _lockOnTarget;
        set
        {
            if (_lockOnTarget == value) return;

            // 1. 古いターゲットのUIを非表示にする
            if (_lockOnTarget != null)
            {
                // 旧ターゲットのHPバーをクリアするロジック (敵コンポーネントへの依存)
                // 📝 ここは「ScorpionEnemy」コンポーネントが必要です
                _lockOnTarget.GetComponent<ScorpionEnemy>()?.ClearHealthBar();
                lockOnUIRect?.gameObject.SetActive(false);
            }

            _latestTargetPosition = _lookTargetPosition;
            _lockOnTarget = value;
            _timer = 0f; // Lerp開始

            // 2. 新しいターゲットのUIを表示し、HPバーを設定する
            if (_lockOnTarget != null)
            {
                _lockOnTimer = lockOnDuration;

                if (lockOnUIRect != null)
                {
                    lockOnUIRect.gameObject.SetActive(true);
                    // 新ターゲットのHPバーを設定するロジック (敵コンポーネントへの依存)
                    // 📝 ここは「ScorpionEnemy」コンポーネントが必要です
                    _lockOnTarget.GetComponent<ScorpionEnemy>()?.SetHealthBar(healthBarSlider);
                }
            }
            _isLockOnActive = _lockOnTarget != null;
        }
    }

    // =======================================================
    // Unity Lifecycle
    // =======================================================

    void Start()
    {
        // Cursor.lockState/visible の初期化ロジックはコメントアウトされた状態を維持
        _cursorLockedInitially = true;

        if (target != null)
        {
            _lookTargetPosition = target.position + Vector3.up * 1.5f;
            _latestTargetPosition = _lookTargetPosition;
            _yaw = target.eulerAngles.y;
            _pitch = transform.eulerAngles.x;
            if (_pitch > 180) _pitch -= 360;
        }

        lockOnUIRect?.gameObject.SetActive(false);
    }

    void Update()
    {
        if (_isFixedViewMode || target == null || Time.timeScale <= 0) return;

        HandleLockOnTimer();
        HandleLockOnInput();
        HandleTargetSwitching();
    }

    void LateUpdate()
    {
        if (target == null) return;

        // 注視点の更新
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

        UpdateLockOnUIPosition();
    }

    // =======================================================
    // Lock-On Management
    // =======================================================

    private void HandleLockOnTimer()
    {
        if (_lockOnTarget == null) return;

        _lockOnTimer -= Time.deltaTime;
        if (_lockOnTimer <= 0) LockOnTarget = null;
    }

    private void HandleLockOnInput()
    {
        bool rightClickDown = Input.GetMouseButtonDown(1);
        bool rightClickUp = Input.GetMouseButtonUp(1);

        if (rightClickDown && _lockOnTarget == null)
        {
            // 新規ロックオンターゲットの検索
            var colliders = Physics.OverlapSphere(target.position, maxLockOnRange, enemyLayer)
                .Where(col => Vector3.Angle(target.forward, col.transform.position - target.position) <= lockOnAngleLimit);

            if (colliders.Any())
            {
                Transform nearestTarget = colliders
                    .OrderBy(col => Vector3.Angle(target.forward, col.transform.position - target.position))
                    .ThenBy(col => Vector3.Distance(target.position, col.transform.position))
                    .FirstOrDefault()?.transform;

                if (nearestTarget != null) LockOnTarget = nearestTarget;
            }
        }

        if (_lockOnTarget != null)
        {
            // ロックオン維持のためのチェック (無効化、範囲外)
            if (!_lockOnTarget.gameObject.activeInHierarchy || _lockOnTarget.gameObject.GetComponent<Collider>() == null ||
                Vector3.Distance(target.position, _lockOnTarget.position) > maxLockOnKeepRange)
            {
                LockOnTarget = null;
            }

            // 右クリックを離したら即座に解除
            if (rightClickUp) LockOnTarget = null;
        }
    }

    private void HandleTargetSwitching()
    {
        if (_lockOnTarget == null) return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll == 0) return;

        bool switchRight = scroll > 0;
        Vector3 playerPos = target.position;
        Vector3 toCurrentTarget = (_lockOnTarget.position - playerPos).normalized;

        Transform nextTarget = Physics.OverlapSphere(playerPos, maxLockOnRange, enemyLayer)
            .Where(col => col.transform != _lockOnTarget)
            .Select(col => new
            {
                Transform = col.transform,
                SignedAngle = Vector3.SignedAngle(toCurrentTarget, (col.transform.position - playerPos).normalized, Vector3.up),
                Angle = Vector3.Angle(toCurrentTarget, (col.transform.position - playerPos).normalized)
            })
            .Where(t => (switchRight && t.SignedAngle > 0) || (!switchRight && t.SignedAngle < 0))
            .OrderBy(t => t.Angle)
            .FirstOrDefault()?.Transform;

        if (nextTarget != null) LockOnTarget = nextTarget;
    }

    // =======================================================
    // Core Camera Logic
    // =======================================================

    private void HandleTPSViewMode()
    {
        Quaternion targetRotation;
        if (_lockOnTarget != null)
        {
            // ロックオン: ターゲット方向を目標回転とする
            Vector3 directionToTarget = (_lookTargetPosition - transform.position).normalized;
            targetRotation = Quaternion.LookRotation(directionToTarget);
            _yaw = targetRotation.eulerAngles.y;
            _pitch = targetRotation.eulerAngles.x;
            if (_pitch > 180) _pitch -= 360;
        }
        else
        {
            // 通常: マウス入力から回転を計算
            targetRotation = CalculateRotationFromInput();
        }

        Vector3 targetPosition = CalculateTargetPosition(targetRotation);
        targetPosition = CheckCeilingYConstraint(targetPosition);

        // 制限された位置に基づき、目標回転を再計算
        targetRotation = CalculateRotationFromPosition(targetPosition);

        // 衝突判定と位置の調整
        Vector3 finalPosition = ApplyCollisionCheck(targetPosition);

        // スムーズな補間
        float currentSmoothSpeed = _lockOnTarget != null ? lockOnRotationSpeed : smoothSpeed;
        float finalRotationSmoothSpeed = currentSmoothSpeed;

        // ロックオン中の回転減衰
        if (_lockOnTarget != null)
        {
            const float dampingStartAngle = 75.0f;
            if (_pitch > dampingStartAngle)
            {
                float rotationDampingFactor = Mathf.InverseLerp(pitchMinMax.y, dampingStartAngle, _pitch);
                finalRotationSmoothSpeed = currentSmoothSpeed * (rotationDampingFactor * rotationDampingFactor);
            }
        }

        transform.position = Vector3.Lerp(transform.position, finalPosition, Time.deltaTime * currentSmoothSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * finalRotationSmoothSpeed);
    }

    private Quaternion CalculateRotationFromInput()
    {
        _yaw += Input.GetAxis("Mouse X") * rotationSpeed;
        _pitch -= Input.GetAxis("Mouse Y") * rotationSpeed;
        _pitch = Mathf.Clamp(_pitch, pitchMinMax.x, pitchMinMax.y);
        return Quaternion.Euler(_pitch, _yaw, 0);
    }

    private Vector3 CalculateTargetPosition(Quaternion rotation)
    {
        Vector3 camCenter = target.position + Vector3.up * height;
        return camCenter - rotation * Vector3.forward * distance;
    }

    private Vector3 CheckCeilingYConstraint(Vector3 targetPosition)
    {
        if (ceilingLayer == 0) return targetPosition;

        Vector3 playerPos = target.position;
        // プレイヤーの頭上付近から真下にRayを飛ばす
        Vector3 rayStart = playerPos + Vector3.up * (height * 2f);

        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 50f, ceilingLayer))
        {
            float maxCameraY = hit.point.y - ceilingYOffset;
            targetPosition.y = Mathf.Min(targetPosition.y, maxCameraY);
        }

        return targetPosition;
    }

    private Quaternion CalculateRotationFromPosition(Vector3 targetPosition)
    {
        Vector3 camCenter = target.position + Vector3.up * height;
        Vector3 toTarget = targetPosition - camCenter;

        float horizontalDistance = new Vector3(toTarget.x, 0, toTarget.z).magnitude;
        float restrictedPitchRad = Mathf.Atan2(toTarget.y, horizontalDistance);

        _pitch = Mathf.Clamp(restrictedPitchRad * Mathf.Rad2Deg, pitchMinMax.x, pitchMinMax.y);

        return Quaternion.Euler(_pitch, _yaw, 0);
    }

    private Vector3 ApplyCollisionCheck(Vector3 initialPosition)
    {
        Vector3 currentTargetPos = target.position + Vector3.up * height;
        Vector3 direction = (initialPosition - currentTargetPos).normalized;
        float travelDistance = Vector3.Distance(currentTargetPos, initialPosition);

        if (Physics.SphereCast(currentTargetPos, cameraRadius, direction, out RaycastHit hit, travelDistance, collisionLayers))
        {
            // 衝突点から cameraRadius + collisionOffset 分だけ手前にカメラを配置
            float desiredDistance = Mathf.Max(hit.distance - collisionOffset, 0.1f);
            return currentTargetPos + direction * desiredDistance;
        }

        return initialPosition;
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

    // =======================================================
    // Fixed View & Utilities
    // =======================================================

    private void HandleFixedViewMode()
    {
        transform.position = Vector3.Lerp(transform.position, _fixedTargetPosition, Time.unscaledDeltaTime * _fixedViewSmoothSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, _fixedTargetRotation, Time.unscaledDeltaTime * _fixedViewSmoothSpeed);
    }

    public void SetFixedCameraView(Vector3 position, Quaternion rotation, float smoothSpeedValue)
    {
        _isFixedViewMode = true;
        _fixedTargetPosition = position;
        _fixedTargetRotation = rotation;
        _fixedViewSmoothSpeed = smoothSpeedValue;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResetToTPSView(float smoothSpeedValue)
    {
        _fixedViewSmoothSpeed = smoothSpeedValue;
        _isFixedViewMode = false;

        if (_cursorLockedInitially)
        {
            // Cursor.lockState/visible の初期化ロジックはコメントアウトされた状態を維持
        }
    }

    /// <summary>
    /// プレイヤーの向きを、WASD入力方向（カメラを基準とする）に合わせるためのメソッド
    /// </summary>
    public void RotatePlayerToCameraDirection()
    {
        // ロックオン中や固定ビュー中は回転を無効化
        if (target == null || _isFixedViewMode || _lockOnTarget != null) return;

        // 水平方向の入力を取得
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");

        // 入力がなければ回転させない
        if (horizontalInput == 0 && verticalInput == 0) return;

        // カメラの水平方向の回転 (Y軸) を取得
        Quaternion cameraRotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);

        // 入力方向をワールド空間のベクトルに変換（カメラの向きが基準）
        Vector3 inputDirection = cameraRotation * new Vector3(horizontalInput, 0f, verticalInput).normalized;

        // 目標の回転を計算
        // inputDirectionがゼロベクトルでないことを確認してからLookRotationを使用 (ここでは既にチェック済みだが安全策として)
        Quaternion targetRotation = Quaternion.LookRotation(inputDirection);

        // スムーズな回転
        float currentRotationSpeed = smoothSpeed;

        target.rotation = Quaternion.Slerp(target.rotation, targetRotation, Time.deltaTime * currentRotationSpeed);
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

    private void UpdateLockOnUIPosition()
    {
        if (_lockOnTarget == null || lockOnUIRect == null)
        {
            if (lockOnUIRect != null && lockOnUIRect.gameObject.activeSelf) lockOnUIRect.gameObject.SetActive(false);
            return;
        }

        Camera mainCam = Camera.main;
        if (mainCam == null) return;

        Vector3 targetWorldPosition = _lockOnTarget.position + Vector3.up * 1.5f;
        Vector3 screenPos = mainCam.WorldToScreenPoint(targetWorldPosition);

        // 画面の後ろにいる場合はUIを非表示にする
        if (screenPos.z < 0)
        {
            if (lockOnUIRect.gameObject.activeSelf) lockOnUIRect.gameObject.SetActive(false);
            return;
        }

        // UIを表示
        if (!lockOnUIRect.gameObject.activeSelf) lockOnUIRect.gameObject.SetActive(true);

        // RectTransformの位置をスクリーン座標に設定
        lockOnUIRect.position = screenPos;
    }
}