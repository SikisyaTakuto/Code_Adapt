using UnityEngine;

public class VoxPart : MonoBehaviour
{
    private VoxController mainBoss;

    void Start()
    {
        // 親を遡って VoxController を探す
        mainBoss = GetComponentInParent<VoxController>();
    }

    public void TakeDamage(float amount)
    {
        if (mainBoss != null)
        {
            // 本体へダメージを転送
            mainBoss.TakeDamage(amount);

            // ついでにアーム固有のDamageArmも呼びたい場合は、
            // エディタ側で設定したインデックスを保持させて呼び出せます
        }
    }
}