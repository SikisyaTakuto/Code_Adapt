using UnityEngine;

public class BombController : MonoBehaviour
{
    [Header("Ground detection")]
    [Tooltip("地面オブジェクトのタグ")]
    public string groundTag = "Ground";

    [Header("Explosion")]
    [Tooltip("地面に触れてから何秒で爆発するか")]
    public float explodeDelay = 2f;

    [Tooltip("爆発エフェクト。Hierarchyに出すためInstantiateされます）")]
    public GameObject explosionEffectPrefab;

    [Tooltip("爆発時にこのオブジェクトを非表示・無効化する")]
    public bool disableOnExplode = true;

    // 内部状態
    private bool touchingGround = false;
    private float touchTimer = 0f;
    private bool hasExploded = false;

    void Update()
    {
        if (hasExploded) return;

        if (touchingGround)
        {
            touchTimer += Time.deltaTime;
            if (touchTimer >= explodeDelay)
            {
                Explode();
            }
        }
        else
        {
            // 地面から離れている場合はタイマーをリセット
            touchTimer = 0f;
        }
    }

    // 地面に触れたら開始
    void OnCollisionEnter(Collision collision)
    {
        if (hasExploded) return;

        if (collision.gameObject.CompareTag(groundTag))
        {
            touchingGround = true;
            // 必要なら最初の接触時に何かする
        }
    }

    // 地面から離れたら停止
    void OnCollisionExit(Collision collision)
    {
        if (hasExploded) return;

        if (collision.gameObject.CompareTag(groundTag))
        {
            touchingGround = false;
        }
    }

    // もし地面がTriggerで設定されている場合のサポート
    void OnTriggerEnter(Collider other)
    {
        if (hasExploded) return;

        if (other.gameObject.CompareTag(groundTag))
        {
            touchingGround = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (hasExploded) return;

        if (other.gameObject.CompareTag(groundTag))
        {
            touchingGround = false;
        }
    }

    private void Explode()
    {
        hasExploded = true;

        // エフェクトを再生
        if (explosionEffectPrefab != null)
        {
            GameObject fx = Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity, null);

            // 再生時間分だけ残して消す
            ParticleSystem ps = fx.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                float duration = main.duration + main.startLifetime.constantMax;
                Destroy(fx, duration);
            }
            else
            {
                // ParticleSystemがない場合は一律で 5 秒後に削除
                Destroy(fx, 5f);
            }
        }

        // 爆弾本体を無効化
        if (disableOnExplode)
        {
            // 表示を消す
            foreach (var rend in GetComponentsInChildren<Renderer>())
                rend.enabled = false;

            // 衝突を止める
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;
        }

        // 本体を少ししてから破棄
        Destroy(gameObject, 0.1f);
    }
}
