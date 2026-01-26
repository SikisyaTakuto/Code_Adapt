using UnityEngine;
using UnityEngine.UI;

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

    public SceneBasedGameOverManager gameOverManager;

    // 重力パネルに触れているかどうかを保持するフラグ
    [HideInInspector] // インスペクターで隠したい場合
    public bool isMovementSlowed = false;

    void Awake()
    {
        _currentHP = maxHP;
        _currentEnergy = maxEnergy;
    }

    void Update()
    {
        if (_isDead) return;

        // 自然回復
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
        if (_currentHP <= 0) Die();
    }

    private void Die()
    {
        _isDead = true;
        if (gameOverManager != null) gameOverManager.GoToGameOverScene();
    }

    private void UpdateUI()
    {
        if (hpSlider) hpSlider.value = _currentHP / maxHP;
        if (hpText) hpText.text = $"{Mathf.CeilToInt(_currentHP)} / {maxHP}";
        if (energySlider) energySlider.value = _currentEnergy / maxEnergy;
    }
}