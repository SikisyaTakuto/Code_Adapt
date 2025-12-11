using UnityEngine;

/// <summary>
/// プレイヤーを追跡し、目標地点への方向を矢印（子オブジェクト）で示すナビゲーションアシスタント。
/// </summary>
public class NavigationAssistant : MonoBehaviour
{
    [Header("Target & Direction")]
    public Transform playerTransform; // プレイヤーのTransform (Inspectorで設定)
    public Transform targetDestination; // 目的地のTransform (Inspectorで設定)
    public Transform arrowIndicator; // 方向を示す矢印の子オブジェクト (Inspectorで設定)

    [Header("Movement Settings")]
    public float followHeight = 1.8f;      // プレイヤーからの相対的な高さ
    public float followSpeed = 5.0f;       // プレイヤー追従の速さ
    public float rotationSpeed = 10.0f;    // 目的地を向く回転の速さ
    public float hoverAmplitude = 0.1f;    // 上下浮遊の振幅
    public float hoverFrequency = 1.0f;    // 上下浮遊の頻度

    private Vector3 initialLocalPosition;
    private Quaternion initialArrowRotation;

    void Start()
    {
        if (playerTransform == null || targetDestination == null || arrowIndicator == null)
        {
            Debug.LogError("Player, Target Destination, or Arrow Indicator is not assigned. Disabling script.");
            enabled = false;
            return;
        }

        // 浮遊アニメーションのために初期ローカル位置を保存
        initialLocalPosition = transform.localPosition;

        // 矢印の初期回転を保存 (通常、ローカルZ軸が前方を指すように設定されていることを想定)
        initialArrowRotation = arrowIndicator.localRotation;
    }

    void Update()
    {
        HandleFollowAndHover();
        HandleTargetDirection();
    }

    /// <summary>プレイヤーを追従し、浮遊アニメーションを適用します。</summary>
    private void HandleFollowAndHover()
    {
        // 1. ターゲット位置の計算 (プレイヤーの頭上の目標地点)
        Vector3 targetPosition = playerTransform.position + Vector3.up * followHeight;

        // 2. プレイヤーへの追従
        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            Time.deltaTime * followSpeed
        );

        // 3. 浮遊アニメーション (サイン波を使用)
        float hoverOffset = Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitude;
        Vector3 currentPosition = transform.position;
        currentPosition.y += hoverOffset;
        transform.position = currentPosition;
    }

    /// <summary>ターゲットへ向かって矢印を回転させます。</summary>
    private void HandleTargetDirection()
    {
        // プレイヤーとターゲットの水平方向の差分を計算 (Y軸は無視)
        Vector3 directionToTarget = targetDestination.position - transform.position;
        directionToTarget.y = 0;

        if (directionToTarget.magnitude > 0.1f)
        {
            // 1. キャラクター本体の回転 (プレイヤーの移動方向、または常に前方を向くなど、ゲームデザインによる)
            // ここではキャラクター本体は回転させず、矢印のみを回転させる

            // 2. 矢印の回転 (目的地を向かせる)

            // 現在の回転から目的地への回転を計算
            Quaternion lookRotation = Quaternion.LookRotation(directionToTarget);

            // 矢印が設定されていない場合は警告を出し、回転をスキップ
            if (arrowIndicator == null) return;

            // 矢印を目的地に向かってスムーズに回転させる
            arrowIndicator.rotation = Quaternion.Slerp(
                arrowIndicator.rotation,
                lookRotation,
                Time.deltaTime * rotationSpeed
            );

            // 矢印を水平に保つために、回転後にピッチ(X)とロール(Z)をリセット
            Vector3 finalEuler = arrowIndicator.localEulerAngles;
            finalEuler.x = initialArrowRotation.eulerAngles.x;
            finalEuler.z = initialArrowRotation.eulerAngles.z;
            arrowIndicator.localEulerAngles = finalEuler;
        }
        else
        {
            // 目的地に到達した場合、矢印を非表示にするか、初期回転に戻す
            // ここでは初期回転に戻す
            arrowIndicator.localRotation = Quaternion.Slerp(
                arrowIndicator.localRotation,
                initialArrowRotation,
                Time.deltaTime * rotationSpeed
            );
        }
    }
}