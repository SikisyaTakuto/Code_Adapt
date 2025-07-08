using UnityEngine;
using UnityEngine.EventSystems; // UIイベントを扱うために必要

public class ArmorSelectable : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public ArmorData armorData; // このモデルが表すアーマーのデータ
    public ArmorSelectUI armorSelectUI; // ArmorSelectUIへの参照

    private Vector3 originalScale;
    private Color originalColor;
    private Renderer _renderer; // モデルのRenderer

    private const float HOVER_SCALE_FACTOR = 1.1f; // ホバー時の拡大率
    private const float ANIMATION_DURATION = 0.1f; // アニメーション時間

    void Awake()
    {
        originalScale = transform.localScale;
        _renderer = GetComponentInChildren<Renderer>();
        if (_renderer != null)
        {
            originalColor = _renderer.material.color; // モデルのマテリアルの色を保存
        }
    }

    // モデルがクリックされた時
    public void OnPointerClick(PointerEventData eventData)
    {
        if (armorSelectUI != null)
        {
            armorSelectUI.OnArmorClicked(armorData);
        }
    }

    // マウスカーソルがモデルに入った時
    public void OnPointerEnter(PointerEventData eventData)
    {
        LeanTween.scale(gameObject, originalScale * HOVER_SCALE_FACTOR, ANIMATION_DURATION).setEaseOutSine();
        if (_renderer != null)
        {
            // モデルの色を少し明るくするなど、視覚的なフィードバック
            _renderer.material.color = originalColor + new Color(0.1f, 0.1f, 0.1f, 0);
        }
        // 説明パネルを表示
        if (armorSelectUI != null)
        {
            armorSelectUI.ShowDescription(armorData);
        }
    }

    // マウスカーソルがモデルから出た時
    public void OnPointerExit(PointerEventData eventData)
    {
        LeanTween.scale(gameObject, originalScale, ANIMATION_DURATION).setEaseInSine();
        if (_renderer != null)
        {
            // 元の色に戻す
            _renderer.material.color = originalColor;
        }
        // 説明パネルを非表示に (ホバー中に別のアーマーに移動した場合はすぐに非表示に)
        // ただし、クリックして説明を表示した場合は閉じないようにロジックを調整する必要があるかも
        // 今回はシンプルにマウスアウトで消す
        if (armorSelectUI != null)
        {
            armorSelectUI.HideDescription();
        }
    }
}