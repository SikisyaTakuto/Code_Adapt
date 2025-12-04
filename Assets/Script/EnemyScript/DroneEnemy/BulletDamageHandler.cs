using System;
using UnityEngine;

/// <summary>
/// ドローンが発射した弾のダメージ処理と自己破壊を行うスクリプト。
/// PlayerControllerとTutorialPlayerControllerの両方に対応します。
/// </summary>
public class BulletDamageHandler : MonoBehaviour
{
    // ===================================
    //  ? 公開設定 (INSPECTOR)
    // ===================================

    [Header("ダメージ設定")]
    [Tooltip("弾が与えるダメージ量。")]
    public int damageAmount = 10;

    [Header("エフェクト設定")]
    [Tooltip("弾がPlayerに衝突した時に再生するエフェクトのPrefab。")]
    public GameObject hitEffectPrefab; // ?? 新規追加: ヒットエフェクトのPrefab

    // 内部コンポーネント参照
    private Rigidbody rb;
    private bool isDestroyed = false; // 弾が既に破壊処理に入ったかを示すフラグ

    private void Start()
    {
        // ?? 未命中時の保険
        // 5秒後に自動的に弾を破壊 (何にも当たらなかった場合)
        Destroy(gameObject, 5f);

        // コンポーネントの取得
        rb = GetComponent<Rigidbody>();
    }


    /// <summary>
    /// トリガーコライダーを持つオブジェクトと衝突したときに呼ばれる。
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        // 1. 衝突対象がPlayerかチェック
        if (other.CompareTag("Player"))
        {
            // Playerに当たった場合の処理
            // エフェクトを生成
            InstantiateHitEffect();

            // ダメージ処理を実行
            ApplyDamage(other.gameObject);
        }
    }

    /// <summary>
    /// ?? 衝突位置にエフェクトを生成する
    /// </summary>
    private void InstantiateHitEffect()
    {
        if (hitEffectPrefab != null)
        {
            // 弾の現在の位置と回転でエフェクトを生成
            GameObject effect = Instantiate(hitEffectPrefab, transform.position, transform.rotation);

            // エフェクトを一定時間後に自動で破壊する処理
            ParticleSystem ps = effect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                // パーティクルシステムの長さに合わせて破壊時間を設定
                Destroy(effect, ps.main.duration + ps.main.startLifetime.constantMax);
            }
            else
            {
                // パーティクルシステムがない場合は、固定の時間（例：2秒）で破壊
                Destroy(effect, 2f);
            }
        }
    }

    /// <summary>
    /// 衝突したオブジェクトにダメージを与える処理。
    /// PlayerControllerまたはTutorialPlayerControllerに対応します。
    /// </summary>
    private void ApplyDamage(GameObject target)
    {
        bool damageApplied = false;

        // 1. PlayerControllerコンポーネントを探す
        PlayerController player = target.GetComponent<PlayerController>();
        if (player != null)
        {
            player.TakeDamage(damageAmount);
            Debug.Log($"PlayerControllerに{damageAmount}ダメージを与えました。", target);
            damageApplied = true;
        }

        // 2. PlayerControllerが見つからなかった場合、TutorialPlayerControllerコンポーネントを探す
        if (!damageApplied)
        {
            TutorialPlayerController tutorialPlayer = target.GetComponent<TutorialPlayerController>();
            if (tutorialPlayer != null)
            {
                tutorialPlayer.TakeDamage(damageAmount);
                Debug.Log($"TutorialPlayerControllerに{damageAmount}ダメージを与えました。", target);
                damageApplied = true;
            }
        }

        // 3. どちらにも適用できなかった場合の警告
        if (!damageApplied)
        {
            Debug.LogWarning("Playerタグのオブジェクトに TakeDamage を持つ PlayerController または TutorialPlayerController が見つかりません。", target);
        }
    }

    public override bool Equals(object obj)
    {
        return obj is BulletDamageHandler handler &&
               base.Equals(obj) &&
               isDestroyed == handler.isDestroyed;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(base.GetHashCode(), isDestroyed);
    }
}