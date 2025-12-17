using UnityEngine;

public class BombController : MonoBehaviour
{
    [Header("Ground detection")]
    public string groundTag = "Ground";

    [Header("Explosion Settings")]
    [Tooltip("地面に触れてから何秒で爆発するか")]
    public float explodeDelay = 2f;
    [Tooltip("爆発のダメージ範囲（半径）")]
    public float explosionRadius = 5f;
    [Tooltip("プレイヤーに与えるダメージ量")]
    public float explosionDamage = 500f;

    [Header("Visual Effects")]
    public GameObject explosionEffectPrefab;
    public bool disableOnExplode = true;

    public Transform circleA;
    public Transform circleB;
    public Vector3 startScaleA;

    // 内部状態
    private bool touchingGround = false;
    private float touchTimer = 0f;
    private bool hasExploded = false;

    void Start()
    {
        if (circleA != null) circleA.gameObject.SetActive(false);
        if (circleB != null) circleB.gameObject.SetActive(false);
    }

    void Update()
    {
        if (hasExploded) return;

        if (touchingGround)
        {
            touchTimer += Time.deltaTime;
            float t = Mathf.Clamp01(touchTimer / explodeDelay);

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
            if (circleA != null) circleA.localScale = startScaleA;
        }
    }

    // --- 接触判定 ---
    void OnCollisionEnter(Collision collision)
    {
        if (!hasExploded && collision.gameObject.CompareTag(groundTag))
        {
            touchingGround = true;
            ShowCircles();
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (!hasExploded && collision.gameObject.CompareTag(groundTag))
            touchingGround = false;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!hasExploded && other.gameObject.CompareTag(groundTag))
        {
            touchingGround = true;
            ShowCircles();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!hasExploded && other.gameObject.CompareTag(groundTag))
            touchingGround = false;
    }

    void ShowCircles()
    {
        if (circleA != null) { circleA.gameObject.SetActive(true); circleA.localScale = startScaleA; }
        if (circleB != null) circleB.gameObject.SetActive(true);
    }

    private void Explode()
    {
        hasExploded = true;

        // 1. 範囲内のプレイヤーにダメージを与える
        ApplyExplosionDamage();

        // --- 演出処理 ---
        if (circleA != null && circleB != null) circleB.localScale = circleA.localScale;

        if (explosionEffectPrefab != null)
        {
            GameObject fx = Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
            ParticleSystem ps = fx.GetComponent<ParticleSystem>();
            float destTime = (ps != null) ? ps.main.duration + ps.main.startLifetime.constantMax : 5f;
            Destroy(fx, destTime);
        }

        if (disableOnExplode)
        {
            foreach (var rend in GetComponentsInChildren<Renderer>()) rend.enabled = false;
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;
        }

        Destroy(gameObject, 0.1f);
    }

    /// <summary>
    /// 爆発範囲内のPlayerタグを持つオブジェクトにダメージを与えます。
    /// </summary>
    private void ApplyExplosionDamage()
    {
        // 周辺のコライダーを取得
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius);

        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Player"))
            {
                // PlayerStatus を直接探す（親・子・自身）
                PlayerStatus status = hitCollider.GetComponentInParent<PlayerStatus>() ?? hitCollider.GetComponentInChildren<PlayerStatus>();

                if (status != null)
                {
                    // 防御倍率はひとまず 1.0 (等倍) で送信
                    // 必要に応じてアーマーから倍率を取得する処理を追加可能
                    status.TakeDamage(explosionDamage, 1.0f);

                    Debug.Log($"[Bomb] プレイヤーを爆発に巻き込みました: {explosionDamage}ダメージ");
                    break; // 1つの爆弾でダメージは1回のみ
                }
            }
        }
    }

    // エディタ上で爆発範囲を確認できるようにする
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}