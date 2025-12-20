using System;
using UnityEngine;

public class SEManager : MonoBehaviour
{
    public static SEManager instance;

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip seClip;

    private void Awake()
    {
        if (ReferenceEquals(instance, null))
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            // AudioSourceの自動取得（セットし忘れ対策）
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
    public void PlaySE()
    {
        // もし自分が「消される側」のマネージャーだった場合、
        // 本物のマネージャー（instance）に再生を頼む
        if (instance != null && instance != this)
        {
            instance.PlaySE();
            return;
        }

        // ここからが本物の再生処理
        if (audioSource != null && audioSource.isActiveAndEnabled)
        {
            if (seClip != null)
            {
                audioSource.PlayOneShot(seClip);
            }
            else
            {
                Debug.LogWarning("SEClipが設定されていません。");
            }
        }
        else
        {
            Debug.LogWarning("AudioSourceが無効、またはアタッチされていません。");
        }

    }
}