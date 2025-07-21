using UnityEngine;
using UnityEngine.UI; // Slider と Text を使うため
using System; // Action を使うため

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [Tooltip("プレイヤーの最大HP。")]
    public float maxHealth = 1000f;
    [Tooltip("プレイヤーの現在のHP。")]
    public float currentHealth;

    [Header("UI References")]
    [Tooltip("UI上のHPゲージ（Slider）への参照。")]
    public Slider hpSlider; // HP UI Sliderへの参照
    [Tooltip("UI上のHPテキスト表示（Text）への参照。")]
    public Text hpText; // ★追加: HPテキスト表示（例: 1000/1000）への参照

    public event Action onHealthDamaged; // HPがダメージを受けたときに発火するイベント

    void Awake()
    {
        currentHealth = maxHealth;
        // 初期化時にUIを更新
        UpdateHealthUI();
    }

    /// <summary>
    /// プレイヤーがダメージを受ける処理。
    /// </summary>
    /// <param name="amount">受けるダメージ量。</param>
    public void TakeDamage(float amount)
    {
        if (amount <= 0) return; // ダメージ量が0以下の場合は処理しない

        float previousHealth = currentHealth; // ダメージ前のHPを記録
        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth); // HPを0から最大HPの範囲に制限

        // UIを更新
        UpdateHealthUI();

        // 実際にHPが減った場合にのみイベントを発火
        if (currentHealth < previousHealth)
        {
            onHealthDamaged?.Invoke();
        }

        if (currentHealth <= 0)
        {
            Debug.Log("Player defeated!"); // プレイヤーが倒されたことをログに出力
            // ここでゲームオーバー処理などを呼び出す
            // 例: SceneManager.LoadScene("GameOverScene");
            // または、プレイヤーオブジェクトを非アクティブにするなど
            // gameObject.SetActive(false); 
        }
    }

    /// <summary>
    /// プレイヤーがHPを回復する処理。
    /// </summary>
    /// <param name="amount">回復するHP量。</param>
    public void Heal(float amount)
    {
        if (amount <= 0) return; // 回復量が0以下の場合は処理しない

        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth); // HPを0から最大HPの範囲に制限

        // UIを更新
        UpdateHealthUI();
    }

    /// <summary>
    /// HPバーとHPテキストのUIを更新する。
    /// </summary>
    void UpdateHealthUI()
    {
        // Sliderが割り当てられていれば更新
        if (hpSlider != null)
        {
            hpSlider.maxValue = maxHealth; // スライダーの最大値を設定
            hpSlider.value = currentHealth; // スライダーの現在値を設定
        }

        // Textが割り当てられていれば更新
        if (hpText != null)
        {
            // 現在のHPと最大HPを「現在のHP/最大HP」の形式で表示
            // ":F0" は小数点以下を表示しないフォーマット指定子
            hpText.text = $"{currentHealth:F0}/{maxHealth:F0}";
        }
    }
}
