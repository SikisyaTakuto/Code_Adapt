using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class StationBoardScroll : MonoBehaviour
{
    [Header("設定")]
    public float scrollInterval = 0.2f; // 切り替わる速度（秒）
    public int displayWidth = 20;     // 表示する文字数（枠の広さ）

    private Text uiText;
    private string bufferText;        // 計算用のバッファ

    void Start()
    {
        uiText = GetComponent<Text>();

        // 【修正ポイント】
        // 最初から文字を表示するため、文章の後ろにだけ空白を足す
        // これにより、最初は文章の先頭が表示され、徐々に左へ消えていく動きになります
        string space = new string(' ', displayWidth);
        bufferText = uiText.text + space;

        StartCoroutine(ScrollRoutine());
    }

    IEnumerator ScrollRoutine()
    {
        while (true)
        {
            // 1. 現在のバッファの先頭から表示幅分を切り出して表示
            // Substringで範囲外エラーにならないよう、バッファを一時的に拡張して判定
            string currentView = bufferText;
            if (currentView.Length < displayWidth)
            {
                currentView += new string(' ', displayWidth);
            }
            uiText.text = currentView.Substring(0, displayWidth);

            // 2. 指定した秒数待機
            yield return new WaitForSeconds(scrollInterval);

            // 3. 先頭の1文字を末尾に回す（文字をずらす）
            string firstChar = bufferText.Substring(0, 1);
            string rest = bufferText.Substring(1);
            bufferText = rest + firstChar;
        }
    }
}