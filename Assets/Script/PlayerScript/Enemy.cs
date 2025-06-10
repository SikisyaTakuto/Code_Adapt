using UnityEngine;

// 1�̂̓G�L�����N�^�[�ɃA�^�b�`����X�N���v�g
public class Enemy : MonoBehaviour
{
    public string enemyName;
    public int hp;
    public int attack;

    // �O������X�e�[�^�X������������
    public void Initialize(EnemyData data)
    {
        enemyName = data.Name;
        hp = data.HP;
        attack = data.Attack;

        Debug.Log($"{enemyName} ���������FHP={hp}, �U����={attack}");
    }

    public void TakeDamage(int damage)
    {
        hp -= damage;
        Debug.Log($"{enemyName} �� {damage} �_���[�W���󂯂��i�c��HP: {hp}�j");
    }
}
