using UnityEngine;
using UnityEngine.SceneManagement; // �V�[���Ǘ��̂��߂ɕK�v

public class SceneLoader : MonoBehaviour
{
    // ���̃��\�b�h���{�^����OnClick�C�x���g�ɐݒ肵�܂�
    public void LoadStageSelectScene()
    {
        Debug.Log("�Q�[���X�^�[�g�{�^����������܂����B�X�e�[�W�Z���N�g��ʂɈڍs���܂��B");
        SceneManager.LoadScene("StageSelectScene"); // StageSelectScene�̃V�[�������w��
    }
}