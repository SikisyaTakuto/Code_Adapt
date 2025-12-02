using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Input System で設定されたコントローラーの入力をテストし、ログに出力する。
/// </summary>
public class ControllerTest : MonoBehaviour
{
    // ★ 修正: Input Actions アセット名 'PiyerControls' に基づく自動生成クラス名
    private InputSystem_Actions controls;

    // 現在の入力値を保持する変数
    private Vector2 currentMove;
    private Vector2 currentLook;

    void Awake()
    {
        // ★ 修正: インスタンス生成
        controls = new InputSystem_Actions();

        // --- 2. 各アクションへのコールバック登録 (Action Map名 'Player' を使用) ---

        // Value / Vector2 アクション (移動、視点移動)
        controls.PlyerControls.Move.performed += ctx => currentMove = ctx.ReadValue<Vector2>();
        controls.PlyerControls.Move.canceled += ctx => currentMove = Vector2.zero;

        controls.Player.Look.performed += ctx => currentLook = ctx.ReadValue<Vector2>();
        controls.Player.Look.canceled += ctx => currentLook = Vector2.zero;

        // Vector2 (D-Pad)
        controls.PlyerControls.ArmorMode.performed += OnArmorMode;
        controls.PlyerControls.ArmorMode.canceled += OnArmorModeCanceled;

        // Button アクション (押した瞬間に反応)
        controls.PlyerControls.Ascend.performed += _ => LogAction("Aボタン (上昇)");
        controls.PlyerControls.Descend.performed += _ => LogAction("Bボタン (下降)");
        controls.PlyerControls.Boost.performed += _ => LogAction("RB (加速)");
        controls.PlyerControls.Attack.performed += _ => LogAction("RT (攻撃)");
        controls.PlyerControls.LockOn.performed += _ => LogAction("LT (ロックオン)");
        controls.PlyerControls.WeaponSwitch.performed += _ => LogAction("Yボタン (武装切替)");
        controls.PlyerControls.Menu.performed += _ => LogAction("Menuボタン (設定画面)");
    }

    void OnEnable()
    {
        // 3. 入力受付を開始
        controls.Enable();
    }

    void OnDisable()
    {
        // 4. 入力受付を終了 (クリーンアップ)
        controls.Disable();
    }

    // --- コールバックメソッド ---

    private void OnArmorMode(InputAction.CallbackContext context)
    {
        Vector2 direction = context.ReadValue<Vector2>();
        string dirString = "";

        if (direction.y > 0.5f) dirString = "上";
        else if (direction.y < -0.5f) dirString = "下";
        else if (direction.x > 0.5f) dirString = "右";
        else if (direction.x < -0.5f) dirString = "左";

        LogAction($"D-Pad (装甲モード切替) -> {dirString} ({direction})");
    }

    private void OnArmorModeCanceled(InputAction.CallbackContext context)
    {
        // D-Padが離された時の処理 (必要に応じて)
    }

    // --- メインループでのログ表示 ---

    void Update()
    {
        // 移動と視点移動の値がゼロでない場合にのみログに出力
        if (currentMove != Vector2.zero)
        {
            // Debug.Log($"[Move] 移動入力: X={currentMove.x:F2}, Y={currentMove.y:F2}");
        }
        if (currentLook != Vector2.zero)
        {
            // Debug.Log($"[Look] 視点移動入力: X={currentLook.x:F2}, Y={currentLook.y:F2}");
        }
    }

    /// <summary>
    /// アクション実行時にデバッグログを出力
    /// </summary>
    private void LogAction(string actionName)
    {
        Debug.Log($"*** {actionName} が実行されました！ ***");
    }
}