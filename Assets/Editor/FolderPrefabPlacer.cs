using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class FolderPrefabPlacer : EditorWindow
{
    private List<string> folderPaths = new List<string>() { "Assets/Prefabs" };
    private List<GameObject> prefabList = new List<GameObject>();
    private HashSet<GameObject> selectedPrefabs = new HashSet<GameObject>();
    private Vector2 scroll;

    [MenuItem("Tools/Folder Prefab Placer")]
    public static void ShowWindow()
    {
        GetWindow<FolderPrefabPlacer>("Folder Prefab Placer");
    }

    private void OnEnable()
    {
        LoadPrefabsFromFolders();
    }

    private void OnGUI()
    {
        GUILayout.Label("Prefab Folders", EditorStyles.boldLabel);

        for (int i = 0; i < folderPaths.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            folderPaths[i] = EditorGUILayout.TextField($"Folder {i + 1}", folderPaths[i]);
            if (GUILayout.Button("?", GUILayout.Width(20)))
            {
                folderPaths.RemoveAt(i);
                LoadPrefabsFromFolders();
                return;
            }
            EditorGUILayout.EndHorizontal();
        }

        if (GUILayout.Button("+ Add Folder"))
        {
            folderPaths.Add("Assets/");
        }

        if (GUILayout.Button("Reload All Prefabs"))
        {
            LoadPrefabsFromFolders();
        }

        if (prefabList.Count == 0)
        {
            EditorGUILayout.HelpBox("指定されたフォルダにプレハブが見つかりませんでした。", MessageType.Info);
            return;
        }

        scroll = EditorGUILayout.BeginScrollView(scroll);

        int columns = 4;
        int size = 80;
        int col = 0;
        EditorGUILayout.BeginHorizontal();

        foreach (var prefab in prefabList)
        {
            if (col >= columns)
            {
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                col = 0;
            }

            Texture2D preview = AssetPreview.GetAssetPreview(prefab);
            if (preview == null) preview = AssetPreview.GetMiniThumbnail(prefab);

            GUIStyle style = new GUIStyle(GUI.skin.button);
            if (selectedPrefabs.Contains(prefab))
            {
                style.normal.background = Texture2D.grayTexture;
            }

            if (GUILayout.Button(preview, style, GUILayout.Width(size), GUILayout.Height(size)))
            {
                if (selectedPrefabs.Contains(prefab))
                    selectedPrefabs.Remove(prefab);
                else
                    selectedPrefabs.Add(prefab);
            }

            col++;
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();
        GUILayout.Label("選択中のプレハブ:");
        foreach (var p in selectedPrefabs)
        {
            GUILayout.Label(p.name);
        }
    }

    private void LoadPrefabsFromFolders()
    {
        prefabList.Clear();
        selectedPrefabs.Clear();

        foreach (string folderPath in folderPaths)
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null && !prefabList.Contains(prefab))
                    prefabList.Add(prefab);
            }
        }
    }

    [InitializeOnLoadMethod]
    private static void RegisterSceneClick()
    {
        SceneView.duringSceneGui += (sceneView) =>
        {
            if (Event.current.type == EventType.MouseDown && Event.current.button == 1)
            {
                var window = GetWindow<FolderPrefabPlacer>();
                if (window.selectedPrefabs == null || window.selectedPrefabs.Count == 0) return;

                Vector2 mousePos = Event.current.mousePosition;
                Ray ray = HandleUtility.GUIPointToWorldRay(mousePos);

                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    float offset = 0f;
                    foreach (var prefab in window.selectedPrefabs)
                    {
                        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                        Vector3 position = hit.point + new Vector3(offset, 0, 0);
                        instance.transform.position = position;
                        Undo.RegisterCreatedObjectUndo(instance, "Place Prefab");
                        offset += 2.0f;
                    }
                }

                Event.current.Use();
            }
        };
    }
}