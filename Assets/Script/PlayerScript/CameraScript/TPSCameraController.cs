using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Linq;

/// <summary>
/// ターゲットを追跡し、Input Systemのアクションで操作可能な三人称視点（TPS）カメラを制御します。
/// 地面へのめり込み防止と、地面接触時の視線制限機能を含みます。
/// </summary>
public class TPSCameraController : MonoBehaviour
{
    // === 1. 設定: カメラ, 追従, 回転 ===
    [Header("1. Target and Camera Control")]
    public Transform target;
    public float distance = 5.0f;
    public float height = 2.0f;

    public float initialMouseRotationSpeed = 3.0f;
    public float initialControllerRotationSpeed = 500.0f;

    public static float MouseRotationSpeed { get; private set; }
    public static float ControllerRotationSpeed { get; private set; }

    public float smoothSpeed = 10.0f;
    public Vector2 pitchMinMax = new Vector2(-40, 85);

    // === 2. 設定: ロックオン ===
    [Header("2. Lock-On Settings")]
    public float lockOnRotationSpeed = 50f;
    public float maxLockOnRange = 30f;
    public float maxLockOnKeepRange = 40f;
    public LayerMask enemyLayer;
    public float lockOnAngleLimit = 60f;
    public float lockOnDuration = 3.0f;
    [SerializeField] private float _changeDuration = 0.3f;

    // === 3. 設定: 衝突 & UI & 地面 ===
    [Header("3. Collision & Ground Settings")]
    public LayerMask collisionLayers;
    public LayerMask ceilingLayer;
    public LayerMask groundLayer;      // ★ 地面判定用のレイヤー (Layer: Ground等を設定)
    public float groundYOffset = 0.5f; // ★ 地面から離す最低距離
    public float collisionOffset = 0.2f;
    public float cameraRadius = 0.3f;
    public float ceilingYOffset = 0.5f;
    public RectTransform lockOnUIRect;

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

    private PlayerInput _playerInput;
    private Vector2 _lookInput = Vector2.zero;
    private float _lockOnTriggerValue = 0f;
    private Vector2 _targetSwitchInput = Vector2.zero;
    private bool wasControllerLockOnHoldThisFrame = false;

    // --- メッセージレシーバー ---
    public void OnLook(InputValue value) => _lookInput = value.Get<Vector2>();
    public void OnLeftTrigger(InputValue value) => _lockOnTriggerValue = value.Get<float>();
    public void OnTargetSwitch(InputValue value) => _targetSwitchInput = value.Get<Vector2>();

    public Transform LockOnTarget
    {
        get => _lockOnTarget;
        set
        {
            if (_lockOnTarget == value) return;
            if (_lockOnTarget != null) lockOnUIRect?.gameObject.SetActive(false);
            _latestTargetPosition = _lookTargetPosition;
            _lockOnTarget = value;
            _timer = 0f;
            if (_lockOnTarget != null)
            {
                _lockOnTimer = lockOnDuration;
                if (lockOnUIRect != null) lockOnUIRect.gameObject.SetActive(true);
            }
        }
    }

    public static void SetMouseSensitivity(float value) => MouseRotationSpeed = value;
    public static void SetControllerSensitivity(float value) => ControllerRotationSpeed = value;

    void Start()
    {
        _playerInput = target?.GetComponentInParent<PlayerInput>();
        if (MouseRotationSpeed <= 0f)
        {
            MouseRotationSpeed = initialMouseRotationSpeed;
            ControllerRotationSpeed = initialControllerRotationSpeed;
        }

        _cursorLockedInitially = true;
        if (target != null)
        {
            _lookTargetPosition = target.position + Vector3.up * 1.5f;
            _latestTargetPosition = _lookTargetPosition;
            _yaw = target.eulerAngles.y;
            _pitch = transform.eulerAngles.x;
            if (_pitch > 180) _pitch -= 360;
        }
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

        Vector3 defaultLookPosition = target.position + Vector3.up * height;
        Vector3 targetLookPosition = _lockOnTarget != null ? _lockOnTarget.position + Vector3.up * 1.5f : defaultLookPosition;
        UpdateLookTargetPosition(targetLookPosition);

        if (_isFixedViewMode) HandleFixedViewMode();
        else HandleTPSViewMode();

        UpdateLockOnUIPosition();
        if (_lockOnTarget != null) RotatePlayerToLockOnTarget();
    }

    // =======================================================
    // TPS View Logic (地面制限付き)
    // =======================================================

    private void HandleTPSViewMode()
    {
        Quaternion targetRotation;
        float currentRotationSmoothSpeed;

        // 1. 基本回転の計算
        if (_lockOnTarget != null)
        {
            Vector3 directionToTarget = (_lookTargetPosition - transform.position).normalized;
            targetRotation = Quaternion.LookRotation(directionToTarget);
            _yaw = targetRotation.eulerAngles.y;
            _pitch = targetRotation.eulerAngles.x;
            if (_pitch > 180) _pitch -= 360;
            currentRotationSmoothSpeed = lockOnRotationSpeed;
        }
        else
        {
            targetRotation = CalculateRotationFromInput();
            currentRotationSmoothSpeed = smoothSpeed;
        }

        // 2. 理想的な位置を計算
        Vector3 idealPosition = CalculateTargetPosition(targetRotation);

        // 3. 天井制限
        idealPosition = CheckCeilingYConstraint(idealPosition);

        // 4. 地面・障害物衝突判定を適用して最終位置を決定
        Vector3 finalPosition = ApplyCollisionCheck(idealPosition);

        // 5. ★ 地面制限による回転（視線）の補正
        // カメラが地面の押し上げ（groundYOffset）により、本来のピッチ計算より高い位置にいる場合、
        // 視線が地面の下を向かないようにプレイヤー頭上を向くように回転を上書きする
        if (finalPosition.y > idealPosition.y + 0.01f)
        {
            Vector3 lookDir = (target.position + Vector3.up * height) - finalPosition;
            if (lookDir != Vector3.zero)
            {
                targetRotation = Quaternion.LookRotation(lookDir);
                // 内部変数も更新して、急激な挙動変化を防ぐ
                _pitch = targetRotation.eulerAngles.x;
                if (_pitch > 180) _pitch -= 360;
            }
        }

        // 6. 最終的な適用
        transform.position = Vector3.Lerp(transform.position, finalPosition, Time.deltaTime * currentRotationSmoothSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * currentRotationSmoothSpeed);
    }

    private Quaternion CalculateRotationFromInput()
    {
        float deltaYaw = 0, deltaPitch = 0;
        bool isGamepad = _playerInput != null && _playerInput.currentControlScheme != null && _playerInput.currentControlScheme.Contains("Controller");

        if (isGamepad)
        {
            deltaYaw = _lookInput.x * ControllerRotationSpeed * Time.deltaTime;
            deltaPitch = _lookInput.y * ControllerRotationSpeed * Time.deltaTime;
        }
        else
        {
            // Input SystemのLookアクション値を使用
            deltaYaw = _lookInput.x * MouseRotationSpeed;
            deltaPitch = _lookInput.y * MouseRotationSpeed;

            // Input Systemからの入力が極端に小さい場合は、従来のInput Managerからも補助的に取る（安全策）
            if (Mathf.Abs(deltaYaw) < 0.001f && Mathf.Abs(deltaPitch) < 0.001f)
            {
                deltaYaw = Input.GetAxis("Mouse X") * MouseRotationSpeed;
                deltaPitch = Input.GetAxis("Mouse Y") * MouseRotationSpeed;
            }
        }

        _yaw += deltaYaw;
        _pitch -= deltaPitch;
        _pitch = Mathf.Clamp(_pitch, pitchMinMax.x, pitchMinMax.y);
        return Quaternion.Euler(_pitch, _yaw, 0);
    }

    private Vector3 CalculateTargetPosition(Quaternion rotation)
    {
        Vector3 camCenter = target.position + Vector3.up * height;
        return camCenter - rotation * Vector3.forward * distance;
    }

    // 衝突判定と地面埋まり防止の統合修正版
    private Vector3 ApplyCollisionCheck(Vector3 initialPosition)
    {
        Vector3 currentTargetPos = target.position + Vector3.up * height;
        Vector3 direction = (initialPosition - currentTargetPos).normalized;
        float travelDistance = Vector3.Distance(currentTargetPos, initialPosition);

        // 1. 壁・障害物判定 (SphereCastでカメラの大きさを考慮)
        if (Physics.SphereCast(currentTargetPos, cameraRadius, direction, out RaycastHit hit, travelDistance, collisionLayers))
        {
            initialPosition = currentTargetPos + direction * Mathf.Max(hit.distance, 0.1f);
        }

        // 2. 地面判定の強化
        if (groundLayer != 0)
        {
            // 地面判定を「カメラの少し上」から下へ飛ばす (埋まっていても検知できるように)
            float rayLength = groundYOffset + 1.0f;
            Vector3 rayStart = initialPosition + Vector3.up * 0.5f; // 0.5m上からチェック開始

            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit groundHit, rayLength, groundLayer))
            {
                float minAllowedY = groundHit.point.y + groundYOffset;
                if (initialPosition.y < minAllowedY)
                {
                    initialPosition.y = minAllowedY;
                }
            }
        }
        return initialPosition;
    }

    // =======================================================
    // その他補助メソッド (ロックオン・UI等)
    // =======================================================

    private Vector3 CheckCeilingYConstraint(Vector3 pos)
    {
        if (ceilingLayer == 0) return pos;

        // ターゲットの頭上からカメラ位置に向かって天井がないかチェック
        Vector3 checkStart = target.position + Vector3.up * height;
        float distToCam = Vector3.Distance(checkStart, pos);
        Vector3 dirToCam = (pos - checkStart).normalized;

        // 天井レイヤーに対して球体判定
        if (Physics.SphereCast(checkStart, cameraRadius, dirToCam, out RaycastHit hit, distToCam, ceilingLayer))
        {
            // 天井にぶつかるなら、その手前で止める
            pos = checkStart + dirToCam * Mathf.Max(hit.distance - 0.1f, 0.2f);

            // さらに絶対的な高さ制限
            if (pos.y > hit.point.y - ceilingYOffset)
            {
                pos.y = hit.point.y - ceilingYOffset;
            }
        }
        return pos;
    }

    private void UpdateLookTargetPosition(Vector3 tp)
    {
        if (_timer < _changeDuration)
        {
            _timer += Time.deltaTime;
            _lookTargetPosition = Vector3.Lerp(_latestTargetPosition, tp, _timer / _changeDuration);
        }
        else _lookTargetPosition = tp;
    }

    private void HandleLockOnTimer()
    {
        if (_lockOnTarget == null) return;
        if (_lockOnTriggerValue > 0.5f) { _lockOnTimer = lockOnDuration; return; }
        _lockOnTimer -= Time.deltaTime;
        if (_lockOnTimer <= 0) LockOnTarget = null;
    }

    private void HandleLockOnInput()
    {
        bool isCtrlHold = _lockOnTriggerValue > 0.5f;
        bool isCtrlDown = isCtrlHold && !wasControllerLockOnHoldThisFrame;
        bool isCtrlUp = !isCtrlHold && wasControllerLockOnHoldThisFrame;
        wasControllerLockOnHoldThisFrame = isCtrlHold;

        if ((Input.GetMouseButtonDown(1) || isCtrlDown) && _lockOnTarget == null)
        {
            Vector3 camFwd = transform.forward; camFwd.y = 0; camFwd.Normalize();
            var enemies = Physics.OverlapSphere(target.position, maxLockOnRange, enemyLayer)
                .Where(c => Vector3.Angle(camFwd, c.transform.position - target.position) <= lockOnAngleLimit);
            if (enemies.Any()) LockOnTarget = enemies.OrderBy(c => Vector3.Angle(camFwd, c.transform.position - target.position)).First().transform;
        }
        if (_lockOnTarget != null && (Input.GetMouseButtonUp(1) || isCtrlUp)) LockOnTarget = null;
    }

    private void HandleTargetSwitching()
    {
        if (_lockOnTarget == null) return;
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        bool isSwitch = Mathf.Abs(_targetSwitchInput.x) > 0.5f;
        if (scroll == 0 && !isSwitch) return;
        bool right = scroll > 0 || (isSwitch && _targetSwitchInput.x > 0);
        if (isSwitch) _targetSwitchInput = Vector2.zero;

        Vector3 pPos = target.position;
        Vector3 toCur = (_lockOnTarget.position - pPos).normalized;
        var next = Physics.OverlapSphere(pPos, maxLockOnRange, enemyLayer)
            .Where(c => c.transform != _lockOnTarget)
            .Select(c => new { T = c.transform, A = Vector3.SignedAngle(toCur, (c.transform.position - pPos).normalized, Vector3.up) })
            .Where(t => (right && t.A > 0) || (!right && t.A < 0))
            .OrderBy(t => Mathf.Abs(t.A)).FirstOrDefault();
        if (next != null) LockOnTarget = next.T;
    }

    private void HandleFixedViewMode()
    {
        transform.position = Vector3.Lerp(transform.position, _fixedTargetPosition, Time.unscaledDeltaTime * _fixedViewSmoothSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, _fixedTargetRotation, Time.unscaledDeltaTime * _fixedViewSmoothSpeed);
    }

    public void SetFixedCameraView(Vector3 p, Quaternion r, float s) { _isFixedViewMode = true; _fixedTargetPosition = p; _fixedTargetRotation = r; _fixedViewSmoothSpeed = s; }
    public void ResetToTPSView(float s) { _fixedViewSmoothSpeed = s; _isFixedViewMode = false; }

    private void UpdateLockOnUIPosition()
    {
        if (_lockOnTarget == null || lockOnUIRect == null) { if (lockOnUIRect != null) lockOnUIRect.gameObject.SetActive(false); return; }
        Vector3 screenPos = Camera.main.WorldToScreenPoint(_lockOnTarget.position + Vector3.up * 1.5f);
        if (screenPos.z < 0) { lockOnUIRect.gameObject.SetActive(false); return; }
        lockOnUIRect.gameObject.SetActive(true); lockOnUIRect.position = screenPos;
    }

    public void RotatePlayerToLockOnTarget()
    {
        if (target == null || _lockOnTarget == null) return;
        Vector3 d = _lockOnTarget.position - target.position; d.y = 0;
        if (d.sqrMagnitude < 0.001f) return;
        target.rotation = Quaternion.Slerp(target.rotation, Quaternion.LookRotation(d), Time.deltaTime * smoothSpeed * 1.5f);
    }

    public void RotatePlayerToCameraDirection()
    {
        if (target == null || _isFixedViewMode || _lockOnTarget != null) return;
        float h = Input.GetAxisRaw("Horizontal"), v = Input.GetAxisRaw("Vertical");
        if (h == 0 && v == 0) return;
        Vector3 d = Quaternion.Euler(0, transform.eulerAngles.y, 0) * new Vector3(h, 0, v).normalized;
        target.rotation = Quaternion.Slerp(target.rotation, Quaternion.LookRotation(d), Time.deltaTime * smoothSpeed);
    }
}