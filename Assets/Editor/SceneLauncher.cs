using UnityEditor.SceneManagement;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Unity�G�f�B�^��̃��j���[�ɃJ�X�^�����ڂ�ǉ����A����̃V�[�����ȒP�ɊJ���郉���`���[�@�\��񋟂���N���X�B
/// </summary>
public class SceneLauncher
{
    //// ���j���[�ɁuLauncher/Enemy Scene�v�Ƃ������ڂ�ǉ�
    //// �N���b�N�����Enemy Test Scene���J�����
    //[MenuItem("Launcher/Enemy Scene", priority = 0)]
    //public static void OpenEnemyScene()
    //{
    //    OpenSceneWithSaveCheck("Assets/Scenes/EnemyScene/EnemyTestScene.unity");
    //}

    // ���j���[�ɁuLauncher/PlayerScene�v�Ƃ������ڂ�ǉ�
    // �N���b�N�����PlayerScene���J�����
    [MenuItem("Launcher/PlayerScene", priority = 0)]
    public static void OpenPlayerScene()
    {
        OpenSceneWithSaveCheck("Assets/Scenes/PlayerScene/PlayerScene.unity");
    }

    [MenuItem("Launcher/GameScene", priority = 0)]
    public static void OpenGameScene()
    {
        OpenSceneWithSaveCheck("Assets/Scenes/GameScene/GameScene.unity");
    }


    [MenuItem("Launcher/GameScene1", priority = 0)]
    public static void OpenGameScene1()
    {
        OpenSceneWithSaveCheck("Assets/Scenes/GameScene/GameScene1.unity");
    }


    [MenuItem("Launcher/GameScene2", priority = 0)]
    public static void OpenGameScene2()
    {
        OpenSceneWithSaveCheck("Assets/Scenes/GameScene/GameScene2.unity");
    }

    [MenuItem("Launcher/ArmorSelectScene", priority = 0)]
    public static void OpenArmorSelectScene()
    {
        OpenSceneWithSaveCheck("Assets/Scenes/GameScene/ArmorSelectScene.unity");
    }

    [MenuItem("Launcher/TitleScene", priority = 0)]
    public static void OpenTitleScene()
    {
        OpenSceneWithSaveCheck("Assets/Scenes/GameScene/TitleScene.unity");
    }

    [MenuItem("Launcher/StageSelectScene", priority = 0)]
    public static void OpenStageSelectScene()
    {
        OpenSceneWithSaveCheck("Assets/Scenes/GameScene/StageSelectScene.unity");
    }

    [MenuItem("Launcher/ClearScene", priority = 0)]
    public static void OpenSampleScene()
    {
        OpenSceneWithSaveCheck("Assets/Scenes/GameScene/ClearScene.unity");
    }

    [MenuItem("Launcher/TutorialScene", priority = 0)]
    public static void OpenTutorialScene()
    {
        OpenSceneWithSaveCheck("Assets/Scenes/GameScene/TutorialScene.unity");
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