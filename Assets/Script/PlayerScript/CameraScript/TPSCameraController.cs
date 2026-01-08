using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;

/// <summary>
/// 高機能三人称視点（TPS）カメラコントローラー
/// 機能：ターゲット追従、Input System/旧Input両対応、障害物回避（壁・天井・地面）、ロックオン
/// </summary>
public class TPSCameraController : MonoBehaviour
{
    #region 1. 構造・設定データ (Inspector)

    [Header("--- 基本追従設定 ---")]
    [SerializeField] private Transform target;           // 追従する対象（プレイヤーなど）
    [SerializeField] private float distance = 5.0f;     // ターゲットとの基本距離
    [SerializeField] private float height = 2.0f;       // ターゲットの足元からのカメラ支点の高さ
    [SerializeField] private float smoothSpeed = 10.0f; // カメラの移動・回転の滑らかさ
    [SerializeField] private Vector2 pitchMinMax = new Vector2(-40, 85); // 上下回転の角度制限

    [Header("--- 初期感度設定 ---")]
    [SerializeField] private float initialMouseSpeed =1000000.0f; //カメラがどれくらい速く回転するか
    [SerializeField] private float initialControllerSpeed = 500.0f; //ゲームパッド（コントローラー）のスティックを倒したときの回転速度

    [Header("--- ロックオン設定 ---")]
    [SerializeField] private float lockOnRotationSpeed = 50f; // ロックオン時の旋回速度
    [SerializeField] private float maxLockOnRange = 30f;      // ロックオン可能な距離
    [SerializeField] private LayerMask enemyLayer;            // 敵として認識するレイヤー
    [SerializeField] private float lockOnAngleLimit = 60f;    // 画面中央からの有効角度
    [SerializeField] private float lockOnDuration = 3.0f;     // 入力を離した後の維持時間
    [SerializeField] private float changeDuration = 0.3f;     // 注視点切り替えの補間時間
    [SerializeField] private RectTransform lockOnUIRect;      // ロックオン時に表示するUI

    [Header("--- 衝突判定・地形保護 ---")]
    [SerializeField] private LayerMask collisionLayers; // 壁などの障害物
    [SerializeField] private LayerMask ceilingLayer;   // 天井専用（頭上を押し下げる）
    [SerializeField] private LayerMask groundLayer;    // 地面専用（足元を押し上げる）
    [SerializeField] private float cameraRadius = 0.3f; // カメラの物理的な大きさ
    [SerializeField] private float collisionOffset = 0.2f; // 壁から離す余裕
    [SerializeField] private float groundYOffset = 0.5f;   // 地面からの最低浮上距離
    [SerializeField] private float ceilingYOffset = 0.5f;  // 天井からの最低押し下げ距離

    #endregion

    #region 2. 内部変数 (Private)

    // 回転・位置管理
    private float _yaw;
    private float _pitch;
    private Vector3 _lookTargetPosition;  // 現在の注視点
    private Vector3 _latestTargetPosition; // 前フレームの注視点
    private Transform _lockOnTarget;

    // タイマー・状態管理
    private float _timer;
    private float _lockOnTimer;
    private bool _isFixedViewMode;

    // 固定視点モード用
    private Vector3 _fixedTargetPosition;
    private Quaternion _fixedTargetRotation;
    private float _fixedViewSmoothSpeed;

    // 入力キャッシュ
    private PlayerInput _playerInput;
    private Vector2 _lookInput;
    private float _lockOnTriggerValue;
    private Vector2 _targetSwitchInput;
    private bool _wasControllerLockOnHold;

    // 静的プロパティ（感度調整用）
    public static float MouseRotationSpeed { get; private set; }
    public static float ControllerRotationSpeed { get; private set; }

    #endregion

    #region 3. プロパティ・メッセージレシーバー

    public Transform LockOnTarget
    {
        get => _lockOnTarget;
        set => SetLockOnTarget(value);
    }

    // Input System からのメッセージ受け取り
    public void OnLook(InputValue value) => _lookInput = value.Get<Vector2>();
    public void OnLeftTrigger(InputValue value) => _lockOnTriggerValue = value.Get<float>();
    public void OnTargetSwitch(InputValue value) => _targetSwitchInput = value.Get<Vector2>();

    // 感度設定用（外部からアクセス可能）
    public static void SetMouseSensitivity(float value) => MouseRotationSpeed = value;
    public static void SetControllerSensitivity(float value) => ControllerRotationSpeed = value;

    #endregion

    #region 4. Unity ライフサイクル

    private void Start()
    {
        _playerInput = target?.GetComponentInParent<PlayerInput>();

        // 感度の初期化
        if (MouseRotationSpeed <= 0f)
        {
            MouseRotationSpeed = initialMouseSpeed;
            ControllerRotationSpeed = initialControllerSpeed;
        }

        // 初期位置の設定
        if (target != null)
        {
            _lookTargetPosition = target.position + Vector3.up * 1.5f;
            _latestTargetPosition = _lookTargetPosition;
            _yaw = target.eulerAngles.y;
            _pitch = transform.eulerAngles.x;
            if (_pitch > 180) _pitch -= 360;
        }
    }

    private void Update()
    {
        if (_isFixedViewMode || target == null || Time.timeScale <= 0) return;

        HandleLockOnTimer();    // ロックオンの持続時間管理
        HandleLockOnInput();    // ロックオンの開始・解除入力
        HandleTargetSwitching(); // ロックオン対象の切り替え
    }

    private void LateUpdate()
    {
        if (target == null) return;

        UpdateInterpolatedLookTarget(); // 注視点の補間更新

        if (_isFixedViewMode)
            HandleFixedViewMode();
        else
            HandleTPSViewMode();

        UpdateLockOnUIPosition(); // ロックオンUIをターゲットに追従

        // ロックオン中、プレイヤーを敵の方向へ向かせる
        if (_lockOnTarget != null) RotatePlayerToLockOnTarget();
    }

    #endregion

    #region 5. コアロジック (TPSカメラ移動・衝突判定)

    private void HandleTPSViewMode()
    {
        float currentSmooth = (_lockOnTarget != null) ? lockOnRotationSpeed : smoothSpeed;

        // 1. 回転の計算
        Quaternion targetRotation = CalculateDesiredRotation();
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * currentSmooth);

        // 2. 支点の計算（天井がある場合は押し下げる）
        Vector3 anchorPos = target.position + Vector3.up * height;
        anchorPos = AdjustAnchorForCeiling(anchorPos);

        // 3. 障害物との距離計算 (SphereCast)
        float finalDist = CalculateCollisionDistance(anchorPos);
        Vector3 resultPosition = anchorPos - transform.forward * finalDist;

        // 4. 地面・天井への最終的な押し返し
        resultPosition = ApplyFinalPushBack(resultPosition);

        // 5. 滑らかに位置を適用
        transform.position = Vector3.Lerp(transform.position, resultPosition, Time.deltaTime * currentSmooth * 2.0f);
    }

    private Quaternion CalculateDesiredRotation()
    {
        // A. ロックオン中：ターゲットを向く
        if (_lockOnTarget != null)
        {
            Vector3 dir = (_lookTargetPosition - transform.position).normalized;
            Quaternion rot = Quaternion.LookRotation(dir);
            _yaw = rot.eulerAngles.y;
            _pitch = rot.eulerAngles.x;
            if (_pitch > 180) _pitch -= 360;
            return rot;
        }

        // 通常時：入力を元に回転
        bool isGamepad = _playerInput != null && _playerInput.currentControlScheme != null && _playerInput.currentControlScheme.Contains("Controller");

        // 感度の適用
        float sensitivity = isGamepad ? ControllerRotationSpeed : MouseRotationSpeed;

        // マウス入力にはデルタタイムを掛けつつ、少し強めの係数を設定することも可能です
        _yaw += _lookInput.x * sensitivity * Time.deltaTime;
        _pitch -= _lookInput.y * sensitivity * Time.deltaTime;

        // 上下の角度を制限
        _pitch = Mathf.Clamp(_pitch, pitchMinMax.x, pitchMinMax.y);

        return Quaternion.Euler(_pitch, _yaw, 0);
    }

    /// <summary>
    /// 壁や障害物を検知してカメラの距離を短くする
    /// </summary>
    private float CalculateCollisionDistance(Vector3 anchor)
    {
        LayerMask mask = collisionLayers | groundLayer | ceilingLayer;
        if (Physics.SphereCast(anchor, cameraRadius, -transform.forward, out RaycastHit hit, distance, mask))
        {
            return Mathf.Max(hit.distance - collisionOffset, 0.1f);
        }
        return distance;
    }

    /// <summary>
    /// 支点が天井に埋まらないよう調整
    /// </summary>
    private Vector3 AdjustAnchorForCeiling(Vector3 anchor)
    {
        if (Physics.Raycast(target.position + Vector3.up * 0.5f, Vector3.up, out RaycastHit hit, height + ceilingYOffset, ceilingLayer))
        {
            float safeHeight = hit.point.y - (target.position.y + ceilingYOffset);
            anchor.y = target.position.y + Mathf.Max(safeHeight, 0.5f);
        }
        return anchor;
    }

    /// <summary>
    /// カメラが地面や天井にめり込むのを最終的に防ぐ
    /// </summary>
    private Vector3 ApplyFinalPushBack(Vector3 pos)
    {
        // 天井チェック
        if (Physics.Raycast(pos + Vector3.down * 0.1f, Vector3.up, out RaycastHit cHit, cameraRadius + ceilingYOffset, ceilingLayer | collisionLayers))
            pos.y = cHit.point.y - (cameraRadius + ceilingYOffset);

        // 地面チェック
        if (Physics.Raycast(pos + Vector3.up * 0.1f, Vector3.down, out RaycastHit gHit, cameraRadius + groundYOffset, groundLayer | collisionLayers))
            pos.y = gHit.point.y + (cameraRadius + groundYOffset);

        return pos;
    }

    #endregion

    #region 6. ロックオン・ターゲット管理

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

        // ロックオン開始（右クリックまたはトリガー）
        if ((Input.GetMouseButtonDown(1) || isDown) && _lockOnTarget == null)
        {
            Vector3 camFwd = transform.forward; camFwd.y = 0; camFwd.Normalize();
            var enemies = Physics.OverlapSphere(target.position, maxLockOnRange, enemyLayer)
                .Where(e => Vector3.Angle(camFwd, e.transform.position - target.position) <= lockOnAngleLimit)
                .OrderBy(e => Vector3.Angle(camFwd, e.transform.position - target.position));

            if (enemies.Any()) LockOnTarget = enemies.First().transform;
        }

        // ロックオン解除
        if (_lockOnTarget != null && (Input.GetMouseButtonUp(1) || isUp))
        {
            LockOnTarget = null;
        }
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

    #endregion

    #region 7. 補助機能 (UI, プレイヤー回転, 固定視点)

    /// <summary>
    /// 注視点（カメラの向きの先）を滑らかに補間する
    /// </summary>
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

    public void RotatePlayerToCameraDirection()
    {
        if (target == null || _isFixedViewMode || _lockOnTarget != null) return;
        float h = Input.GetAxisRaw("Horizontal"), v = Input.GetAxisRaw("Vertical");
        if (h == 0 && v == 0) return;
        Vector3 moveDir = Quaternion.Euler(0, transform.eulerAngles.y, 0) * new Vector3(h, 0, v).normalized;
        target.rotation = Quaternion.Slerp(target.rotation, Quaternion.LookRotation(moveDir), Time.deltaTime * smoothSpeed);
    }

    private void HandleFixedViewMode()
    {
        transform.position = Vector3.Lerp(transform.position, _fixedTargetPosition, Time.unscaledDeltaTime * _fixedViewSmoothSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, _fixedTargetRotation, Time.unscaledDeltaTime * _fixedViewSmoothSpeed);
    }

    public void SetFixedCameraView(Vector3 p, Quaternion r, float s) { _isFixedViewMode = true; _fixedTargetPosition = p; _fixedTargetRotation = r; _fixedViewSmoothSpeed = s; }
    public void ResetToTPSView(float s) { _fixedViewSmoothSpeed = s; _isFixedViewMode = false; }

    #endregion
}