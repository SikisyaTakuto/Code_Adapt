using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // UI操作に必要
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class SceneBasedGameOverManager : MonoBehaviour
{
    [System.Serializable]
    public class SceneMapping
    {
        public string gameSceneName;
        public string gameOverSceneName;
    }

    [Header("シーン マッピング設定")]
    public List<SceneMapping> sceneMappings = new List<SceneMapping>();
    public string defaultGameOverSceneName = "DefaultGameOverScene";

    [Header("遅延設定")]
    public float delaySeconds = 2.0f;

    [Header("UI参照 (HPを0にするために必要)")]
    [Tooltip("HPバーのSliderをドラッグ&ドロップ")]
    public Slider hpSlider;
    [Tooltip("HPの数字テキストをドラッグ&ドロップ")]
    public Text hpText;

    private bool isGameOverStarted = false;

    /// <summary>
    /// PlayerControllerなどでHPが0になった瞬間にこれを呼び出す
    /// </summary>
    public void GoToGameOverScene()
    {
        if (isGameOverStarted) return;
        isGameOverStarted = true;

        // マネージャー側の遅延は使わず、PlayerStatus側の遅延に合わせるなら即座にコルーチンを開始
        StartCoroutine(DelayedGameOverRoutine());
    }

    private void ForceUpdateUIToZero()
    {
        // HPバーを最小値にする
        if (hpSlider != null)
        {
            hpSlider.value = hpSlider.minValue;
        }

        // HPテキストを"0"にする
        if (hpText != null)
        {
            hpText.text = "0";
        }

        Debug.Log("UIのHP表示を強制的に0にしました。");
    }

    private IEnumerator DelayedGameOverRoutine()
    {
        // マネージャー側の待機時間を0にするか、PlayerStatus側のgameOverDelayと調整してください
        yield return new WaitForSeconds(delaySeconds);

        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneMapping foundMapping = sceneMappings.FirstOrDefault(m => m.gameSceneName == currentSceneName);
        string targetGameOverScene = foundMapping != null ? foundMapping.gameOverSceneName : defaultGameOverSceneName;

        if (!string.IsNullOrEmpty(targetGameOverScene))
        {
            SceneManager.LoadScene(targetGameOverScene);
        }
    }
}