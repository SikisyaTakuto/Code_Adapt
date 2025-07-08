using UnityEngine;
using UnityEngine.SceneManagement; // �V�[���J�ڂ̂��߂ɕK�v

public class StageSelectManager : MonoBehaviour
{
    public static StageSelectManager Instance { get; private set; }

    [Header("�J�ڐݒ�")]
    [Tooltip("�h��I���V�[���̃V�[����")]
    public string armorSelectSceneName = "ArmorSelectScene"; // �f�t�H���g�l

    private string selectedStageName; // �I�����ꂽ�X�e�[�W�̃V�[����

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // �V�[���J�ڌ�����̏���ێ��������ꍇ��DontDestroyOnLoad���g��
            // DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// �X�e�[�W���I�����ꂽ�Ƃ��ɌĂяo����܂��B
    /// </summary>
    /// <param name="stageName">�I�����ꂽ�X�e�[�W�̃V�[����</param>
    public void SelectStage(string stageName)
    {
        selectedStageName = stageName;
        Debug.Log($"�I�����ꂽ�X�e�[�W: {selectedStageName}");

        // �I�����ꂽ�X�e�[�W���� GameManager �ɕۑ��i��������΁j
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetSelectedStage(selectedStageName);
        }

        // �h��I���V�[���֑J��
        SceneManager.LoadScene(armorSelectSceneName);
    }

    /// <summary>
    /// �I�����ꂽ�X�e�[�W�̃V�[�������擾���܂��B
    /// </summary>
    /// <returns>�I�����ꂽ�X�e�[�W�̃V�[����</returns>
    public string GetSelectedStageName()
    {
        return selectedStageName;
    }
}