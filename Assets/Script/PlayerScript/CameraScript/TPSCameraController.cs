using UnityEngine;
using UnityEngine.InputSystem; // ★ Input Systemを追加
using UnityEngine.UI;
using System.Linq;

/// <summary>
/// ターゲットを追跡し、Input Systemのアクションで操作可能な三人称視点（TPS）カメラを制御します。（Input System 使用）
/// </summary>
public class TPSCameraController : MonoBehaviour
{
    // === 1. 設定: カメラ, 追従, 回転 ===
    [Header("1. Target and Camera Control")]
    [Tooltip("カメラが追従するターゲット（通常はプレイヤー）のTransform。")]
    public Transform target;
    [Tooltip("ターゲットの中心からカメラまでの理想的な距離。")]
    public float distance = 5.0f;
    [Tooltip("ターゲットの中心からカメラまでの相対的な高さ。")]
    public float height = 2.0f;

    [Tooltip("マウス入力によるカメラの回転速度（感度）。")]
    public float mouseRotationSpeed = 3.0f;
    [Tooltip("コントローラー入力によるカメラの回転速度（感度）。")]
    public float controllerRotationSpeed = 500.0f;

    [Tooltip("ターゲット位置と回転に移動する際のスムーズさ（値が大きいほど速い）。")]
    public float smoothSpeed = 10.0f;
    [Tooltip("垂直方向（上下）のカメラの角度制限。Xが最小値（下）、Yが最大値（上）。")]
    public Vector2 pitchMinMax = new Vector2(-40, 85);

    // === 2. 設定: ロックオン ===
    [Header("2. Lock-On Settings")]
    [Tooltip("ロックオン時のカメラの回転速度。値を増やすと追従が強くなる。")]
    // ★ 修正点: 初期値を強化
    public float lockOnRotationSpeed = 50f;
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

    // === 3. 設定: 衝突 & UI ===
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

    // public Slider healthBarSlider; // コメントアウト状態を維持

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
    private bool _isLockOnActive = false;

    // --- Input System 用の変数 ---
    private PlayerInput _playerInput;
    private Vector2 _lookInput = Vector2.zero;

    private float _lockOnTriggerValue = 0f;      // ★ ロックオンボタン/トリガーの値
    private Vector2 _targetSwitchInput = Vector2.zero; // ★ ターゲット切り替え用 (右スティックのX軸など)

    // ロックオンボタンの状態を保持するためのプライベート変数
    private bool wasControllerLockOnHoldThisFrame = false;


    // プレイヤーインプットからのメッセージレシーバー (Action Name: Look)
    public void OnLook(InputValue value)
    {
        _lookInput = value.Get<Vector2>();
    }

    // ★ プレイヤーインプットからのメッセージレシーバー (Action Name: LeftTrigger)
    public void OnLeftTrigger(InputValue value)
    {
        // 押されている/トリガーの値 (0.0f から 1.0f) を取得
        _lockOnTriggerValue = value.Get<float>();
    }

    // ★ プレイヤーインプットからのメッセージレシーバー (Action Name: TargetSwitch/RightStickClickなど)
    public void OnTargetSwitch(InputValue value)
    {
        // Vector2 (例: 十字キー, 右スティックの押し込み方向など) 
        _targetSwitchInput = value.Get<Vector2>();
    }


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
                // 📝 ここは「ScorpionEnemy」コンポーネントが必要です
                // _lockOnTarget.GetComponent<ScorpionEnemy>()?.ClearHealthBar(); 
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
                    // 📝 ここは「ScorpionEnemy」コンポーネントが必要です
                    // _lockOnTarget.GetComponent<ScorpionEnemy>()?.SetHealthBar(healthBarSlider);
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
        // PlayerInputコンポーネントを取得 (ターゲットまたはその親)
        _playerInput = target?.GetComponentInParent<PlayerInput>();
        if (_playerInput == null)
        {
            Debug.LogError("PlayerInput component not found on the target or its parent. Controller rotation and lock-on will not work.");
        }

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
    // Lock-On Management (LT押し続けでロックオン維持)
    // =======================================================

    private void HandleLockOnTimer()
    {
        if (_lockOnTarget == null) return;

        // LTが押されている場合は、自動解除タイマーを無視する
        const float triggerThreshold = 0.5f;
        if (_lockOnTriggerValue > triggerThreshold)
        {
            _lockOnTimer = lockOnDuration; // タイマーをリセットしてロックオンを維持
            return;
        }

        // LTが押されていない、またはマウス右クリックが離された場合にタイマーチェックを行う
        _lockOnTimer -= Time.deltaTime;
        if (_lockOnTimer <= 0) LockOnTarget = null;
    }

    private void HandleLockOnInput()
    {
        // 既存のキーボード/マウス入力（変更なし）
        bool rightClickDown = Input.GetMouseButtonDown(1);
        bool rightClickUp = Input.GetMouseButtonUp(1);

        // ★ コントローラー入力の判定
        const float triggerThreshold = 0.5f;
        // コントローラーのロックオンボタン/トリガーが押されているか
        bool isControllerLockOnHold = _lockOnTriggerValue > triggerThreshold;

        // コントローラーのロックオンボタン/トリガーが今押された瞬間を検出 (このフレームで閾値を超えた)
        bool isControllerLockOnDown = isControllerLockOnHold && !wasControllerLockOnHoldThisFrame;
        // コントローラーのロックオンボタン/トリガーが今離された瞬間を検出
        bool isControllerLockOnUp = !isControllerLockOnHold && wasControllerLockOnHoldThisFrame;

        // 次のフレームのために現在の状態を保持
        wasControllerLockOnHoldThisFrame = isControllerLockOnHold;


        // ロックオン開始の条件: (マウス右クリックダウン) または (コントローラーボタン/トリガーダウン)
        if ((rightClickDown || isControllerLockOnDown) && _lockOnTarget == null)
        {
            // 新規ロックオンターゲットの検索 (ロジックは変更なし)
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

            // ロックオン解除の条件: (マウス右クリックアップ) または (コントローラーボタン/トリガーアップ)
            if (rightClickUp || isControllerLockOnUp)
            {
                // マウス右クリックを離す、またはLTを離すと即座に解除
                LockOnTarget = null;
            }
        }
    }


    private void HandleTargetSwitching()
    {
        if (_lockOnTarget == null) return;

        // 既存のキーボード/マウス入力（変更なし）
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        bool isMouseScroll = scroll != 0;

        // ★ コントローラー入力の判定
        // Vector2の入力があったか (例: DPad/右スティックのX軸)
        bool isControllerSwitch = Mathf.Abs(_targetSwitchInput.x) > 0.5f;

        // マウスホイールまたはコントローラー入力がない場合はスキップ
        if (!isMouseScroll && !isControllerSwitch) return;

        // 切り替え方向を決定
        bool switchRight = false;
        if (isMouseScroll)
        {
            switchRight = scroll > 0;
        }
        else if (isControllerSwitch)
        {
            // コントローラーのX軸入力に基づいて方向を決定
            switchRight = _targetSwitchInput.x > 0;
            // 入力を消費 (連続切り替えを防ぐため)
            _targetSwitchInput = Vector2.zero;
        }

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
            // 切り替え方向にいるターゲットに絞る
            .Where(t => (switchRight && t.SignedAngle > 0) || (!switchRight && t.SignedAngle < 0))
            .OrderBy(t => t.Angle) // 最も角度が近い敵を選択
            .FirstOrDefault()?.Transform;

        if (nextTarget != null) LockOnTarget = nextTarget;
    }

    // =======================================================
    // Core Camera Logic
    // =======================================================

    private void HandleTPSViewMode()
    {
        Quaternion targetRotation;
        float currentRotationSmoothSpeed;

        if (_lockOnTarget != null)
        {
            // ロックオン: ターゲット方向を目標回転とする
            Vector3 directionToTarget = (_lookTargetPosition - transform.position).normalized;
            targetRotation = Quaternion.LookRotation(directionToTarget);

            // ロックオン中は、_yaw/_pitchを直接更新してプレイヤーの回転に反映させる必要がある
            _yaw = targetRotation.eulerAngles.y;
            _pitch = targetRotation.eulerAngles.x;
            if (_pitch > 180) _pitch -= 360;

            // ロックオン時のスムーズ速度
            currentRotationSmoothSpeed = lockOnRotationSpeed;
        }
        else
        {
            // 通常: マウス/コントローラー入力から回転を計算
            targetRotation = CalculateRotationFromInput();
            // 通常時のスムーズ速度
            currentRotationSmoothSpeed = smoothSpeed;
        }

        // (中略 - 衝突判定とスムーズな補間ロジックは変更なし)

        Vector3 targetPosition = CalculateTargetPosition(targetRotation);
        targetPosition = CheckCeilingYConstraint(targetPosition);

        // 制限された位置に基づき、目標回転を再計算
        targetRotation = CalculateRotationFromPosition(targetPosition);

        // 衝突判定と位置の調整
        Vector3 finalPosition = ApplyCollisionCheck(targetPosition);

        // スムーズな補間
        float finalRotationSmoothSpeed = currentRotationSmoothSpeed;

        // ロックオン中の回転減衰 (既存ロジックを維持)
        if (_lockOnTarget != null)
        {
            const float dampingStartAngle = 75.0f;
            if (_pitch > dampingStartAngle)
            {
                float rotationDampingFactor = Mathf.InverseLerp(pitchMinMax.y, dampingStartAngle, _pitch);
                finalRotationSmoothSpeed = currentRotationSmoothSpeed * (rotationDampingFactor * rotationDampingFactor);
            }
        }

        // カメラ位置と回転をスムーズに更新
        transform.position = Vector3.Lerp(transform.position, finalPosition, Time.deltaTime * currentRotationSmoothSpeed);
        // ★ 修正点: ロックオン解除時の回転のスムーズさを考慮し、スムーズ速度を統一
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * finalRotationSmoothSpeed);
    }

    /// <summary>
    /// マウスとコントローラーの右スティックの入力を分離して処理し、カメラの回転を計算します。
    /// </summary>
    private Quaternion CalculateRotationFromInput()
    {
        float deltaYaw = 0;
        float deltaPitch = 0;

        // 1. 従来のInput Manager (マウス) からの値を取得
        float mouseX = Input.GetAxis("Mouse X") * mouseRotationSpeed;
        float mouseY = Input.GetAxis("Mouse Y") * mouseRotationSpeed;

        // 2. Input Systemからの値を取得
        float inputSystemX = _lookInput.x;
        float inputSystemY = _lookInput.y;

        // デバイス判定: PlayerInputコンポーネントの currentControlScheme を利用する
        bool isGamepad = _playerInput != null &&
                             _playerInput.currentControlScheme != null &&
                             _playerInput.currentControlScheme.Contains("Controller");

        if (isGamepad)
        {
            // コントローラー (Gamepad) からの入力: Input Systemの値を採用
            deltaYaw += inputSystemX * controllerRotationSpeed * Time.deltaTime;
            deltaPitch += inputSystemY * controllerRotationSpeed * Time.deltaTime;
        }
        else
        {
            // キーボードまたはマウスからの入力 (Mouse/Keyboard Scheme)

            // a) Input SystemでマウスがLookアクションにバインドされている場合: 
            if (Mathf.Abs(inputSystemX) > 0.001f || Mathf.Abs(inputSystemY) > 0.001f)
            {
                deltaYaw += inputSystemX * mouseRotationSpeed;
                deltaPitch += inputSystemY * mouseRotationSpeed;
            }
            // b) 従来のInput Managerでのみマウスが使用されている場合 (既存コード維持のため):
            else
            {
                deltaYaw += mouseX;
                deltaPitch += mouseY;
            }
        }

        // Pitchの調整 (上下の回転)
        _yaw += deltaYaw;
        _pitch -= deltaPitch; // カメラの上下視点は、通常入力Yと逆になるためマイナス

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
    // Fixed View & Utilities (変更なし)
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