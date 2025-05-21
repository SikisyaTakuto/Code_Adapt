using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 指定したコンポーネントを持つGameObjectをヒエラルキーから検索するエディタウィンドウ
/// </summary>
public class ComponentFinderWindow : EditorWindow
{
    // 検索するコンポーネント名（デフォルトは "BoxCollider"）
    private string componentTypeName = "BoxCollider";

    // 検索結果表示用スクロール位置
    private Vector2 scrollPos;

    // 検索結果として見つかったGameObjectのリスト
    private List<GameObject> results = new List<GameObject>();

    // エディタメニューに「Tools/Component Finder」項目を追加
    [MenuItem("Tools/Component Finder")]
    public static void ShowWindow()
    {
        // ウィンドウを開く（タイトルは "Component Finder"）
        GetWindow<ComponentFinderWindow>("Component Finder");
    }

    // エディタウィンドウのGUI描画処理
    void OnGUI()
    {
        // タイトルラベル
        GUILayout.Label("Search for Component in Hierarchy", EditorStyles.boldLabel);

        // コンポーネント名入力フィールド
        componentTypeName = EditorGUILayout.TextField("Component Type", componentTypeName);

        // 検索ボタン
        if (GUILayout.Button("Search"))
        {
            FindComponentsInScene(componentTypeName);
        }

        // 結果がある場合に表示
        if (results.Count > 0)
        {
            GUILayout.Label($"Found {results.Count} objects", EditorStyles.boldLabel);

            // 検索結果をスクロール表示
            scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Height(300));

            foreach (var obj in results)
            {
                // オブジェクト名のボタンを表示し、クリックで選択＆Ping表示
                if (GUILayout.Button(obj.name))
                {
                    Selection.activeGameObject = obj;
                    EditorGUIUtility.PingObject(obj);
                }
            }

            GUILayout.EndScrollView();
        }
    }

    /// <summary>
    /// 指定されたコンポーネント名を持つオブジェクトをシーン内から検索する
    /// </summary>
    /// <param name="typeName">検索するコンポーネントの型名（例："BoxCollider"）</param>
    void FindComponentsInScene(string typeName)
    {
        // 前回の検索結果をクリア
        results.Clear();

        // シーン内のすべてのGameObjectを取得
        var allObjects = GameObject.FindObjectsOfType<GameObject>();

        foreach (var obj in allObjects)
        {
            // 非表示オブジェクトはスキップ
            if (obj.hideFlags != HideFlags.None)
                continue;

            // 指定名のコンポーネントを持っているか確認
            var component = obj.GetComponent(typeName);
            if (component != null)
            {
                results.Add(obj);
            }
        }
    }
}