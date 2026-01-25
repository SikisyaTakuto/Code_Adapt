using UnityEngine;
using System.Collections; // コルーチンに必要

public class TargetObject : MonoBehaviour
{
    [Header("Settings")]
    public BossDoorController doorController;
    [Tooltip("これが最後のターゲットならチェックを入れる（完了後に消えます）")]
    public bool isLastTarget = false;

    [Header("Lever Rotation Settings")]
    [Tooltip("回転させたいパーツ（空なら自分自身）")]
    public Transform targetRotationObject;
    [SerializeField] private Vector3 rotationAmount = new Vector3(0, 90, 0);
    [SerializeField] private float rotationDuration = 1.0f; // 何秒かけて回転するか

    [Header("Animation")]
    public Animator animator;
    public string animationTrigger = "Activate";

    [Header("Input")]
    public KeyCode interactKey = KeyCode.F;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip leverSound;

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
                ExecuteLogic();
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

        // --- 滑らかな回転を開始 ---
        Transform objToRotate = (targetRotationObject != null) ? targetRotationObject : transform;
        StartCoroutine(SmoothRotation(objToRotate));

        // --- 以下、各種Nullチェック付き処理 ---
        if (audioSource != null && leverSound != null)
        {
            audioSource.PlayOneShot(leverSound);
        }

        if (animator != null)
        {
            animator.SetTrigger(animationTrigger);
        }

        if (doorController != null)
        {
            doorController.OpenDoor();
        }

        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.CompleteCurrentMission();
        }

        if (TargetManager.Instance != null)
        {
            TargetManager.Instance.CompleteCurrentObjective();
        }

        if (isLastTarget)
        {
            // 回転時間を考慮して少し遅めに消去
            Destroy(gameObject, rotationDuration + 0.5f);
        }
    }

    // 滑らかに回転させるコルーチン
    private IEnumerator SmoothRotation(Transform target)
    {
        Quaternion startRotation = target.localRotation;
        Quaternion endRotation = startRotation * Quaternion.Euler(rotationAmount);
        float elapsed = 0f;

        while (elapsed < rotationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / rotationDuration;

            // イージング（滑らかな動き出しと終わり）をつける
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            target.localRotation = Quaternion.Slerp(startRotation, endRotation, smoothT);
            yield return null;
        }

        // 最後に確実に目標角度に固定
        target.localRotation = endRotation;
    }
}