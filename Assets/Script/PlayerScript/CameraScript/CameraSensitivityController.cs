using UnityEngine;
using UnityEngine.UI;

public class CameraSensitivityController : MonoBehaviour
{
    [Tooltip("感度を制御するための UI Slider をアタッチ")]
    public Slider sensitivitySlider;

    [Header("基準となる感度 (スライダー 0.5 の時の値)")]
    [Tooltip("マウス：この値をベースにスライダーで倍率をかけます")]
    public float baseMouseSensitivity = 0.5f;
    [Tooltip("コントローラー：この値をベースにスライダーで倍率をかけます")]
    public float baseControllerSensitivity = 2.0f;

    [Header("調整幅 (倍率)")]
    [Range(0.1f, 1.0f)]
    [Tooltip("スライダーを最小にした時に基準の何倍にするか (0.2なら0.2倍)")]
    public float minMultiplier = 0.2f;
    [Range(1.0f, 10.0f)]
    [Tooltip("スライダーを最大にした時に基準の何倍にするか (5.0なら5倍)")]
    public float maxMultiplier = 5.0f;

    void Start()
    {
        if (sensitivitySlider == null)
        {
            Debug.LogError("Sensitivity Slider is not assigned.");
            return;
        }

        // スライダーの範囲を 0?1 に固定
        sensitivitySlider.minValue = 0f;
        sensitivitySlider.maxValue = 1f;

        // 初期値を中央に設定
        sensitivitySlider.value = 0.5f;

        sensitivitySlider.onValueChanged.AddListener(OnSliderValueChanged);

        // 初回実行
        OnSliderValueChanged(sensitivitySlider.value);
    }

    private void OnSliderValueChanged(float sliderValue)
    {
        float multiplier;

        // スライダー 0.5 を境界に、下半分と上半分で倍率を計算
        if (sliderValue <= 0.5f)
        {
            // 0.0(minMultiplier倍) ～ 0.5(1.0倍)
            float t = sliderValue / 0.5f;
            multiplier = Mathf.Lerp(minMultiplier, 1.0f, t);
        }
        else
        {
            // 0.5(1.0倍) ～ 1.0(maxMultiplier倍)
            float t = (sliderValue - 0.5f) / 0.5f;
            multiplier = Mathf.Lerp(1.0f, maxMultiplier, t);
        }

        // 基準値に倍率を適用
        float newMouse = baseMouseSensitivity * multiplier;
        float newController = baseControllerSensitivity * multiplier;

        // TPSCameraController への適用
        TPSCameraController.MouseRotationSpeed = newMouse;
        TPSCameraController.ControllerRotationSpeed = newController;

        // デバッグ用（調整が終わったら消してOK）
        // Debug.Log($"Multiplier: {multiplier:F2} | Mouse: {newMouse:F2} | Controller: {newController:F2}");
    }
}