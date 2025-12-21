using UnityEngine;

public class DoorController : MonoBehaviour
{
    private Transform playerTransform;

    public float openDistance = 3f;   // 開く距離
    public float openHeight = 3f;     // 開く高さ
    public float speed = 3f;          // 開閉スピード

    private Vector3 closedPos;        // 閉じた位置
    private Vector3 openPos;          // 開いた位置

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip openSound;       // 扉が開く時の音
    public AudioClip closeSound;      // 扉が閉まる時の音

    private bool isOpening = false;   // 現在開く動作中か
    private bool isClosing = false;   // 現在閉じる動作中か

    void Start()
    {
        closedPos = transform.position;
        openPos = closedPos + Vector3.up * openHeight;

        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
        else
        {
            Debug.LogError("シーン内に 'Player' タグがついたオブジェクトが見つかりません！");
        }

        // AudioSourceがアサインされていない場合の自動取得
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (playerTransform == null) return;

        float distance = Vector3.Distance(playerTransform.position, transform.position);
        bool isPlayerNear = distance < openDistance;

        if (isPlayerNear)
        {
            // --- 開く処理 ---
            // まだ「開く動作中」でなく、かつ完全に開ききっていない場合に音を鳴らす
            if (!isOpening && transform.position != openPos)
            {
                PlaySound(openSound);
                isOpening = true;
                isClosing = false; // 閉じるフラグをリセット
            }

            transform.position = Vector3.MoveTowards(transform.position, openPos, speed * Time.deltaTime);
        }
        else
        {
            // --- 閉じる処理 ---
            // まだ「閉じる動作中」でなく、かつ完全に閉じきっていない場合に音を鳴らす
            if (!isClosing && transform.position != closedPos)
            {
                PlaySound(closeSound);
                isClosing = true;
                isOpening = false; // 開くフラグをリセット
            }

            transform.position = Vector3.MoveTowards(transform.position, closedPos, speed * Time.deltaTime);
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}