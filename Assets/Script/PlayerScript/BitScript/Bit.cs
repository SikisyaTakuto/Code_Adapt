using UnityEngine;
using System.Collections;

public class Bit : MonoBehaviour
{
    private Transform target;
    private Vector3 initialSpawnPosition;
    private float launchHeight;
    private float launchDuration;
    private float attackSpeed;
    private float arcHeight;
    private LayerMask enemyLayer;
    private float damageAmount; // ビットが与えるダメージ量

    private float launchTimer = 0f;
    private bool isLaunching = true;
    private bool hasDealtDamage = false; // ダメージを一度与えたかどうかを追跡

    /// <summary>
    /// ビットを初期化し、ターゲットに向かって発射する準備をする
    /// </summary>
    /// <param name="spawnPos">初期スポーン位置</param>
    /// <param name="targetTransform">ターゲットのTransform</param>
    /// <param name="height">上昇する高さ</param>
    /// <param name="duration">上昇にかかる時間</param>
    /// <param name="speed">ターゲットへの攻撃速度</param>
    /// <param name="arc">上昇軌道のアーチの高さ</param>
    /// <param name="layer">敵のレイヤーマスク</param>
    /// <param name="damage">ビットが与えるダメージ</param>
    public void InitializeBit(Vector3 spawnPos, Transform targetTransform, float height, float duration, float speed, float arc, LayerMask layer, float damage)
    {
        initialSpawnPosition = spawnPos;
        target = targetTransform;
        launchHeight = height;
        launchDuration = duration;
        attackSpeed = speed;
        arcHeight = arc;
        enemyLayer = layer;
        damageAmount = damage; // ダメージ量を設定

        StartCoroutine(LaunchAndAttack());
    }

    IEnumerator LaunchAndAttack()
    {
        // 上昇フェーズ
        while (launchTimer < launchDuration)
        {
            float t = launchTimer / launchDuration;
            Vector3 currentPos = Vector3.Lerp(initialSpawnPosition, initialSpawnPosition + Vector3.up * launchHeight, t);
            // アーチを描くための追加のYオフセット
            currentPos.y += Mathf.Sin(t * Mathf.PI) * arcHeight;
            transform.position = currentPos;
            launchTimer += Time.deltaTime;
            yield return null;
        }

        isLaunching = false;
        // ターゲットに向かって攻撃フェーズ
        while (target != null && target.gameObject.activeInHierarchy && !hasDealtDamage) // ダメージを与えたらループを抜ける
        {
            // ターゲットが有効な場合のみ追跡
            Vector3 direction = (target.position - transform.position).normalized;
            transform.position += direction * attackSpeed * Time.deltaTime;

            // ターゲットの方向を向く
            transform.LookAt(target);

            // ターゲットに十分に近づいたか、または衝突したかをチェック
            // ここでは簡易的に距離で判定
            if (Vector3.Distance(transform.position, target.position) < 1.0f) // 適切な衝突距離を設定
            {
                DealDamageToTarget(); // ターゲットにダメージを与える
                break; // 攻撃完了
            }
            yield return null;
        }

        // ターゲットがnullになったり、倒されたり、ダメージを与え終わったりした場合、ビットを破棄
        Destroy(gameObject);
    }

    /// <summary>
    /// ターゲットにダメージを与える
    /// </summary>
    private void DealDamageToTarget()
    {
        if (target != null && !hasDealtDamage) // ダメージをまだ与えていない場合のみ実行
        {
            EnemyHealth enemyHealth = target.GetComponent<EnemyHealth>();
            if (enemyHealth == null)
            {
                enemyHealth = target.GetComponentInParent<EnemyHealth>();
            }

            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(damageAmount);
                Debug.Log($"Bit hit {target.name} for {damageAmount} damage.");
                hasDealtDamage = true; // ダメージを与えたフラグを立てる
            }
        }
    }

    // 衝突判定 (より正確なダメージ処理のため)
    void OnTriggerEnter(Collider other)
    {
        // 既にダメージを与えているか、上昇中の場合は処理しない
        if (hasDealtDamage || isLaunching) return;

        // 敵レイヤーのオブジェクトに衝突した場合
        if (((1 << other.gameObject.layer) & enemyLayer) != 0)
        {
            // 衝突したのがターゲット自身、またはターゲットの子オブジェクトであるか確認
            // （ビットが複数の敵に当たる可能性を考慮する場合、このロジックは調整が必要）
            // 現状は、InitializeBitで設定された単一のターゲットにのみダメージを与える想定
            if (other.transform == target || other.transform.IsChildOf(target))
            {
                DealDamageToTarget();
                Destroy(gameObject); // ダメージを与えたらビットは消滅
            }
        }
    }
}
