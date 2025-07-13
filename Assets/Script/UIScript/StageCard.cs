using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // マウスイベントを扱うために必要
using System.Collections;       // Coroutineのために必要

public class StageCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("ステージ設定")]
    [Tooltip("このカードが表すステージのシーン名")]
    public string stageSceneName;

    private Vector3 originalScale;
    private int originalSiblingIndex; // 元のHierarchyでの並び順
    private Image cardImage; // カードのImageコンポーネント

    private const float HOVER_SCALE_FACTOR = 1.1f; // マウスオーバー時の拡大率
    private const float ANIMATION_DURATION = 0.15f; // アニメーション時間
    [SerializeField, Tooltip("SE再生が完了するまでの待機時間（秒）")]
    private float sePlayDuration = 0.3f; // SEの長さに応じて調整

    private void Awake()
    {
        originalScale = transform.localScale;
        cardImage = GetComponent<Image>();
        if (cardImage == null)
        {
            Debug.LogError("StageCardスクリプトはImageコンポーネントにアタッチしてください。", this);
            enabled = false; // Imageコンポーネントがない場合はスクリプトを無効化
        }
    }

    // マウスカーソルがカードに入った時
    public void OnPointerEnter(PointerEventData eventData)
    {
        // Debug.Log($"{gameObject.name} にマウスが入った！");
        originalSiblingIndex = transform.GetSiblingIndex(); // 現在の並び順を保存
        transform.SetAsLastSibling(); // 最前面に移動（Hierarchyの末尾に移動）
        LeanTween.scale(gameObject, originalScale * HOVER_SCALE_FACTOR, ANIMATION_DURATION).setEaseOutSine();
    }

    // マウスカーソルがカードから出た時
    public void OnPointerExit(PointerEventData eventData)
    {
        // Debug.Log($"{gameObject.name} からマウスが出た！");
        LeanTween.scale(gameObject, originalScale, ANIMATION_DURATION).setEaseInSine();
    }

    // カードがクリックされた時
    public void OnPointerClick(PointerEventData eventData)
    {
        // Debug.Log($"{gameObject.name} がクリックされた！");

        if (!string.IsNullOrEmpty(stageSceneName))
        {
            // まずボタンクリックSEを再生
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayButtonClickSE();
                // SE再生が終わるのを待ってからシーンをロードするコルーチンを開始
                StartCoroutine(LoadSceneAfterSE(stageSceneName));
            }
            else
            {
                Debug.LogError("AudioManager.Instance が見つかりません。SEを再生せずにシーンをロードします。");
                // AudioManagerがない場合でもシーンはロードする
                if (StageSelectManager.Instance != null)
                {
                    StageSelectManager.Instance.SelectStage(stageSceneName);
                }
            }
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} の Stage Scene Name が設定されていません。");
        }
    }

    /// <summary>
    /// SE再生を待ってからシーンをロードするコルーチン
    /// </summary>
    private IEnumerator LoadSceneAfterSE(string sceneToLoad)
    {
        // SEが再生し終わるまで待機
        yield return new WaitForSeconds(sePlayDuration);

        // StageSelectManagerを通して選択されたステージ情報を保存し、次のシーンへ遷移
        if (StageSelectManager.Instance != null)
        {
            StageSelectManager.Instance.SelectStage(sceneToLoad);
        }
        else
        {
            Debug.LogError("StageSelectManager が見つかりません。シーンロードを直接実行します。");
            // 緊急回避として直接シーンロード
            // SceneManager.LoadScene(sceneToLoad); // 必要に応じてコメント解除
        }
    }
}