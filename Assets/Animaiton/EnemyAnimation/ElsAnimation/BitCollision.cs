using UnityEngine;

public class BitCollision : MonoBehaviour
{
    private Collider _collider;
    private float _damage;
    private bool _hasHitInThisAttack = false;

    void Awake()
    {
        _collider = GetComponent<Collider>();
        // 物理的な押し出しを防ぎつつ接触を検知するため、トリガーに設定
        if (_collider != null) _collider.isTrigger = true;
    }

    public void Setup(float damage)
    {
        _damage = damage;
    }

    public void SetColliderActive(bool active)
    {
        if (_collider != null) _collider.enabled = active;
        if (active) _hasHitInThisAttack = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_hasHitInThisAttack) return;

        if (other.CompareTag("Player"))
        {
            ApplyDamageToStatus(other.gameObject);
        }
    }

    /// <summary>
    /// ターゲットから PlayerStatus を探し、ダメージを適用します
    /// </summary>
    private void ApplyDamageToStatus(GameObject target)
    {
        // PlayerStatusを親、あるいは子から検索
        PlayerStatus status = target.GetComponentInParent<PlayerStatus>() ?? target.GetComponentInChildren<PlayerStatus>();

        if (status != null)
        {
            float defenseMultiplier = 1.0f; // デフォルトは等倍

            // 現在装備しているアーマー（コントローラー）から防御倍率を取得する試み
            var balance = target.GetComponentInParent<BlanceController>() ?? target.GetComponentInChildren<BlanceController>();
            var buster = target.GetComponentInParent<BusterController>() ?? target.GetComponentInChildren<BusterController>();
            var speedCtrl = target.GetComponentInParent<SpeedController>() ?? target.GetComponentInChildren<SpeedController>();

            // ※各コントローラーに defenseRate などの変数がある場合、ここで倍率を上書きできます
            // 例: if (balance != null) defenseMultiplier = 0.8f; // 20%軽減など

            // PlayerStatus の TakeDamage を実行
            status.TakeDamage(_damage, defenseMultiplier);

            _hasHitInThisAttack = true;
            Debug.Log($"[BitCollision] {target.name} に {_damage} (倍率:{defenseMultiplier}) ダメージを適用。");
        }
        else
        {
            Debug.LogWarning($"{target.name} に Playerタグがありますが、PlayerStatusが見つかりません。");
        }
    }
}