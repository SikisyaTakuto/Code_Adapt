using UnityEngine;

public class BitCollision : MonoBehaviour
{
    public float damage = 20f;
    private Collider _collider;

    void Awake()
    {
        // 起動時に取得を試みる
        _collider = GetComponent<Collider>();
        
        if (_collider != null)
        {
            _collider.enabled = false;
            // 突き刺し攻撃なので、ConvexなMeshColliderかBoxColliderを想定
            _collider.isTrigger = true; 
        }
    }

    public void SetColliderActive(bool active)
    {
        // ここで再取得を試みる（後からAddComponentされた場合への対策）
        if (_collider == null) _collider = GetComponent<Collider>();

        if (_collider != null)
        {
            _collider.enabled = active;
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} にコライダーが付いていません！");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("プレイヤーにダメージ！");
            // もしプレイヤー側にHPスクリプトがあればここで呼ぶ
            // other.GetComponent<PlayerHealth>()?.TakeDamage(damage);
            
            if (_collider != null) _collider.enabled = false;
        }
    }
}