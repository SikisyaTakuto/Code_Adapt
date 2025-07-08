using UnityEngine;
using UnityEngine.SceneManagement; // �V�[���J�ڂ̂��߂ɕK�v

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private string _selectedStageName; // �I�����ꂽ�X�e�[�W�̃V�[����

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // �V�[���J�ڌ�����̃I�u�W�F�N�g��j�����Ȃ�
        }
        else
        {
            Destroy(gameObject); // ���ɃC���X�^���X�����݂���ꍇ�́A�d���I�u�W�F�N�g��j��
        }
    }

    /// <summary>
    /// �I�����ꂽ�X�e�[�W�̃V�[������ݒ肵�܂��B
    /// </summary>
    /// <param name="stageName">�I�����ꂽ�X�e�[�W�̃V�[����</param>
    public void SetSelectedStage(string stageName)
    {
        _selectedStageName = stageName;
    }

    /// <summary>
    /// �I�����ꂽ�X�e�[�W�̃V�[�������擾���܂��B
    /// </summary>
    /// <returns>�I�����ꂽ�X�e�[�W�̃V�[����</returns>
    public string GetSelectedStageName()
    {
        return _selectedStageName;
    }

    /// <summary>
    /// �ŏI�I�ɑI�����ꂽ�X�e�[�W�ɑJ�ڂ��܂��B
    /// �Ⴆ�΁AArmorSelectScene�Ŗh��I����A���̃��\�b�h���Ăяo���ăX�e�[�W�֑J�ڂ��܂��B
    /// </summary>
    public void GoToSelectedStage()
    {
        if (!string.IsNullOrEmpty(_selectedStageName))
        {
            Debug.Log($"�I�����ꂽ�X�e�[�W {_selectedStageName} �ֈړ����܂��B");
            SceneManager.LoadScene(_selectedStageName);
        }
        else
        {
            Debug.LogError("�I�����ꂽ�X�e�[�W������܂���B");
        }
    }
}