using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections; // Coroutine�̂��߂ɕK�v

public class SceneLoader : MonoBehaviour
{
    // �V�[���؂�ւ�����SE�̍Đ����ԁiSE�̒����ɂ��j
    [SerializeField] private float sePlayDuration = 0.5f;

    public void LoadStageSelectScene()
    {
        // �܂�SE���Đ�
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClickSE();
            // SE���Đ������̂�҂��Ă���V�[�������[�h����R���[�`�����J�n
            StartCoroutine(LoadSceneAfterSE("StageSelectScene"));
        }
        else
        {
            Debug.LogError("AudioManager.Instance is null. Cannot play button click SE.");
            // AudioManager���Ȃ��ꍇ�ł��V�[���̓��[�h����
            SceneManager.LoadScene("StageSelectScene");
        }
    }

    private IEnumerator LoadSceneAfterSE(string sceneName)
    {
        // SE�̍Đ���҂�
        yield return new WaitForSeconds(sePlayDuration);

        Debug.Log("�Q�[���X�^�[�g�{�^����������܂����B�X�e�[�W�Z���N�g��ʂɈڍs���܂��B");
        SceneManager.LoadScene(sceneName);
    }
}