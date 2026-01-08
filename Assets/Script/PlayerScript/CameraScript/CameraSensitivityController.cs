using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UIスライダーの値に応じて、TPSCameraControllerの感度を調整します。
/// このスクリプトは、ゲーム内の任意のSceneでUIがアクティブなGameObjectにアタッチしてください。
/// </summary>
public class CameraSensitivityController : MonoBehaviour
{
    [Tooltip("感度を制御するための UI Slider をアタッチ")]
    public Slider sensitivitySlider;

    [Header("感度の設定範囲")]
    [Tooltip("スライダーの最小値 (0.0f) に対応するマウス感度")]
    public float minMouseSensitivity = 0.5f;
    [Tooltip("スライダーの最大値 (1.0f) に対応するマウス感度")]
    public float maxMouseSensitivity = 10.0f;

    [Tooltip("マウス感度に対するコントローラー感度の基準倍率")]
    public float controllerSensitivityMultiplier = 100f;


    void Start()
    {
        if (sensitivitySlider == null)
        {
            Debug.LogError("Sensitivity Slider is not assigned to the CameraSensitivityController.");
            return;
        }

        // スライダーの初期設定
        sensitivitySlider.minValue = 0f;
        sensitivitySlider.maxValue = 1f;

        // TPSCameraControllerの現在の感度からスライダーの初期値を逆算して設定
        float currentMouseSpeed = TPSCameraController.MouseRotationSpeed;

        // 最初のシーンで TPSCameraController の Start() がまだ呼ばれていない可能性を考慮
        // ここでは、範囲チェックを行い、適切に初期値を設定します
        if (currentMouseSpeed < minMouseSensitivity || currentMouseSpeed > maxMouseSensitivity)
        {
            // 初期値（3.0f）をスライダーの真ん中（0.5f）と仮定して初期化
            sensitivitySlider.value = Mathf.InverseLerp(minMouseSensitivity, maxMouseSensitivity, 3.0f);
        }
        else
        {
            // 現在の感度に基づいてスライダーの値を設定
            sensitivitySlider.value = Mathf.InverseLerp(minMouseSensitivity, maxMouseSensitivity, currentMouseSpeed);
        }

        // スライダーの値が変更されたときのイベントにメソッドを登録
        sensitivitySlider.onValueChanged.AddListener(OnSliderValueChanged);

        // 初回実行でカメラの感度を初期設定に合わせて更新
        OnSliderValueChanged(sensitivitySlider.value);
    }

    /// <summary>
    /// スライダーの値 (0.0 ～ 1.0) が変更されたときに呼び出されます。
    /// </summary>
    private void OnSliderValueChanged(float sliderValue)
    {
        // スライダーの値 (0.0 ～ 1.0) を、設定した感度範囲に線形補間（Lerp）
        float newMouseSensitivity = Mathf.Lerp(minMouseSensitivity, maxMouseSensitivity, sliderValue);

        // コントローラー感度をマウス感度の倍率で決定
        float newControllerSensitivity = newMouseSensitivity * controllerSensitivityMultiplier;

        // TPSCameraControllerの静的メソッドを呼び出して感度を設定
        TPSCameraController.MouseRotationSpeed = newMouseSensitivity;
        TPSCameraController.ControllerRotationSpeed = newControllerSensitivity;
    }
}