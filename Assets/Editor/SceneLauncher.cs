using UnityEditor.SceneManagement;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Unityエディタ上のメニューにカスタム項目を追加し、特定のシーンを簡単に開けるランチャー機能を提供するクラス。
/// </summary>
public class SceneLauncher
{
    //// メニューに「Launcher/Enemy Scene」という項目を追加
    //// クリックするとEnemy Test Sceneが開かれる
    //[MenuItem("Launcher/Enemy Scene", priority = 0)]
    //public static void OpenEnemyScene()
    //{
    //    OpenSceneWithSaveCheck("Assets/Scenes/EnemyScene/EnemyTestScene.unity");
    //}

    [MenuItem("Launcher/TitleScene", priority = 0)]
    public static void OpenTitleScene()
    {
        OpenSceneWithSaveCheck("Assets/Scenes/GameScene/TitleScene.unity");
    }

    [MenuItem("Launcher/StageSelectScene", priority = 1)]
    public static void OpenStageSelectScene()
    {
        OpenSceneWithSaveCheck("Assets/Scenes/GameScene/StageSelectScene.unity");
    }

    [MenuItem("Launcher/TutorialScene", priority = 3)]
    public static void OpenTutorialScene()
    {
        OpenSceneWithSaveCheck("Assets/Scenes/GameScene/TutorialScene.unity");
    }


    [MenuItem("Launcher/ArmorSelectScene", priority =2)]
    public static void OpenArmorSelectScene()
    {
        OpenSceneWithSaveCheck("Assets/Scenes/GameScene/ArmorSelectScene/ArmorSelectScene.unity");
    }

    [MenuItem("Launcher/ArmorSelectScene1", priority = 2)]
    public static void OpenArmorSelectScene1()
    {
        OpenSceneWithSaveCheck("Assets/Scenes/GameScene/ArmorSelectScene/ArmorSelectScene 1.unity");
    }

    [MenuItem("Launcher/GameScene1", priority = 4)]
    public static void OpenGameScene1()
    {
        OpenSceneWithSaveCheck("Assets/Scenes/GameScene/GameScenes/GameSceneStage1.unity");
    }

    [MenuItem("Launcher/GameScene2", priority = 5)]
    public static void OpenGameScene2()
    {
        OpenSceneWithSaveCheck("Assets/Scenes/GameScene/GameScenes/GameSceneStage2.unity");
    }

    [MenuItem("Launcher/ClearScene", priority = 6)]
    public static void OpenClearScene()
    {
        OpenSceneWithSaveCheck("Assets/Scenes/GameScene/ClearScene/ClearScene.unity");
    }

    [MenuItem("Launcher/ClearScene1", priority = 7)]
    public static void OpenClearScene1()
    {
        OpenSceneWithSaveCheck("Assets/Scenes/GameScene/ClearScene/ClearScene1.unity");
    }

    [MenuItem("Launcher/ClearScene2", priority = 8)]
    public static void OpenClearScene2()
    {
        OpenSceneWithSaveCheck("Assets/Scenes/GameScene/ClearScene/ClearScene2.unity");
    }

    [MenuItem("Launcher/GameOverScene", priority = 9)]
    public static void OpenGameOverScene()
    {
        OpenSceneWithSaveCheck("Assets/Scenes/GameScene/GameOverScene/GameOverScene.unity");
    }

    [MenuItem("Launcher/GameOverScene1", priority = 10)]
    public static void OpenGameOverScene1()
    {
        OpenSceneWithSaveCheck("Assets/Scenes/GameScene/GameOverScene/GameOverScene.unity");
    }

    [MenuItem("Launcher/GameOverScene2", priority = 11)]
    public static void OpenGameOverScene2()
    {
        OpenSceneWithSaveCheck("Assets/Scenes/GameScene/GameOverScene/GameOverScene2.unity");
    }

    /// <summary>
    /// シーンを開く前に、現在のシーンに未保存の変更がある場合は保存を促し、
    /// 指定されたパスのシーンを開く。
    /// </summary>
    /// <param name="scenePath">開きたいシーンのプロジェクト内パス</param>
    static void OpenSceneWithSaveCheck(string scenePath)
    {
        // 指定されたシーンファイルが存在するか確認
        if (!System.IO.File.Exists(scenePath))
        {
            Debug.LogError($"シーンが見つかりません: {scenePath}");
            return;
        }

        // 現在のシーンに変更がある場合、保存するかどうかをユーザーに確認
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            // シーンを単一モードで開く（現在のシーンを閉じて指定のシーンに切り替える）
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        }
    }
}