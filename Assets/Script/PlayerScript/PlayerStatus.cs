using UnityEngine;
using UnityEngine.UI;
using System.Collections; // コルーチンを使うために必要

public class PlayerStatus : MonoBehaviour
{
    [Header("HP Settings")]
    public float maxHP = 5000.0f;
    private float _currentHP;
    public float CurrentHP => _currentHP;

    [Header("Energy Settings")]
    public float maxEnergy = 200.0f;
    public float energyRecoveryRate = 20.0f;
    public float recoveryDelay = 1.0f;
    private float _currentEnergy;
    public float currentEnergy => _currentEnergy;
    private float _lastEnergyConsumptionTime;

    [Header("UI References")]
    public Slider hpSlider;
    public Text hpText;
    public Slider energySlider;

    [Header("Global State")]
    private bool _isDead = false;
    public bool IsDead => _isDead;

    [Header("Death Settings")]
    [Tooltip("死んでからゲームオーバー画面に行くまでの待ち時間")]
    public float gameOverDelay = 2.0f;
    public float fadeDuration = 2.0f; // 何秒かけて暗くするか
    public FadeManager fadeManager;   // 上で作ったFadeCanvasをドラッグ&ドロップ

    public SceneBasedGameOverManager gameOverManager;

    [HideInInspector]
    public bool isMovementSlowed = false;

    void Awake()
    {
        _currentHP = maxHP;
        _currentEnergy = maxEnergy;
    }

    void Update()
    {
        if (_isDead) return;

        if (Time.time >= _lastEnergyConsumptionTime + recoveryDelay)
        {
            _currentEnergy = Mathf.MoveTowards(_currentEnergy, maxEnergy, energyRecoveryRate * Time.deltaTime);
        }
        UpdateUI();
    }

    public bool ConsumeEnergy(float amount)
    {
        if (_currentEnergy >= amount)
        {
            _currentEnergy -= amount;
            _lastEnergyConsumptionTime = Time.time;
            return true;
        }
        return false;
    }

    public void TakeDamage(float rawDamage, float defenseMultiplier)
    {
        if (_isDead) return;
        float finalDamage = rawDamage * defenseMultiplier;
        _currentHP = Mathf.Max(0, _currentHP - finalDamage);

        // ダメージを受けた直後にUIを更新（ラグ防止）
        UpdateUI();

        if (_currentHP <= 0) Die();
    }

    private void Die()
    {
        if (_isDead) return;
        _isDead = true;

        UpdateUI(); // HPを0にする

        Debug.Log("Player Died. Start FadeOut and Animation.");

        // フェードとゲームオーバー遷移のコルーチンを開始
        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        // 1. 死亡アニメーションを少し見せる（1秒間など）
        yield return new WaitForSeconds(1.0f);

        // 2. フェードアウト開始
        if (fadeManager != null)
        {
            yield return StartCoroutine(fadeManager.FadeOut(fadeDuration));
        }

        // 3. 残りの時間を待機してからシーン遷移
        float remainingWait = gameOverDelay - fadeDuration - 1.0f;
        if (remainingWait > 0) yield return new WaitForSeconds(remainingWait);

        if (gameOverManager != null)
        {
            gameOverManager.GoToGameOverScene();
        }
    }

    private void UpdateUI()
    {
        if (hpSlider) hpSlider.value = _currentHP / maxHP;
        if (hpText) hpText.text = $"{Mathf.CeilToInt(_currentHP)} / {maxHP}";
        if (energySlider) energySlider.value = _currentEnergy / maxEnergy;
    }
}