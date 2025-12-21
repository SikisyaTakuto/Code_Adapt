using UnityEngine;

public class TargetObject : MonoBehaviour
{
    [Header("Settings")]
    public BossDoorController doorController;
    [Tooltip("これが最後のターゲットならチェックを入れる（完了後に消えます）")]
    public bool isLastTarget = false;

    [Header("Animation")]
    public Animator animator;
    public string animationTrigger = "Activate";

    [Header("Input")]
    public KeyCode interactKey = KeyCode.F;

    [Header("Audio")] // ★オーディオ設定を追加
    public AudioSource audioSource;
    public AudioClip leverSound; // レバーを倒す時の音

    private bool isActivated = false;
    private bool isPlayerInRange = false;

    private void Update()
    {
        if (isPlayerInRange && !isActivated && gameObject.CompareTag("Lever"))
        {
            if (Input.GetKeyDown(interactKey))
            {
                Debug.Log("レバーを操作しました（Fキー）");
                ExecuteLogic();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;

            if (gameObject.CompareTag("TargetCheck") && !isActivated)
            {
                Debug.Log("TargetCheckに接触：自動で更新します");
                ExecuteLogic();
            }
            else if (gameObject.CompareTag("Lever") && !isActivated)
            {
                Debug.Log("Leverに接触：Fキーを押してください");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
        }
    }

    private void ExecuteLogic()
    {
        if (isActivated) return;
        isActivated = true;

        // ★ 0. 効果音の再生
        if (audioSource != null && leverSound != null)
        {
            audioSource.PlayOneShot(leverSound);
        }

        // 1. アニメーション再生
        if (animator != null)
        {
            animator.SetTrigger(animationTrigger);
        }

        // 2. 扉を開ける処理
        if (doorController != null)
        {
            doorController.OpenDoor();
        }

        // 3. ミッションテキストを更新
        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.CompleteCurrentMission();
        }

        // 4. 矢印を次の目的地へ更新
        if (TargetManager.Instance != null)
        {
            TargetManager.Instance.CompleteCurrentObjective();
        }

        // 5. 最後のターゲットなら消去
        if (isLastTarget)
        {
            // 音が途切れないよう、Destoryの時間を少し余裕を持たせます
            Destroy(gameObject, 1.5f);
        }

        Debug.Log($"{gameObject.name} の処理が完了しました。");
    }
}