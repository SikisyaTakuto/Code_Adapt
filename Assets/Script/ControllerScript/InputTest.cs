using UnityEngine;
using UnityEngine.InputSystem;

// Controllerは、Input Actionアセットから生成されたC#クラス名と仮定します。
// IControllerInputActions は、アクションマップ名「ControllerInput」に対応するインターフェースと仮定します。
public class InputTest : MonoBehaviour, Controller.IControllerInputActions
{
    private Controller _controls;
    private Vector2 _moveInput;
    private Vector2 _lookInput;

    void Awake()
    {
        _controls = new Controller();

        // アクションマップ「ControllerInput」のコールバックをこのスクリプトに設定
        _controls.ControllerInput.SetCallbacks(this);

        Debug.Log("InputTest: 初期化完了。コントローラー操作を開始してください。");
    }

    private void OnEnable()
    {
        _controls.Enable();
    }

    private void OnDisable()
    {
        _controls.Disable();
    }

    void Update()
    {
        // 連続的な入力を確認 (Move/Lookなど)
        if (_moveInput.magnitude > 0.1f)
        {
            Debug.Log($"[UPDATE] MOVE: {_moveInput:F2}");
        }
        if (_lookInput.magnitude > 0.1f)
        {
            Debug.Log($"[UPDATE] LOOK: {_lookInput:F2}");
        }
    }

    // =============================== IControllerInputActions の実装 (全アクション) ===============================

    // 1. スティック入力 (Move/Look)
    public void OnMove(InputAction.CallbackContext context)
    {
        _moveInput = context.ReadValue<Vector2>();
        if (context.started) Debug.Log($"[STICK] Move Started");
        if (context.canceled)
        {
            Debug.Log($"[STICK] Move Canceled");
            _moveInput = Vector2.zero;
        }
        // 連続的な値はUpdateで出力されます。
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        _lookInput = context.ReadValue<Vector2>();
        if (context.started) Debug.Log($"[STICK] Look Started");
        if (context.canceled)
        {
            Debug.Log($"[STICK] Look Canceled");
            _lookInput = Vector2.zero;
        }
        // 連続的な値はUpdateで出力されます。
    }

    // 2. D-Pad 入力 (DPad)
    public void OnDPad(InputAction.CallbackContext context)
    {
        // DPadはValue: Vector2であり、十字キーの方向を示します。
        if (context.performed)
        {
            Vector2 dpadValue = context.ReadValue<Vector2>();
            Debug.Log($"[DPAD] DPad 押されました: {dpadValue}");
        }
    }


    // 3. ボタン入力 (A, B, X, Y, Shoulder, StickPress, Start, Select)

    public void OnAButton(InputAction.CallbackContext context)
    {
        if (context.performed) Debug.Log("--- A BUTTON: Pushed (performed) ---");
        if (context.canceled) Debug.Log("--- A BUTTON: Released (canceled) ---");
    }

    public void OnBButton(InputAction.CallbackContext context)
    {
        if (context.performed) Debug.Log("--- B BUTTON: Pushed (performed) ---");
        if (context.canceled) Debug.Log("--- B BUTTON: Released (canceled) ---");
    }

    public void OnXButton(InputAction.CallbackContext context)
    {
        if (context.performed) Debug.Log("--- X BUTTON: Pushed (performed) ---");
        if (context.canceled) Debug.Log("--- X BUTTON: Released (canceled) ---");
    }

    public void OnYButton(InputAction.CallbackContext context)
    {
        if (context.performed) Debug.Log("--- Y BUTTON: Pushed (performed) ---");
        if (context.canceled) Debug.Log("--- Y BUTTON: Released (canceled) ---");
    }

    public void OnLeftShoulder(InputAction.CallbackContext context)
    {
        if (context.performed) Debug.Log("[SHOULDER] Left Shoulder (L1/LB) Pushed!");
        if (context.canceled) Debug.Log("[SHOULDER] Left Shoulder (L1/LB) Released.");
    }

    public void OnRightShoulder(InputAction.CallbackContext context)
    {
        if (context.performed) Debug.Log("[SHOULDER] Right Shoulder (R1/RB) Pushed!");
        if (context.canceled) Debug.Log("[SHOULDER] Right Shoulder (R1/RB) Released.");
    }

    public void OnLeftStickPress(InputAction.CallbackContext context)
    {
        if (context.performed) Debug.Log("[STICK PRESS] Left Stick Pushed!");
        if (context.canceled) Debug.Log("[STICK PRESS] Left Stick Released.");
    }

    public void OnRightStickPress(InputAction.CallbackContext context)
    {
        if (context.performed) Debug.Log("[STICK PRESS] Right Stick Pushed!");
        if (context.canceled) Debug.Log("[STICK PRESS] Right Stick Released.");
    }

    public void OnStartButton(InputAction.CallbackContext context)
    {
        if (context.performed) Debug.Log("[SYSTEM] Start Button (Menu) Pushed!");
        if (context.canceled) Debug.Log("[SYSTEM] Start Button (Menu) Released.");
    }

    public void OnSelectButton(InputAction.CallbackContext context)
    {
        if (context.performed) Debug.Log("[SYSTEM] Select/View Button Pushed!");
        if (context.canceled) Debug.Log("[SYSTEM] Select/View Button Released.");
    }


    // 4. トリガー入力 (Trigger - Value: Axis)

    public void OnLeftTrigger(InputAction.CallbackContext context)
    {
        float triggerValue = context.ReadValue<float>();

        if (triggerValue > 0.01f)
        {
            Debug.Log($"[TRIGGER] Left Trigger (L2/LT): Value = {triggerValue:F2}");
        }
        else if (context.canceled)
        {
            Debug.Log("[TRIGGER] Left Trigger: Fully Released.");
        }
    }

    public void OnRightTrigger(InputAction.CallbackContext context)
    {
        float triggerValue = context.ReadValue<float>();

        if (triggerValue > 0.01f)
        {
            Debug.Log($"[TRIGGER] Right Trigger (R2/RT): Value = {triggerValue:F2}");
        }
        else if (context.canceled)
        {
            Debug.Log("[TRIGGER] Right Trigger: Fully Released.");
        }
    }
}