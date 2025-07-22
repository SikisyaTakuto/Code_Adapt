using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // マウスイベントを扱うために必要
using System.Collections;        // Coroutineのために必要
using UnityEngine.SceneManagement; // SceneManagerを使用するために追加

public class StageCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("ステージ設定")]
    [Tooltip("このカードが表すステージのシーン名")]
    public string stageSceneName;

    [Tooltip("チュートリアルシーンの名前。この名前のステージが選択された場合、アーマー選択をスキップして直接ロードします。")]
    public string tutorialSceneName = "TutorialScene"; // 例: "TutorialScene" をデフォルト値として設定

    [Header("チュートリアル用アーマー自動選択")]
    [Tooltip("チュートリアルシーンに直接遷移する際に自動で選択されるArmorData。3つ設定してください。")]
    public ArmorData[] tutorialArmors; // string[] から ArmorData[] に変更

    private Vector3 originalScale;
    private int originalSiblingIndex; // 元のHierarchyでの並び順
    private Image cardImage; // カードのImageコンポーネント

    private const float HOVER_SCALE_FACTOR = 1.1f; // マウスオーバー時の拡大率
    private const float ANIMATION_DURATION = 0.15f; // アニメーション時間
    [SerializeField, Tooltip("SE再生が完了するまでの待機時間（秒）")]
    private float sePlayDuration = 0.3f; // SEの長さに応じて調整

    private bool isProcessingClick = false; // クリック処理中かどうかのフラグ（多重クリック防止）

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

    void Start()
    {
        // Debug.Log を追加して、ArmorManager.Instance の状態を確認
        if (AudioManager.Instance == null)
        {
            Debug.LogWarning("StageCard: Start時にAudioManager.Instanceがnullです。", this);
        }
        if (ArmorManager.Instance == null)
        {
            Debug.LogWarning("StageCard: Start時にArmorManager.Instanceがnullです。チュートリアルアーマーの自動設定に影響する可能性があります。", this);
        }
    }

    // マウスカーソルがカードに入った時
    public void OnPointerEnter(PointerEventData eventData)
    {
        originalSiblingIndex = transform.GetSiblingIndex();
        transform.SetAsLastSibling();
        LeanTween.scale(gameObject, originalScale * HOVER_SCALE_FACTOR, ANIMATION_DURATION).setEaseOutSine();
    }

    // マウスカーソルがカードから出た時
    public void OnPointerExit(PointerEventData eventData)
    {
        LeanTween.scale(gameObject, originalScale, ANIMATION_DURATION).setEaseInSine();
    }

    // カードがクリックされた時
    public void OnPointerClick(PointerEventData eventData)
    {
        if (isProcessingClick)
        {
            return;
        }

        if (!string.IsNullOrEmpty(stageSceneName))
        {
            isProcessingClick = true;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayButtonClickSE();
                StartCoroutine(LoadSceneAfterSE(stageSceneName));
            }
            else
            {
                Debug.LogError("AudioManager.Instance が見つかりません。SEを再生せずにシーンをロードします。", this);
                HandleSceneLoadImmediately(stageSceneName);
                isProcessingClick = false;
            }
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} の Stage Scene Name が設定されていません。", this);
        }
    }

    private IEnumerator LoadSceneAfterSE(string sceneToLoad)
    {
        yield return new WaitForSeconds(sePlayDuration);
        HandleSceneLoadImmediately(sceneToLoad);
        isProcessingClick = false;
    }

    private void HandleSceneLoadImmediately(string sceneToLoad)
    {
        if (sceneToLoad == tutorialSceneName)
        {
            Debug.Log($"チュートリアルシーン '{sceneToLoad}' を直接ロードします。", this);

            if (ArmorManager.Instance != null)
            {
                ArmorManager.Instance.SetTutorialArmors(tutorialArmors);
            }
            else
            {
                // ★このエラーメッセージが出たら、上記「考えられる原因と修正方法」を再確認してください。
                Debug.LogError("ArmorManager.Instance が見つかりません。チュートリアルアーマーを自動設定できませんでした。", this);
            }

            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            if (StageSelectManager.Instance != null)
            {
                StageSelectManager.Instance.SelectStage(sceneToLoad);
            }
            else
            {
                Debug.LogError("StageSelectManager が見つかりません。シーンロードを直接実行します。", this);
                SceneManager.LoadScene(sceneToLoad);
            }
        }
    }
}