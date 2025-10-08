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

    // メニューに「Launcher/PlayerScene」という項目を追加
    // クリックするとPlayerSceneが開かれる
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