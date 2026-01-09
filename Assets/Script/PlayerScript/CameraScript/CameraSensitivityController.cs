using UnityEngine;
using UnityEngine.UI;

public class CameraSensitivityController : MonoBehaviour
{
    [Tooltip("感度を制御するための UI Slider をアタッチ")]
    public Slider sensitivitySlider;

    [Header("感度の設定範囲 (実際の数値)")]
    public float minMouseSensitivity = 0.1f;
    public float maxMouseSensitivity = 5.0f;

    [Tooltip("マウス感度に対するコントローラー感度の基準倍率")]
    public float controllerSensitivityMultiplier = 4.0f;

    void Start()
    {
        if (sensitivitySlider == null) return;

        // 1. スライダーの範囲設定
        sensitivitySlider.minValue = minMouseSensitivity;
        sensitivitySlider.maxValue = maxMouseSensitivity;

        // 2. 現在の TPSCameraController の値を取得してスライダーに反映
        // (これにより、スライダーの位置が現在の値と一致します)
        float currentSens = TPSCameraController.MouseRotationSpeed;

        // もし初期値が設定されていない(-1等)の場合は、中間値をセット
        if (currentSens <= 0)
        {
            currentSens = (minMouseSensitivity + maxMouseSensitivity) * 0.5f;
        }

        sensitivitySlider.value = currentSens;

        // 3. 値変更イベントの登録
        sensitivitySlider.onValueChanged.AddListener(OnSliderValueChanged);

        // 初回実行
        OnSliderValueChanged(sensitivitySlider.value);
    }

    private void OnSliderValueChanged(float value)
    {
        // スライダーの値(value)そのものが感度値になる
        float newMouse = value;
        float newController = value * controllerSensitivityMultiplier;

        // TPSCameraController への適用
        TPSCameraController.MouseRotationSpeed = newMouse;
        TPSCameraController.ControllerRotationSpeed = newController;

        // Debug.Log($"Current Sensitivity: {newMouse}");
    }
}