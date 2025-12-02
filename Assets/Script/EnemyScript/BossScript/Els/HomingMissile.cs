using UnityEngine;

public class HomingMissile : MonoBehaviour
{
    [Header("Missile Settings")]
    [SerializeField]
    private float moveSpeed = 5f;

    [SerializeField]
    private float rotationSpeed = 10f;

    [SerializeField]
    private float lifeTime = 5f;

    [Header("Launch Settings")]
    [SerializeField]
    private float initialLaunchAngle = 45f; // ★追加: 初期発射角度（垂直方向からの角度）

    [SerializeField]
    private float launchTime = 0.5f; // ★追加: 追尾を開始するまでの時間 (秒)

    [Header("Effect Settings")]
    [SerializeField]
    private GameObject explosionEffectPrefab;

    private Transform target;
    private float launchTimer; // 経過時間を計測するタイマー
    private bool isHoming = false; // ★追加: 追尾中かどうかを示すフラグ

    void Start()
    {
        // 1. 自動消滅タイマーの設定
        Invoke("ExplodeMissile", lifeTime);

        // 2. ターゲットの取得
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            target = player.transform;
        }
        else
        {
            Debug.LogError("Player with 'Player' tag not found. Missile will still launch and self-destruct.");
        }

        // 3. 発射角度の設定
        // Z軸 (前方) を基準に、X軸 (左右) 周りに initialLaunchAngle 分だけ回転させる
        // ※このスクリプトをアタッチする前に、ミサイルのZ軸が発射方向を向いていることを前提としています。
        transform.rotation *= Quaternion.Euler(-initialLaunchAngle, 0, 0);

        // 4. タイマーの初期化
        launchTimer = 0f;
        isHoming = false; // 発射直後は追尾しない
    }

    void Update()
    {
        // --- 1. 移動処理 (追尾中かどうかに関わらず常に前進) ---
        transform.position += transform.forward * moveSpeed * Time.deltaTime;

        // --- 2. 追尾判定と回転処理 ---

        if (!isHoming)
        {
            // 発射直後：タイマーを更新し、launchTimeを超えたら追尾開始
            launchTimer += Time.deltaTime;
            if (launchTimer >= launchTime)
            {
                isHoming = true;
            }
            // 追尾開始前は初期設定の角度のまま直進するため、ここでは回転処理を行わない
            return; // 追尾処理をスキップ
        }

        // 追尾中: ターゲットへ向かって旋回
        if (target != null)
        {
            Vector3 direction = target.position - transform.position;
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            // 滑らかにターゲット方向へ旋回
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    // ... (OnTriggerEnter および ExplodeMissile メソッドは変更なし) ...

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            ExplodeMissile();
        }
    }

    private void ExplodeMissile()
    {
        if (gameObject == null)
        {
            return;
        }

        if (explosionEffectPrefab != null)
        {
            Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
        }

        CancelInvoke("ExplodeMissile");
        Destroy(gameObject);
    }
}