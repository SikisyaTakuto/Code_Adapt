using UnityEngine;
using UnityEngine.UI; // �K�v�ɉ�����UI�v�f���X�V����ꍇ

public class ResolutionManager : MonoBehaviour
{
    private int currentResolutionIndex = 0;

    // ��`�ς݂̉𑜓x���X�g (��, ����, �t���X�N���[�����ǂ���)
    private ResolutionSetting[] resolutions = new ResolutionSetting[]
    {
        new ResolutionSetting(1920, 1080, FullScreenMode.ExclusiveFullScreen), // �����ݒ�: �t���X�N���[��
        new ResolutionSetting(1920, 1080, FullScreenMode.Windowed),          // �E�B���h�E���[�h
        new ResolutionSetting(1280, 720, FullScreenMode.Windowed),           // �������E�B���h�E
        new ResolutionSetting(1280, 720, FullScreenMode.ExclusiveFullScreen), // �������t���X�N���[��
        // ����ɉ𑜓x��ǉ��ł��܂�
    };

    // �𑜓x�ݒ��ێ�����\����
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
                    modeText = "�t���X�N���[��";
                    break;
                case FullScreenMode.Windowed:
                    modeText = "�E�B���h�E";
                    break;
                case FullScreenMode.FullScreenWindow:
                    modeText = "�{�[�_�[���X�t���X�N���[��";
                    break;
                case FullScreenMode.MaximizedWindow:
                    modeText = "�ő剻�E�B���h�E";
                    break;
            }
            return $"{width}x{height} ({modeText})";
        }
    }

    void Start()
    {
        // �Q�[���J�n���ɏ����𑜓x��ݒ�
        ApplyResolution(resolutions[currentResolutionIndex]);
    }

    // ���̃��\�b�h����ʃT�C�Y�ύX�{�^����OnClick�C�x���g�ɐݒ肵�܂�
    public void CycleResolution()
    {
        currentResolutionIndex = (currentResolutionIndex + 1) % resolutions.Length;
        ApplyResolution(resolutions[currentResolutionIndex]);
        Debug.Log($"��ʃT�C�Y��ύX���܂���: {resolutions[currentResolutionIndex]}");
    }

    private void ApplyResolution(ResolutionSetting setting)
    {
        Screen.SetResolution(setting.width, setting.height, setting.fullScreenMode);
    }
}