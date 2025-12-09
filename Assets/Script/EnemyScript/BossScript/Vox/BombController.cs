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

    public Transform circleA;     // 動かす円
    public Transform circleB;     // 固定の円）
    public Vector3 startScaleA;   // circleA の開始スケール

    // 内部状態
    private bool touchingGround = false;
    private float touchTimer = 0f;
    private bool hasExploded = false;

    void Start()
    {
        // 最初は非表示にしておく
        if (circleA != null) circleA.gameObject.SetActive(false);
        if (circleB != null) circleB.gameObject.SetActive(false);
    }

    void Update()
    {
        if (hasExploded) return;

        if (touchingGround)
        {
            touchTimer += Time.deltaTime;

            // 爆発までの進行率 0〜1
            float t = Mathf.Clamp01(touchTimer / explodeDelay);

            // circleA を、開始サイズ → circleB のスケールへ近づける
            if (circleA != null && circleB != null)
            {
                circleA.localScale = Vector3.Lerp(startScaleA, circleB.localScale, t);
            }

            if (touchTimer >= explodeDelay)
            {
                Explode();
            }
        }
        else
        {
            touchTimer = 0f;

            // 地面から離れたら元に戻す（任意）
            if (circleA != null)
                circleA.localScale = startScaleA;
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

            // Circle を出現させる
            ShowCircles();
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

            // Circle を出現させる
            ShowCircles();
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

    // 実際にCircleを表示させる関数
    void ShowCircles()
    {
        if (circleA != null)
        {
            circleA.gameObject.SetActive(true);
            circleA.localScale = startScaleA; // 初期サイズ
        }

        if (circleB != null)
        {
            circleB.gameObject.SetActive(true);
            // circleBは固定サイズなのでそのまま
        }
    }

    private void Explode()
    {
        hasExploded = true;

        // 爆発直前に2つのCircleのScaleを揃える
        if (circleA != null && circleB != null)
        {
            // どちらか基準にする。ここでは circleA の scale を基準に。
            Vector3 targetScale = circleA.localScale;
            circleB.localScale = targetScale;
        }

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
