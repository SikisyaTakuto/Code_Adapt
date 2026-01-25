using UnityEngine;

public class BitCollision : MonoBehaviour
{
    private float _damage;
    private Collider _collider;

    public void Setup(float damage)
    {
        _damage = damage;
        _collider = GetComponent<Collider>();
        if (_collider != null) _collider.isTrigger = true; // トリガーとして設定
    }

    public void SetColliderActive(bool active)
    {
        if (_collider != null) _collider.enabled = active;
    }

    // プレイヤーが触れた瞬間に呼ばれる
    private void OnTriggerEnter(Collider other)
    {
        // 当たった相手がPlayerタグを持っているか、PlayerStatusを持っているか確認
        if (other.CompareTag("Player"))
        {
            // --- ここにデバッグログを追加 ---
            Debug.Log($"<color=red>[Hit]</color> {gameObject.name} が Player に当たりました！ ダメージ: {_damage}");

            // ダメージ処理の呼び出し（既存のシステムに合わせて調整してください）
            PlayerStatus status = other.GetComponentInParent<PlayerStatus>();
            if (status != null)
            {
                status.TakeDamage(_damage, 1.0f);
            }
        }
    }
}