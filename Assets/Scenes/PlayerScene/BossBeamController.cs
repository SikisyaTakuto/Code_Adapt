using UnityEngine;

public class BossBeamController : MonoBehaviour
{
    [Header("Visual Settings")]
    public float lifetime = 1.0f;
    public Transform beamVisual;

    [Header("Damage Settings")]
    public float damageAmount = 20f;
    [SerializeField] private float damageInterval = 0.5f; // ダ滅間隔
    [SerializeField] private float beamRange = 50f;      // ビームの射程距離

    private float _nextDamageTime = 0f;
    private Transform _firePoint; // 発射地点の参照を保持

    void Start()
    {
        // 発射時の親オブジェクト（ビットや盾）を保存
        _firePoint = transform.parent;
        Destroy(gameObject, lifetime);
    }

    // Fireメソッドは初期設定のみに使用
    public void Fire(Vector3 startPoint, Vector3 endPoint, bool didHit, GameObject hitObject = null)
    {
        // 初期ログ。Updateでリアルタイム判定するため、ここでのダメージ処理は削除
        Debug.Log($"[Beam] Fire Start. Duration: {lifetime}");
    }

    void Update()
    {
        // 常にビームの方向にレイを飛ばして判定
        CheckBeamHit();
    }

    private void CheckBeamHit()
    {
        // 1. ビームの向きにレイを飛ばす
        // transform.forward は Instantiate 時に LookRotation で設定されている前提
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, beamRange))
        {
            // 2. 当たったものが PlayerStatus を持っているか確認
            PlayerStatus status = hit.collider.GetComponentInParent<PlayerStatus>() ??
                                 hit.collider.GetComponentInChildren<PlayerStatus>() ??
                                 hit.collider.GetComponent<PlayerStatus>();

            if (status != null)
            {
                // 3. ダメージ間隔のチェック
                if (Time.time >= _nextDamageTime)
                {
                    ApplyDamage(status, hit.collider.gameObject);
                    _nextDamageTime = Time.time + damageInterval;
                }
            }
        }
    }

    private void ApplyDamage(PlayerStatus status, GameObject hitTarget)
    {
        float defense = 1.0f;
        // 防御倍率の取得
        var balance = hitTarget.GetComponentInParent<BlanceController>() ??
                      hitTarget.GetComponentInChildren<BlanceController>() ??
                      hitTarget.GetComponent<BlanceController>();

        // if (balance != null) defense = balance.defenseRate;

        status.TakeDamage(damageAmount, defense);
        Debug.Log($"[Beam] {status.gameObject.name} が射線上にいるためダメージ適用！");
    }
}