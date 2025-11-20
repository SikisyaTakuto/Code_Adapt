using UnityEngine;
using System;

[RequireComponent(typeof(Rigidbody))]
public class Bullet : MonoBehaviour
{
    private Rigidbody rb;
    private Action<Bullet> returnAction;
    private TrailRenderer trail; // 追加: 軌跡用

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        trail = GetComponent<TrailRenderer>(); // 追加
    }

    [Obsolete]
    public void Initialize(Action<Bullet> onReturn, float speed, float lifetime)
    {
        returnAction = onReturn;

        // 1. 物理挙動の完全リセット（安全策）
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.Sleep(); // 一旦スリープさせることで前の物理演算を完全に断ち切るテクニック
        }

        // 2. トレイル（軌跡）のリセット（もしアタッチされていれば）
        if (trail != null)
        {
            trail.Clear(); // これがないと、プールから出現位置までの「移動線」が見えてしまう
        }

        // 3. 寿命設定
        Invoke(nameof(ReturnToPool), lifetime); // 文字列よりnameof()推奨（ミス防止）
    }

    [Obsolete]
    private void OnTriggerEnter(Collider other)
    {
        // 衝突判定（タグ判定など）
        if (other.CompareTag("Player") || other.CompareTag("Wall"))
        {
            // 必要ならここでダメージ処理
            // IDamageable target = other.GetComponent<IDamageable>();
            // target?.TakeDamage(10);

            ReturnToPool();
        }
    }

    [Obsolete]
    private void ReturnToPool()
    {
        CancelInvoke(nameof(ReturnToPool));

        // 念のためここでも物理リセット
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // パーティクル停止処理
        var ps = GetComponent<ParticleSystem>();
        if (ps != null) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        returnAction?.Invoke(this);
        gameObject.SetActive(false);
    }

    // 安全のため、予期せぬDisable時もInvokeをキャンセルする
    private void OnDisable()
    {
        CancelInvoke(nameof(ReturnToPool));
    }
}