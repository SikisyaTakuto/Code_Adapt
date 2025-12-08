using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class EnemyButtonsSimple : MonoBehaviour
{
    [Header("1. Self Panel")]
    [Tooltip("このボタンに対応するエネミーパネル (開くパネル) を設定してください。")]
    public GameObject selfPanel;

    [Header("2. Sibling Panels")]
    [Tooltip("他の5つのエネミーパネル (兄弟パネル) を全て設定してください。")]
    public List<GameObject> siblingPanels = new List<GameObject>();

    private Button button;

    void Awake()
    {
        // Awakeではコンポーネントの有無チェックと取得のみを行い、
        // 外部からの参照エラーを防ぐ
        button = GetComponent<Button>();
    }

    void Start()
    {
        // Buttonコンポーネントが見つからない場合はエラーを出し、処理を中断
        if (button == null)
        {
            // ここでエラーが出た場合、このスクリプトがButtonコンポーネントのないGameObjectに
            // アタッチされていることを意味します。
            Debug.LogError(gameObject.name + ": Buttonコンポーネントが見つかりません。このスクリプトはButtonにアタッチしてください。", this);
            return;
        }

        // ButtonのOnClickイベントに、HandleClickメソッドを登録
        // Startで登録することで、Awake時の参照問題を回避しやすくなります。
        button.onClick.AddListener(HandleClick);
    }

    /// <summary>
    /// ボタンがクリックされたときに呼び出されます。
    /// OnClickイベントから呼び出せるよう、publicに宣言します。
    /// </summary>
    public void HandleClick() // ★ public に修正済み ★
    {
        // 1. 兄弟パネル（他のエネミーパネル）を全て閉じる
        if (siblingPanels != null)
        {
            foreach (var panel in siblingPanels)
            {
                if (panel != null)
                {
                    panel.SetActive(false);
                }
            }
        }

        // 2. 自分のパネルを開く
        if (selfPanel != null)
        {
            selfPanel.SetActive(true);
            // オプション: 開いたパネルをCanvasの最前面に移動 (描画順序の保証)
            selfPanel.transform.SetAsLastSibling();
            Debug.Log(selfPanel.name + " を開きました。");
        }
        else
        {
            Debug.LogError("Self Panelが設定されていません。", this);
        }

        // 外部のSE再生ロジックがある場合はここに追加
    }

    void OnDestroy()
    {
        // スクリプトが破棄されるときにリスナーを解除 (メモリリーク防止)
        if (button != null)
        {
            button.onClick.RemoveListener(HandleClick);
        }
    }
}