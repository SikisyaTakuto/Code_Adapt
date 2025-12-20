using UnityEngine;

public class BGMManager : MonoBehaviour
{
    public static BGMManager instance;

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip bgmClip; // 流したいBGM

    private void Awake()
    {
        // シーンを跨いでも破棄されない設定（シングルトン）
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // BGMの設定を自動で行う
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        audioSource.clip = bgmClip;
        audioSource.loop = true;  // ループ再生を有効にする
        audioSource.playOnAwake = true; // 起動時に再生

        if (!audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }

    // 他のスクリプトから曲を変えたい時に使う関数
    public void ChangeBGM(AudioClip newClip)
    {
        if (audioSource.clip == newClip) return; // 同じ曲なら何もしない

        audioSource.Stop();
        audioSource.clip = newClip;
        audioSource.Play();
    }
}