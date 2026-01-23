using UnityEngine;
using UnityEditor.Animations;

public class AnimationRecorder : MonoBehaviour
{
    [Header("Settings")]
    public AnimationClip clip;
    public Transform ikTarget;

    [Header("Recording Controls")]
    public KeyCode recordKey = KeyCode.Space;
    public float recordDuration = 15f; // 15, 30, 45, 60など

    [Header("Smoothing")]
    [Range(0.01f, 1f)]
    public float smoothTime = 0.15f;
    public float noiseAmount = 0.003f;

    private GameObjectRecorder m_Recorder;
    private Vector3 _posVelocity = Vector3.zero;

    private bool _isRecording = false;
    private float _startTime;

    void Start()
    {
        Application.targetFrameRate = 60;
        // 最初の初期化
        InitRecorder();
    }

    // レコーダーを初期化するメソッド（Resetの代わり）
    void InitRecorder()
    {
        m_Recorder = new GameObjectRecorder(gameObject);
        m_Recorder.BindComponentsOfType<Transform>(gameObject, true);
    }

    void LateUpdate()
    {
        if (clip == null) return;

        // Spaceキーで記録開始
        if (Input.GetKeyDown(recordKey) && !_isRecording)
        {
            _isRecording = true;
            _startTime = Time.time;
            InitRecorder(); // 記録開始前に作り直して中身をリセット
            Debug.Log($"記録開始: {recordDuration}秒間録画します...");
        }

        if (_isRecording)
        {
            float elapsed = Time.time - _startTime;

            if (ikTarget != null)
            {
                // 自分自身の位置に対してSmoothDampをかけることでガタつきを吸収
                ikTarget.position = Vector3.SmoothDamp(
                    ikTarget.position,
                    ikTarget.position,
                    ref _posVelocity,
                    smoothTime
                );

                float noiseX = (Mathf.PerlinNoise(Time.time * 1.5f, 0) - 0.5f) * noiseAmount;
                float noiseY = (Mathf.PerlinNoise(0, Time.time * 1.5f) - 0.5f) * noiseAmount;
                ikTarget.position += new Vector3(noiseX, noiseY, 0);
            }

            m_Recorder.TakeSnapshot(Time.deltaTime);

            if (elapsed >= recordDuration)
            {
                StopRecording();
            }
        }
    }

    void StopRecording()
    {
        if (!_isRecording) return;

        _isRecording = false;
        m_Recorder.SaveToClip(clip);
        Debug.Log($"録画終了: {clip.name} に保存しました。");
    }

    void OnDisable()
    {
        if (_isRecording)
        {
            StopRecording();
        }
    }
}