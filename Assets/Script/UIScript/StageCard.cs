using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // マウスイベントを扱うために必要

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
        // 元の並び順に戻す (ただし、最前面になったままの場合は調整が必要になることも)
        // 例えば、他のカードにマウスが当たってない場合のみ元の位置に戻すなど
        // 今回は、マウスが離れたら元のスケールに戻すことに注力
        LeanTween.scale(gameObject, originalScale, ANIMATION_DURATION).setEaseInSine();

        // 元の並び順に戻す処理は、他のカードが手前になったときに自動で後ろに行くため、
        // 必ずしも必要ではないが、視覚的な一貫性を保つためには検討する余地あり
        // transform.SetSiblingIndex(originalSiblingIndex); 
    }

    // カードがクリックされた時
    public void OnPointerClick(PointerEventData eventData)
    {
        // Debug.Log($"{gameObject.name} がクリックされた！");
        // GameManagerを通して選択されたステージ情報を保存し、ArmorSelectSceneへ遷移
        if (!string.IsNullOrEmpty(stageSceneName))
        {
            if (StageSelectManager.Instance != null)
            {
                StageSelectManager.Instance.SelectStage(stageSceneName);
            }
            else
            {
                Debug.LogError("StageSelectManager が見つかりません。");
            }
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} の Stage Scene Name が設定されていません。");
        }
    }
}