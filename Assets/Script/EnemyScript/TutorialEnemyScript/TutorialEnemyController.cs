using UnityEngine;
using System; // Actionを使うために必要

/// <summary>
/// チュートリアル用の訓練用敵（サンドバック）のコントローラー。
/// 破壊された際にイベントを発生させ、TutorialManagerに通知する。
/// </summary>
public class TutorialEnemyController : MonoBehaviour
{
    // ?? 外部（TutorialManager）に破壊を通知するためのイベント
    public event Action onDeath;

    [Header("設定")]
    [Tooltip("敵の初期体力")]
    public float maxHealth = 100f;
    private float currentHealth;

    void Start()
    {
        currentHealth = maxHealth;
        // サンドバックなので、通常はプレイヤーからの攻撃以外では死なない
    }

    /// <summary>
    /// 外部からダメージを受けるメソッド。
    /// </summary>
    /// <param name="damageAmount">ダメージ量</param>
    public void TakeDamage(float damageAmount)
    {
        currentHealth -= damageAmount;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// 敵が破壊された時の処理。イベントを発火させる。
    /// </summary>
    private void Die()
    {
        Debug.Log("TutorialEnemyController: 撃破されました。Managerに通知します。");

        // イベントが登録されていれば発火（ManagerのOnEnemyDestroyed()が呼ばれる）
        onDeath?.Invoke();

        // オブジェクトを破壊
        Destroy(gameObject);
    }

    // ?? 補足: プレイヤーの攻撃スクリプトがこのメソッドを呼ぶ必要があります。
    // 例: playerAttackScript.cs の中で、衝突したオブジェクトに対して
    // if (hit.transform.GetComponent<TutorialEnemyController>() is TutorialEnemyController enemy)
    // {
    //     enemy.TakeDamage(attackPower);
    // }
}