// ArmorSelectable.cs
using UnityEngine;
using UnityEngine.EventSystems; // マウスイベントを扱うために必要
using System.Collections;       // Coroutineのために必要

// 必要なイベントインターフェースをすべて実装していることを確認
public class ArmorSelectable : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public ArmorData armorData; // このアーマーモデルに関連付けられたデータ
    public ArmorSelectUI armorSelectUI; // 親のArmorSelectUIへの参照

    private Vector3 originalScale;
    private Color originalColor;
    private Renderer _renderer; // モデルのRenderer (複数ある場合はInChildrenで取得)

    private const float HOVER_SCALE_FACTOR = 1.1f; // マウスオーバー時の拡大率
    private const float CLICK_SCALE_FACTOR = 1.05f; // クリック時の拡大率 (元のスケール基準)
    private const float ANIMATION_DURATION = 0.1f; // アニメーション時間

    private bool _isBeingHeld = false; // マウスが押され続けているか

    void Awake()
    {
        originalScale = transform.localScale;
        _renderer = GetComponentInChildren<Renderer>(); // ★変更点: 子のRendererも考慮
        if (_renderer != null)
        {
            // マテリアルのインスタンスを作成し、オリジナルの色を保持
            _renderer.material = new Material(_renderer.material); // ★変更点: マテリアルのインスタンスを作成
            originalColor = _renderer.material.color;
        }
        else
        {
            Debug.LogWarning($"{gameObject.name}: Rendererが見つかりません。色変更は無効です。", this);
        }

        // コライダーが存在するか確認し、なければ追加
        Collider collider = GetComponent<Collider>();
        if (collider == null)
        {
            collider = gameObject.AddComponent<BoxCollider>(); // ★変更点: BoxColliderを追加
            Debug.LogWarning($"{gameObject.name}: Colliderがありませんでした。BoxColliderを追加しました。", this);
        }
        // Colliderをトリガーに設定して、モデルが物理的に干渉しないようにする
        collider.isTrigger = true;

        // RendererからBound情報を取得してBoxColliderのサイズを自動調整
        if (_renderer != null && collider is BoxCollider boxCollider)
        {
            // RendererのBoundsのローカル座標を計算してColliderに設定
            Vector3 center = _renderer.bounds.center - transform.position;
            Vector3 size = _renderer.bounds.size;
            boxCollider.center = center;
            boxCollider.size = size;
            Debug.Log($"{gameObject.name}: BoxColliderのサイズをRendererに合わせて調整しました。");
        }
    }

    // マウスカーソルがモデルに入った時
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (armorSelectUI != null)
        {
            armorSelectUI.ShowDescription(armorData); // 説明パネルを表示
        }
        // マウスオーバーアニメーション
        LeanTween.scale(gameObject, originalScale * HOVER_SCALE_FACTOR, ANIMATION_DURATION).setEaseOutSine();
    }

    // マウスカーソルがモデルから出た時
    public void OnPointerExit(PointerEventData eventData)
    {
        // スケールを元に戻す
        if (!_isBeingHeld) // クリック中はスケールを維持
        {
            LeanTween.scale(gameObject, originalScale, ANIMATION_DURATION).setEaseInSine();
        }
    }

    // マウスボタンが押された時
    public void OnPointerDown(PointerEventData eventData)
    {
        _isBeingHeld = true;
        // マウスダウン時のアニメーション
        LeanTween.scale(gameObject, originalScale * CLICK_SCALE_FACTOR, ANIMATION_DURATION).setEaseOutSine();

        // 選択されているアーマーのHighlightを更新
        if (armorSelectUI != null)
        {
            armorSelectUI.HighlightTemporary(armorData); // ★変更点: 一時的なハイライトを呼び出す
        }
    }

    // マウスボタンが離された時
    public void OnPointerUp(PointerEventData eventData)
    {
        _isBeingHeld = false;
        // マウスアップ時のアニメーション
        // マウスがまだモデル上にある場合はHOVER_SCALE_FACTORに戻す
        if (EventSystem.current.IsPointerOverGameObject() && eventData.pointerCurrentRaycast.gameObject == gameObject)
        {
            LeanTween.scale(gameObject, originalScale * HOVER_SCALE_FACTOR, ANIMATION_DURATION).setEaseOutSine();
        }
        else
        {
            LeanTween.scale(gameObject, originalScale, ANIMATION_DURATION).setEaseInSine();
        }

        // アーマー選択ロジックを呼び出す
        if (armorSelectUI != null)
        {
            armorSelectUI.OnArmorClicked(armorData); // 選択/解除ロジックを呼び出す
        }
    }

    // モデルの色をハイライト/通常に戻す
    public void SetHighlight(bool isSelected) // ★変更点: メソッド名をisSelectedに変更（選択済み状態を明確化）
    {
        if (_renderer != null)
        {
            _renderer.material.color = isSelected ? Color.yellow : originalColor; // 例: 黄色でハイライト
        }
    }

    // 一時的なハイライト（マウスダウン中のみ）
    public void SetTemporaryHighlight(bool isTemporaryHighlighted)
    {
        if (_renderer != null)
        {
            // 既に選択済みの場合は色を変えない（選択済みが優先）
            if (armorSelectUI != null && armorSelectUI.IsArmorSelected(armorData))
            {
                _renderer.material.color = Color.yellow; // 選択済みは常に黄色
            }
            else
            {
                _renderer.material.color = isTemporaryHighlighted ? Color.cyan : originalColor; // 例: マウスダウン中はシアン
            }
        }
    }
}