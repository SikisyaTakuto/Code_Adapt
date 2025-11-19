using UnityEngine;

/// <summary>
/// 生成されたビームを指定時間後に破壊し、Raycastingで衝突判定とダメージ処理を行うスクリプト。
/// </summary>
public class BeamDestroyer : MonoBehaviour
{
    [Tooltip("ビームが自動的に破壊されるまでの時間 (秒)。")]
    public float lifetime = 5f;

    [Header("移動とダメージ設定")]
    [Tooltip("ビームの移動速度。")]
    public float beamSpeed = 40f;

    [Tooltip("ビームが与えるダメージ量。")]
    public int damageAmount = 10;

    // 前フレームの位置を保存し、Raycastingの始点とする
    private Vector3 previousPosition;

    private void Start()
    {
        // オブジェクトを一定時間後に破壊する
        Destroy(gameObject, lifetime);

        // 初期の位置を保存
        previousPosition = transform.position;
    }

    private void Update()
    {
        // 1. ビームを前方に移動させる (物理エンジン不使用)
        transform.position += transform.forward * beamSpeed * Time.deltaTime;

        // 2. Raycastingによる衝突判定

        // Rayの始点: 前フレームの位置
        Vector3 rayStart = previousPosition;

        // Rayの方向: 前フレームの位置から現在の位置へ向かうベクトル
        Vector3 rayDirection = transform.position - previousPosition;

        // Rayの距離: 2点間の距離 (今回の移動距離)
        float distance = rayDirection.magnitude;

        // 進行方向の単位ベクトルを取得
        rayDirection.Normalize();

        RaycastHit hit;

        // Raycast(始点, 方向, 衝突情報, 距離)
        if (Physics.Raycast(rayStart, rayDirection, out hit, distance))
        {
            // Playerタグのオブジェクトに当たった場合の処理
            if (hit.collider.CompareTag("Player"))
            {
                // ?? ダメージ処理を呼び出す
                ApplyDamage(hit.collider.gameObject);
            }

            // 衝突位置にビームを移動させてから破壊（ビームが壁などを貫通しないように）
            transform.position = hit.point;
            Destroy(gameObject);
        }

        // 3. 次のフレームのために位置を更新
        previousPosition = transform.position;
    }

    /// <summary>
    /// 衝突したオブジェクトにダメージを与える処理。
    /// PlayerControllerまたはTutorialPlayerControllerに対応します。
    /// </summary>
    private void ApplyDamage(GameObject target)
    {
        // 1. PlayerControllerコンポーネントを探す
        PlayerController player = target.GetComponent<PlayerController>();

        if (player != null)
        {
            // ?? PlayerControllerにTakeDamageがある場合
            player.TakeDamage(damageAmount);
            Debug.Log($"PlayerControllerに{damageAmount}ダメージを与えました。", target);
            return; // ダメージ処理が完了したので終了
        }

        // 2. PlayerControllerが見つからなかった場合、TutorialPlayerControllerコンポーネントを探す
        TutorialPlayerController tutorialPlayer = target.GetComponent<TutorialPlayerController>();

        if (tutorialPlayer != null)
        {
            // ?? TutorialPlayerControllerにTakeDamageがある場合
            tutorialPlayer.TakeDamage(damageAmount);
            Debug.Log($"TutorialPlayerControllerに{damageAmount}ダメージを与えました。", target);
            return; // ダメージ処理が完了したので終了
        }

        // どちらも見つからなかった場合
        Debug.LogWarning("Playerタグのオブジェクトにダメージ処理を行う (PlayerController または TutorialPlayerController) コンポーネントが見つかりません。", target);
    }
}