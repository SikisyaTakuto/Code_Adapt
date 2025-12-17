using UnityEngine;

/// <summary>
/// 敵が発射するビームの視覚効果と、プレイヤーへの衝突判定を制御します。
/// </summary>
public class EnemyBeamController : MonoBehaviour
{
    [Header("Visual Settings")]
    [Tooltip("ビームの持続時間")]
    public float lifetime = 0.5f;

    [Tooltip("ビーム本体のエフェクトまたはモデル（これをZ軸方向にスケールする）")]
    public Transform beamVisual;

    [Header("Damage Settings")]
    [Tooltip("プレイヤーに与えるダメージ量")]
    public int damage = 20;

    void Awake()
    {
        if (beamVisual == null)
        {
            Debug.LogError("EnemyBeamController: Beam Visual が設定されていません。");
        }
    }

    /// <summary>
    /// ビームを生成し、ヒット判定がある場合はプレイヤーにダメージを与えます。
    /// ScorpionEnemyなどの「発射側」から呼び出されます。
    /// </summary>
    public void Fire(Vector3 startPoint, Vector3 endPoint, bool didHit, GameObject hitObject = null)
    {
        // 1. ビームの長さを計算して見た目を反映
        if (beamVisual != null)
        {
            float distance = Vector3.Distance(startPoint, endPoint);
            Vector3 localScale = beamVisual.localScale;
            localScale.z = distance;
            beamVisual.localScale = localScale;
        }

        // 2. 何かに当たっており、かつ対象が指定されている場合
        if (didHit && hitObject != null)
        {
            ApplyDamage(hitObject);
        }

        // 3. 持続時間後に自身を破棄
        Destroy(gameObject, lifetime);
    }

    /// <summary>
    /// ヒットしたオブジェクトからプレイヤーのコントローラーを探してダメージを適用します。
    /// </summary>
    // EnemyBeamController.cs の ApplyDamage を修正
    private void ApplyDamage(GameObject target)
    {
        // ヒットしたオブジェクト自体、またはその親が "Player" タグを持っているか確認
        if (target.CompareTag("Player") || (target.transform.parent != null && target.transform.parent.CompareTag("Player")))
        {
            bool damageApplied = false;

            // 3つのコントローラーを「自分・親・子」すべてから探す
            var blance = target.GetComponentInParent<BlanceController>() ?? target.GetComponentInChildren<BlanceController>();
            if (blance != null) { blance.TakeDamage(damage); damageApplied = true; }

            if (!damageApplied)
            {
                var buster = target.GetComponentInParent<BusterController>() ?? target.GetComponentInChildren<BusterController>();
                if (buster != null) { buster.TakeDamage(damage); damageApplied = true; }
            }

            if (!damageApplied)
            {
                var speed = target.GetComponentInParent<SpeedController>() ?? target.GetComponentInChildren<SpeedController>();
                if (speed != null) { speed.TakeDamage(damage); damageApplied = true; }
            }
        }
    }
}