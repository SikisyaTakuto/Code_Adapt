using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio; // Audio Mixer���g�����߂ɕK�v

public class UISystem : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject settingsPanel; // �ݒ�p�l���S�́i�w�i��X���C�_�[���܂ށj
    [SerializeField] private Slider bgmSlider;         // BGM�p�X���C�_�[
    [SerializeField] private Text bgmVolumeText;       // BGM�̉��ʕ\���e�L�X�g
    [SerializeField] private Slider seSlider;          // SE�p�X���C�_�[
    [SerializeField] private Text seVolumeText;        // SE�̉��ʕ\���e�L�X�g

    [Header("Audio Settings")]
    [SerializeField] private AudioMixer mainMixer;     // Audio Mixer�ւ̎Q��

    // Audio Mixer�̌��J�p�����[�^���iInspector�Őݒ肵�����̂ƈ�v������j
    private const string BGM_VOLUME_PARAM = "BGMVolume";
    private const string SE_VOLUME_PARAM = "SEVolume";

    void Start()
    {
        // ������Ԃł͐ݒ�p�l�����\���ɂ���
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        // �X���C�_�[�Ƀ��X�i�[��ǉ�
        if (bgmSlider != null)
        {
            bgmSlider.onValueChanged.AddListener(SetBGMVolume);
        }
        if (seSlider != null)
        {
            seSlider.onValueChanged.AddListener(SetSEVolume);
        }

        // Audio Mixer�̌��݂̒l���擾���A�X���C�_�[�ƃe�L�X�g�ɔ��f
        LoadAndApplyVolumeSettings();
    }

    /// <summary>
    /// �ݒ�p�l���̕\��/��\����؂�ւ���
    /// �i�ݒ�{�^����OnClick�C�x���g�ɐݒ�j
    /// </summary>
    public void ToggleSettingsPanel()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(!settingsPanel.activeSelf);
        }
    }

    /// <summary>
    /// BGM���ʂ�ݒ肵�A�e�L�X�g���X�V����
    /// �iBGM�X���C�_�[��OnValueChanged�C�x���g�ɐݒ�j
    /// </summary>
    public void SetBGMVolume(float volume)
    {
        if (mainMixer != null)
        {
            mainMixer.SetFloat(BGM_VOLUME_PARAM, volume);
            UpdateVolumeText(bgmVolumeText, volume);
        }
    }

    /// <summary>
    /// SE���ʂ�ݒ肵�A�e�L�X�g���X�V����
    /// �iSE�X���C�_�[��OnValueChanged�C�x���g�ɐݒ�j
    /// </summary>
    public void SetSEVolume(float volume)
    {
        if (mainMixer != null)
        {
            mainMixer.SetFloat(SE_VOLUME_PARAM, volume);
            UpdateVolumeText(seVolumeText, volume);
        }
    }

    /// <summary>
    /// �X���C�_�[�ƃe�L�X�g�Ɍ��݂̉��ʐݒ�����[�h���ēK�p����
    /// </summary>
    private void LoadAndApplyVolumeSettings()
    {
        float currentBGMVolume;
        if (mainMixer != null && mainMixer.GetFloat(BGM_VOLUME_PARAM, out currentBGMVolume))
        {
            if (bgmSlider != null)
            {
                bgmSlider.value = currentBGMVolume; // �X���C�_�[�̒l�𓯊�
            }
            UpdateVolumeText(bgmVolumeText, currentBGMVolume); // �e�L�X�g���X�V
        }

        float currentSEVolume;
        if (mainMixer != null && mainMixer.GetFloat(SE_VOLUME_PARAM, out currentSEVolume))
        {
            if (seSlider != null)
            {
                seSlider.value = currentSEVolume; // �X���C�_�[�̒l�𓯊�
            }
            UpdateVolumeText(seVolumeText, currentSEVolume); // �e�L�X�g���X�V
        }
    }

    /// <summary>
    /// ���ʕ\���e�L�X�g���X�V����w���p�[���\�b�h
    /// </summary>
    private void UpdateVolumeText(Text volumeText, float volume)
    {
        if (volumeText != null)
        {
            // dB�l���p�[�Z���e�[�W�܂��͕�����₷�����l�ɕϊ����ĕ\��
            // ��: -80dB��0%�A0dB��100%�Ƃ��ĕ\��
            // dB�l�̓��j�A�ł͂Ȃ����߁A�P���ȃp�[�Z���e�[�W�ϊ��͊��o�ƈقȂ�ꍇ������
            // �����ł͊ȈՓI��0.1f���݂Ŏl�̌ܓ����ĕ\��
            volumeText.text = $"{Mathf.Round(volume * 10f) / 10f} dB";
            // �����p�[�Z���e�[�W�ŕ\���������ꍇ�͈ȉ��̌v�Z���Q�l�ɂ��Ă�������
            // float normalizedVolume = Mathf.InverseLerp(bgmSlider.minValue, bgmSlider.maxValue, volume);
            // volumeText.text = $"{Mathf.RoundToInt(normalizedVolume * 100)}%";
        }
    }
}