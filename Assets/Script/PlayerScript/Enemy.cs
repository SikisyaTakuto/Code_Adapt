using UnityEngine;

// 1体の敵キャラクターにアタッチするスクリプト
public class Enemy : MonoBehaviour
{
    public string enemyName;
    public int hp;
    public int attack;

    // 外部からステータスを初期化する
    public void Initialize(EnemyData data)
    {
        enemyName = data.Name;
        hp = data.HP;
        attack = data.Attack;

        Debug.Log($"{enemyName} を初期化：HP={hp}, 攻撃力={attack}");
    }

    public void TakeDamage(int damage)
    {
        hp -= damage;
        Debug.Log($"{enemyName} は {damage} ダメージを受けた（残りHP: {hp}）");
    }
}
