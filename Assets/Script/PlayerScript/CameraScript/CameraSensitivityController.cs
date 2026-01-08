using UnityEngine;
using UnityEngine.UI;

public class CameraSensitivityController : MonoBehaviour
{
    [Tooltip("感度を制御するための UI Slider をアタッチ")]
    public Slider sensitivitySlider;

    [Header("感度の設定範囲")]
    [Tooltip("スライダーの最小値 (0.0f) に対応するマウス感度")]
    public float minMouseSensitivity = 1.0f; // 他のPCで動かない対策として少し底上げ
    [Tooltip("スライダーの最大値 (1.0f) に対応するマウス感度")]
    public float maxMouseSensitivity = 10.0f; // 他のPCで動かない対策として最大値をアップ

    [Tooltip("マウス感度に対するコントローラー感度の基準倍率")]
    public float controllerSensitivityMultiplier = 4.0f; // 倍率を現実的な数値に調整

    void Start()
    {
        if (sensitivitySlider == null)
        {
            Debug.LogError("Sensitivity Slider is not assigned.");
            return;
        }

        // 1. スライダーの基本設定
        sensitivitySlider.minValue = 0f;
        sensitivitySlider.maxValue = 1f;

        // 2. スライダーを中央 (0.5) に設定
        sensitivitySlider.value = 0.5f;

        // 3. スライダーの値が変更されたときのイベント登録
        sensitivitySlider.onValueChanged.AddListener(OnSliderValueChanged);

        // 4. 初回実行：スライダーの中央値（0.5f）に基づいてカメラの感度を初期化
        OnSliderValueChanged(sensitivitySlider.value);
    }

    private void OnSliderValueChanged(float sliderValue)
    {
        // スライダーの値 (0.0 ～ 1.0) を感度範囲に変換
        float newMouseSensitivity = Mathf.Lerp(minMouseSensitivity, maxMouseSensitivity, sliderValue);
        float newControllerSensitivity = newMouseSensitivity * controllerSensitivityMultiplier;

        // TPSCameraController の静的プロパティに直接代入
        TPSCameraController.MouseRotationSpeed = newMouseSensitivity;
        TPSCameraController.ControllerRotationSpeed = newControllerSensitivity;
    }
}