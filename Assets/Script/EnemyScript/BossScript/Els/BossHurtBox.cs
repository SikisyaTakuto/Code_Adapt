// ファイル名: BossHurtBox.cs
using UnityEngine;

public class BossHurtBox : MonoBehaviour
{
    private ElsController _mainBoss;

    void Start()
    {
        // 親階層にある ElsController を自動取得
        _mainBoss = GetComponentInParent<ElsController>();
    }

    public void OnHit(float damage)
    {
        if (_mainBoss != null)
        {
            _mainBoss.TakeDamage(damage);
        }
    }
}