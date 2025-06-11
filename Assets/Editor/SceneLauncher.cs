using UnityEditor.SceneManagement;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Unity�G�f�B�^��̃��j���[�ɃJ�X�^�����ڂ�ǉ����A����̃V�[�����ȒP�ɊJ���郉���`���[�@�\��񋟂���N���X�B
/// </summary>
public class SceneLauncher
{
    // ���j���[�ɁuLauncher/Enemy Scene�v�Ƃ������ڂ�ǉ�
    // �N���b�N�����Enemy Test Scene���J�����
    [MenuItem("Launcher/Enemy Scene", priority = 0)]
    public static void OpenGameScene()
    {
        OpenSceneWithSaveCheck("Assets/Scenes/EnemyScene/EnemyTestScene.unity");
    }

    // ���j���[�ɁuLauncher/PlayerScene�v�Ƃ������ڂ�ǉ�
    // �N���b�N�����PlayerScene���J�����
    [MenuItem("Launcher/PlayerScene", priority = 0)]
    public static void OpenSampleScene()
    {
        OpenSceneWithSaveCheck("Assets/Scenes/PlayerScene/PlayerScene.unity");
    }

    /// <summary>
    /// �V�[�����J���O�ɁA���݂̃V�[���ɖ��ۑ��̕ύX������ꍇ�͕ۑ��𑣂��A
    /// �w�肳�ꂽ�p�X�̃V�[�����J���B
    /// </summary>
    /// <param name="scenePath">�J�������V�[���̃v���W�F�N�g���p�X</param>
    static void OpenSceneWithSaveCheck(string scenePath)
    {
        // �w�肳�ꂽ�V�[���t�@�C�������݂��邩�m�F
        if (!System.IO.File.Exists(scenePath))
        {
            Debug.LogError($"�V�[����������܂���: {scenePath}");
            return;
        }

        // ���݂̃V�[���ɕύX������ꍇ�A�ۑ����邩�ǂ��������[�U�[�Ɋm�F
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            // �V�[����P�ꃂ�[�h�ŊJ���i���݂̃V�[������Ďw��̃V�[���ɐ؂�ւ���j
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        }
    }
}