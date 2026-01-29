using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class FadeManager : MonoBehaviour
{
    private CanvasGroup canvasGroup;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();

        // 1. 最初は完全に透明にする
        canvasGroup.alpha = 0;

        // 2. 透明な時はマウス操作を突き抜けさせる
        canvasGroup.blocksRaycasts = false;

        // 3. 透明な時は描画自体をオフにする（他のUIが見えるようになる）
        canvasGroup.interactable = false;
    }

    public IEnumerator FadeOut(float duration)
    {
        // フェード開始時に操作をブロックし、最前面へ
        canvasGroup.blocksRaycasts = true;

        float timer = 0;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            canvasGroup.alpha = timer / duration;
            yield return null;
        }
        canvasGroup.alpha = 1;
    }
}