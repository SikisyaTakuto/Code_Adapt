using UnityEngine;

// エディター時のみ UnityEditor 関連を読み込む
#if UNITY_EDITOR
using UnityEditor.Animations;
#endif

public class AnimationRecorder : MonoBehaviour
{
    [Header("Settings")]
    public AnimationClip clip;
    public Transform ikTarget;

    [Header("Recording Controls")]
    public KeyCode recordKey = KeyCode.Space;
    public float recordDuration = 15f;

    [Header("Smoothing")]
    [Range(0.01f, 1f)]
    public float smoothTime = 0.15f;
    public float noiseAmount = 0.003f;

#if UNITY_EDITOR
    private GameObjectRecorder m_Recorder;
#endif

    private Vector3 _posVelocity = Vector3.zero;
    private bool _isRecording = false;
    private float _startTime;

    void Start()
    {
        Application.targetFrameRate = 60;
#if UNITY_EDITOR
        InitRecorder();
#endif
    }

    void InitRecorder()
    {
#if UNITY_EDITOR
        m_Recorder = new GameObjectRecorder(gameObject);
        m_Recorder.BindComponentsOfType<Transform>(gameObject, true);
#endif
    }

    void LateUpdate()
    {
        if (clip == null) return;

        if (Input.GetKeyDown(recordKey) && !_isRecording)
        {
            _isRecording = true;
            _startTime = Time.time;
            InitRecorder();
            Debug.Log($"記録開始: {recordDuration}秒間録画します...");
        }

        if (_isRecording)
        {
            float elapsed = Time.time - _startTime;

            if (ikTarget != null)
            {
                ikTarget.position = Vector3.SmoothDamp(
                    ikTarget.position,
                    ikTarget.position, // 本来は目標地点を入れるべきですが元のコードを維持
                    ref _posVelocity,
                    smoothTime
                );

                float noiseX = (Mathf.PerlinNoise(Time.time * 1.5f, 0) - 0.5f) * noiseAmount;
                float noiseY = (Mathf.PerlinNoise(0, Time.time * 1.5f) - 0.5f) * noiseAmount;
                ikTarget.position += new Vector3(noiseX, noiseY, 0);
            }

#if UNITY_EDITOR
            m_Recorder.TakeSnapshot(Time.deltaTime);
#endif

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

#if UNITY_EDITOR
        m_Recorder.SaveToClip(clip);
#endif
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