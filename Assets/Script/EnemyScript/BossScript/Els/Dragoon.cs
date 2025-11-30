using UnityEngine;

public class Dragoon : MonoBehaviour
{
    public float speed = 10f; // ターゲット位置への移動速度
    public float attackRange = 5f; // 攻撃開始距離
    public float fireRate = 1.0f; // 発射間隔
    public GameObject dragoonBulletPrefab; // ドラグーンの弾Prefab
    public float lifetime = 15f; // 展開時間（自動回収されるまでの時間）

    private Transform target;
    private Vector3 initialTargetOffset; // ターゲット周囲の展開位置オフセット
    private float nextFireTime;

    // === 初期設定 ===
    public void Initialize(Transform targetTransform, int index)
    {
        target = targetTransform;
        nextFireTime = Time.time + 1f; // 少し遅延させて攻撃開始

        // ターゲットの周囲を囲む展開位置を計算
        // indexを使って8基を円形に配置するオフセットを計算
        float angle = index * (360f / 8f);
        Vector3 offset = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0f, Mathf.Sin(angle * Mathf.Deg2Rad));
        // Y軸方向にも少し変化をつける
        offset.y = (index % 2 == 0) ? 1.5f : 0.5f;

        initialTargetOffset = offset.normalized * attackRange;

        Destroy(gameObject, lifetime); // 寿命を設定
    }

    void Update()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        // 1. 展開位置への移動
        Vector3 targetPosition = target.position + initialTargetOffset;
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);

        // ターゲットに顔を向ける
        transform.LookAt(target);

        // 2. 攻撃
        if (Vector3.Distance(transform.position, targetPosition) < 0.1f && Time.time >= nextFireTime)
        {
            FireDragoonBeam();
            nextFireTime = Time.time + fireRate;
        }
    }

    // === ドラグーンビーム発射 ===
    void FireDragoonBeam()
    {
        // ターゲットに向かってビームを発射
        if (dragoonBulletPrefab != null)
        {
            // ターゲット方向に回転させてから発射（LookAtで既に回転している）
            Instantiate(dragoonBulletPrefab, transform.position, transform.rotation);
        }
    }
}