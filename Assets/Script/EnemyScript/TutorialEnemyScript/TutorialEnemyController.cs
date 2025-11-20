using UnityEngine;
using System;

public class TutorialEnemyController : MonoBehaviour
{
    // ?? ダメージと死亡判定に必要な基本設定
    public float maxHP = 100f;

    private float _currentHP;
    private bool _isDead = false;

    // ?? TutorialManager が使用するイベント (前回修正済み)
    public event Action onDeath;

    void Start()
    {
        _currentHP = maxHP;
    }

    /// <summary>
    /// プレイヤーから呼び出されるメソッド
    /// </summary>
    /// <param name="damageAmount">受けるダメージ量</param>
    public void TakeDamage(float damageAmount)
    {
        if (_isDead) return;

        _currentHP -= damageAmount;

        Debug.Log($"{gameObject.name} (敵) ダメージ:{damageAmount} | 残りHP:{_currentHP}");

        if (_currentHP <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (_isDead) return;

        _isDead = true;

        // 1. TutorialManager に通知 (これがないとチュートリアルが進まない)
        onDeath?.Invoke();

        Debug.Log($"{gameObject.name} が破壊されました。");

        // 2. 敵のゲームオブジェクトを削除
        Destroy(gameObject, 0.1f); // 削除処理
    }
}