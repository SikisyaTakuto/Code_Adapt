using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;

public class TPSCameraController : MonoBehaviour
{
    #region 1. 構造・設定データ (Inspector)

    [Header("--- 基本追従設定 ---")]
    [SerializeField] private Transform target;
    [SerializeField] private float distance = 5.0f;
    [SerializeField] private float height = 2.0f;
    [SerializeField] private float smoothSpeed = 10.0f;
    [SerializeField] private Vector2 pitchMinMax = new Vector2(-40, 85);

    [Header("--- 感度設定 (初期値) ---")]
    [SerializeField] private float initialMouseSpeed = 5.0f;
    [SerializeField] private float initialControllerSpeed = 10.0f;

    [Header("--- ロックオン設定 ---")]
    [SerializeField] private float lockOnRotationSpeed = 50f;
    [SerializeField] private float maxLockOnRange = 30f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float lockOnAngleLimit = 60f;
    [SerializeField] private float lockOnDuration = 3.0f;
    [SerializeField] private float changeDuration = 0.3f;
    [SerializeField] private RectTransform lockOnUIRect;

    [Header("--- 衝突判定・地形保護 ---")]
    [SerializeField] private LayerMask collisionLayers;
    [SerializeField] private LayerMask ceilingLayer;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float cameraRadius = 0.3f;
    [SerializeField] private float collisionOffset = 0.2f;
    [SerializeField] private float groundYOffset = 0.5f;
    [SerializeField] private float ceilingYOffset = 0.5f;

    #endregion

    #region 2. 内部変数 (Private)

    private float _yaw;
    private float _pitch;
    private Vector3 _lookTargetPosition;
    private Vector3 _latestTargetPosition;
    private Transform _lockOnTarget;

    private float _timer;
    private float _lockOnTimer;
    private bool _isFixedViewMode;

    private Vector3 _fixedTargetPosition;
    private Quaternion _fixedTargetRotation;
    private float _fixedViewSmoothSpeed;

    private PlayerInput _playerInput;
    private Vector2 _lookInput;
    private float _lockOnTriggerValue;
    private Vector2 _targetSwitchInput;
    private bool _wasControllerLockOnHold;

    // 感度管理（staticを維持しつつ初期化を確実にする）
    private static float _mouseRotationSpeed = -1f;
    private static float _controllerRotationSpeed = -1f;

    public static float MouseRotationSpeed
    {
        get => _mouseRotationSpeed;
        set => _mouseRotationSpeed = value;
    }
    public static float ControllerRotationSpeed
    {
        get => _controllerRotationSpeed;
        set => _controllerRotationSpeed = value;
    }

    #endregion

    #region 3. プロパティ・メッセージレシーバー

    public Transform LockOnTarget
    {
        get => _lockOnTarget;
        set => SetLockOnTarget(value);
    }

    public void OnLook(InputValue value) => _lookInput = value.Get<Vector2>();
    public void OnLeftTrigger(InputValue value) => _lockOnTriggerValue = value.Get<float>();
    public void OnTargetSwitch(InputValue value) => _targetSwitchInput = value.Get<Vector2>();

    #endregion

    #region 4. Unity ライフサイクル

    private void Awake()
    {
        // 修正ポイント：static変数が未設定(-1)の場合、インスペクターの値を即座に反映
        // これにより、スライダーを操作しなくてもインスペクターの値で動くようになります
        if (_mouseRotationSpeed < 0) _mouseRotationSpeed = initialMouseSpeed;
        if (_controllerRotationSpeed < 0) _controllerRotationSpeed = initialControllerSpeed;
    }

    private void Start()
    {
        _playerInput = target?.GetComponentInParent<PlayerInput>();

        if (target != null)
        {
            _lookTargetPosition = target.position + Vector3.up * height;
            _latestTargetPosition = _lookTargetPosition;
            _yaw = target.eulerAngles.y;
            _pitch = 0f;

            transform.rotation = Quaternion.Euler(_pitch, _yaw, 0);
            transform.position = _lookTargetPosition - transform.forward * distance;
        }
    }

    private void Update()
    {
        if (_isFixedViewMode || target == null || Time.timeScale <= 0) return;

        HandleLockOnTimer();
        HandleLockOnInput();
        HandleTargetSwitching();
    }

    private void LateUpdate()
    {
        if (target == null) return;

        UpdateInterpolatedLookTarget();

        if (_isFixedViewMode)
            HandleFixedViewMode();
        else
            HandleTPSViewMode();

        UpdateLockOnUIPosition();

        if (_lockOnTarget != null) RotatePlayerToLockOnTarget();
    }

    #endregion

    #region 5. コアロジック

    private void HandleTPSViewMode()
    {
        float currentSmooth = (_lockOnTarget != null) ? lockOnRotationSpeed : smoothSpeed;

        Quaternion targetRotation = CalculateDesiredRotation();
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * currentSmooth);

        Vector3 anchorPos = target.position + Vector3.up * height;
        anchorPos = AdjustAnchorForCeiling(anchorPos);

        float finalDist = CalculateCollisionDistance(anchorPos);
        Vector3 resultPosition = anchorPos - transform.forward * finalDist;

        resultPosition = ApplyFinalPushBack(resultPosition);

        // カメラ位置の更新。位置の補間は少し速め(×2)に設定
        transform.position = Vector3.Lerp(transform.position, resultPosition, Time.deltaTime * currentSmooth * 2.0f);
    }

    private Quaternion CalculateDesiredRotation()
    {
        if (_lockOnTarget != null)
        {
            Vector3 dir = (_lookTargetPosition - transform.position).normalized;
            if (dir == Vector3.zero) return transform.rotation;
            Quaternion rot = Quaternion.LookRotation(dir);
            _yaw = rot.eulerAngles.y;
            _pitch = rot.eulerAngles.x;
            if (_pitch > 180) _pitch -= 360;
            return rot;
        }

        // デバイス判定の修正：現在の入力デバイスから判断
        bool isGamepad = false;
        if (_playerInput != null && _playerInput.devices.Count > 0)
        {
            // 接続されているデバイスのいずれかがGamepadであればGamepadモード
            isGamepad = _playerInput.devices.Any(d => d is Gamepad);
        }

        // 入力の反映
        if (isGamepad)
        {
            // ゲームパッド（Time.deltaTime を掛ける）
            _yaw += _lookInput.x * _controllerRotationSpeed * Time.deltaTime;
            _pitch -= _lookInput.y * _controllerRotationSpeed * Time.deltaTime;
        }
        else
        {
            // マウス（Time.deltaTime を掛けずに感度係数で調整）
            // マウスのデルタ入力はフレームレートに依存しないため
            _yaw += _lookInput.x * _mouseRotationSpeed * 0.1f;
            _pitch -= _lookInput.y * _mouseRotationSpeed * 0.1f;
        }

        _pitch = Mathf.Clamp(_pitch, pitchMinMax.x, pitchMinMax.y);
        return Quaternion.Euler(_pitch, _yaw, 0);
    }

    private float CalculateCollisionDistance(Vector3 anchor)
    {
        LayerMask mask = collisionLayers | groundLayer | ceilingLayer;
        Vector3 castStart = anchor + transform.forward * 0.5f;
        float castDist = distance + 0.5f;

        if (Physics.SphereCast(castStart, cameraRadius, -transform.forward, out RaycastHit hit, castDist, mask))
        {
            return Mathf.Clamp(hit.distance - collisionOffset - 0.5f, 0.4f, distance);
        }
        return distance;
    }

    // (AdjustAnchorForCeiling, ApplyFinalPushBack, SetLockOnTarget 等は変更なしのため中略)
    #endregion

    #region (省略された内部メソッド)
    private Vector3 AdjustAnchorForCeiling(Vector3 anchor)
    {
        if (Physics.Raycast(target.position + Vector3.up * 0.5f, Vector3.up, out RaycastHit hit, height + ceilingYOffset, ceilingLayer))
        {
            float safeHeight = hit.point.y - (target.position.y + ceilingYOffset);
            anchor.y = target.position.y + Mathf.Max(safeHeight, 0.5f);
        }
        return anchor;
    }

    private Vector3 ApplyFinalPushBack(Vector3 pos)
    {
        if (Physics.Raycast(pos + Vector3.down * 0.1f, Vector3.up, out RaycastHit cHit, cameraRadius + ceilingYOffset, ceilingLayer | collisionLayers))
            pos.y = cHit.point.y - (cameraRadius + ceilingYOffset);
        if (Physics.Raycast(pos + Vector3.up * 0.1f, Vector3.down, out RaycastHit gHit, cameraRadius + groundYOffset, groundLayer | collisionLayers))
            pos.y = gHit.point.y + (cameraRadius + groundYOffset);
        return pos;
    }

    private void SetLockOnTarget(Transform newTarget)
    {
        if (_lockOnTarget == newTarget) return;
        if (_lockOnTarget != null) lockOnUIRect?.gameObject.SetActive(false);
        _latestTargetPosition = _lookTargetPosition;
        _lockOnTarget = newTarget;
        _timer = 0f;
        if (_lockOnTarget != null)
        {
            _lockOnTimer = lockOnDuration;
            if (lockOnUIRect != null) lockOnUIRect.gameObject.SetActive(true);
        }
    }

    private void HandleLockOnInput()
    {
        bool isHold = _lockOnTriggerValue > 0.5f;
        bool isDown = isHold && !_wasControllerLockOnHold;
        bool isUp = !isHold && _wasControllerLockOnHold;
        _wasControllerLockOnHold = isHold;

        if ((Input.GetMouseButtonDown(1) || isDown) && _lockOnTarget == null)
        {
            Vector3 camFwd = transform.forward; camFwd.y = 0; camFwd.Normalize();
            var enemies = Physics.OverlapSphere(target.position, maxLockOnRange, enemyLayer)
                .Where(e => Vector3.Angle(camFwd, e.transform.position - target.position) <= lockOnAngleLimit)
                .OrderBy(e => Vector3.Angle(camFwd, e.transform.position - target.position));
            if (enemies.Any()) LockOnTarget = enemies.First().transform;
        }
        if (_lockOnTarget != null && (Input.GetMouseButtonUp(1) || isUp)) LockOnTarget = null;
    }

    private void HandleTargetSwitching()
    {
        if (_lockOnTarget == null) return;
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        bool isSwitch = Mathf.Abs(_targetSwitchInput.x) > 0.5f;
        if (scroll == 0 && !isSwitch) return;
        bool toRight = scroll > 0 || (isSwitch && _targetSwitchInput.x > 0);
        if (isSwitch) _targetSwitchInput = Vector2.zero;
        Vector3 pPos = target.position;
        Vector3 toCur = (_lockOnTarget.position - pPos).normalized;
        var next = Physics.OverlapSphere(pPos, maxLockOnRange, enemyLayer)
            .Where(c => c.transform != _lockOnTarget)
            .Select(c => new { T = c.transform, A = Vector3.SignedAngle(toCur, (c.transform.position - pPos).normalized, Vector3.up) })
            .Where(t => (toRight && t.A > 0) || (!toRight && t.A < 0))
            .OrderBy(t => Mathf.Abs(t.A)).FirstOrDefault();
        if (next != null) LockOnTarget = next.T;
    }

    private void HandleLockOnTimer()
    {
        if (_lockOnTarget == null) return;
        if (_lockOnTriggerValue > 0.5f) { _lockOnTimer = lockOnDuration; return; }
        _lockOnTimer -= Time.deltaTime;
        if (_lockOnTimer <= 0) LockOnTarget = null;
    }

    private void UpdateInterpolatedLookTarget()
    {
        Vector3 defaultLook = target.position + Vector3.up * height;
        Vector3 targetLook = (_lockOnTarget != null) ? _lockOnTarget.position + Vector3.up * 1.5f : defaultLook;
        if (_timer < changeDuration)
        {
            _timer += Time.deltaTime;
            _lookTargetPosition = Vector3.Lerp(_latestTargetPosition, targetLook, _timer / changeDuration);
        }
        else _lookTargetPosition = targetLook;
    }

    private void UpdateLockOnUIPosition()
    {
        if (_lockOnTarget == null || lockOnUIRect == null)
        {
            if (lockOnUIRect != null) lockOnUIRect.gameObject.SetActive(false);
            return;
        }
        Vector3 screenPos = Camera.main.WorldToScreenPoint(_lockOnTarget.position + Vector3.up * 1.5f);
        if (screenPos.z < 0) { lockOnUIRect.gameObject.SetActive(false); return; }
        lockOnUIRect.gameObject.SetActive(true);
        lockOnUIRect.position = screenPos;
    }

    public void RotatePlayerToLockOnTarget()
    {
        Vector3 d = Vector3.ProjectOnPlane(_lockOnTarget.position - target.position, Vector3.up);
        if (d.sqrMagnitude < 0.001f) return;
        target.rotation = Quaternion.Slerp(target.rotation, Quaternion.LookRotation(d), Time.deltaTime * smoothSpeed * 1.5f);
    }

    private void HandleFixedViewMode()
    {
        transform.position = Vector3.Lerp(transform.position, _fixedTargetPosition, Time.unscaledDeltaTime * _fixedViewSmoothSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, _fixedTargetRotation, Time.unscaledDeltaTime * _fixedViewSmoothSpeed);
    }

    /// <summary>
    /// プレイヤー（target）をカメラの水平な正面方向に回転させます。
    /// LateUpdate などで呼び出すことを想定しています。
    /// </summary>
    public void RotatePlayerToCameraDirection()
    {
        if (target == null) return;

        // カメラの正面方向を取得し、垂直方向（Y軸）の成分をカットする
        Vector3 cameraForward = transform.forward;
        cameraForward.y = 0;

        if (cameraForward.sqrMagnitude > 0.001f)
        {
            // カメラの水平な向きに合わせてプレイヤーを回転させる
            Quaternion targetRotation = Quaternion.LookRotation(cameraForward);
            target.rotation = Quaternion.Slerp(target.rotation, targetRotation, Time.deltaTime * smoothSpeed);
        }
    }

    /// <summary>
    /// マウスの回転感度を更新します
    /// </summary>
    public static void SetMouseSensitivity(float newSensitivity)
    {
        // static変数である MouseRotationSpeed を更新
        MouseRotationSpeed = newSensitivity;
    }

    /// <summary>
    /// コントローラーの回転感度を更新します
    /// </summary>
    public static void SetControllerSensitivity(float newSensitivity)
    {
        // static変数である ControllerRotationSpeed を更新
        ControllerRotationSpeed = newSensitivity;
    }

    #endregion
}