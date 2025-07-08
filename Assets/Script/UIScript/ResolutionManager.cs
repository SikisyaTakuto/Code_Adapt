using UnityEngine;
using UnityEngine.UI; // 必要に応じてUI要素を更新する場合

public class ResolutionManager : MonoBehaviour
{
    private int currentResolutionIndex = 0;

    // 定義済みの解像度リスト (幅, 高さ, フルスクリーンかどうか)
    private ResolutionSetting[] resolutions = new ResolutionSetting[]
    {
        new ResolutionSetting(1920, 1080, FullScreenMode.ExclusiveFullScreen), // 初期設定: フルスクリーン
        new ResolutionSetting(1920, 1080, FullScreenMode.Windowed),          // ウィンドウモード
        new ResolutionSetting(1280, 720, FullScreenMode.Windowed),           // 小さいウィンドウ
        new ResolutionSetting(1280, 720, FullScreenMode.ExclusiveFullScreen), // 小さいフルスクリーン
        // さらに解像度を追加できます
    };

    // 解像度設定を保持する構造体
    [System.Serializable]
    public struct ResolutionSetting
    {
        public int width;
        public int height;
        public FullScreenMode fullScreenMode;

        public ResolutionSetting(int w, int h, FullScreenMode mode)
        {
            width = w;
            height = h;
            fullScreenMode = mode;
        }

        public override string ToString()
        {
            string modeText = "";
            switch (fullScreenMode)
            {
                case FullScreenMode.ExclusiveFullScreen:
                    modeText = "フルスクリーン";
                    break;
                case FullScreenMode.Windowed:
                    modeText = "ウィンドウ";
                    break;
                case FullScreenMode.FullScreenWindow:
                    modeText = "ボーダーレスフルスクリーン";
                    break;
                case FullScreenMode.MaximizedWindow:
                    modeText = "最大化ウィンドウ";
                    break;
            }
            return $"{width}x{height} ({modeText})";
        }
    }

    void Start()
    {
        // ゲーム開始時に初期解像度を設定
        ApplyResolution(resolutions[currentResolutionIndex]);
    }

    // このメソッドを画面サイズ変更ボタンのOnClickイベントに設定します
    public void CycleResolution()
    {
        currentResolutionIndex = (currentResolutionIndex + 1) % resolutions.Length;
        ApplyResolution(resolutions[currentResolutionIndex]);
        Debug.Log($"画面サイズを変更しました: {resolutions[currentResolutionIndex]}");
    }

    private void ApplyResolution(ResolutionSetting setting)
    {
        Screen.SetResolution(setting.width, setting.height, setting.fullScreenMode);
    }
}