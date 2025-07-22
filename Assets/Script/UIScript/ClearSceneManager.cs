using UnityEngine;
using UnityEngine.SceneManagement; // SceneManager���g�p���邽�߂ɕK�v

/// <summary>
/// �Q�[���N���A�V�[���̊Ǘ��ƃ^�C�g���V�[���ւ̑J�ڂ��s���X�N���v�g�B
/// </summary>
public class ClearSceneManager : MonoBehaviour
{
    [Tooltip("�^�C�g���V�[���̃r���h�ݒ�ł̖��O�B")]
    public string titleSceneName = "TitleScene"; // �^�C�g���V�[���̖��O��Inspector�Őݒ�ł���悤�ɂ���

    void Start()
    {
        // �}�E�X�J�[�\�����ēx�\�����A���b�N��Ԃ���������
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        Debug.Log("�}�E�X�J�[�\����\�����܂����B");
    }

    /// <summary>
    /// �u�^�C�g���ɖ߂�v�{�^���������ꂽ�Ƃ��ɌĂяo����郁�\�b�h�B
    /// </summary>
    public void BackToTitle()
    {
        Debug.Log("Back to Title button pressed. Loading TitleScene...");
        // �w�肳�ꂽ���O�̃V�[�������[�h
        SceneManager.LoadScene(titleSceneName);
    }
}
