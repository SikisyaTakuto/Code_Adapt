using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// エディタウィンドウを作成
public class PrefabToolWindow : EditorWindow
{
    // 配置したいプレハブを複数格納するリスト
    public List<GameObject> prefabList = new List<GameObject>();

    // 現在選択されているプレハブのインデックス
    private int selectedPrefabIndex = -1;

    // --- 新しい変数 ---
    // 1. 任意の座標入力用
    private Vector3 customPosition = Vector3.zero;
    // 2. 配置時の微調整用オフセット
    private Vector3 placementOffset = Vector3.one;
    // 3. スクロール位置を保持するための変数
    private Vector2 scrollPosition = Vector2.zero;
    // ------------------

    // サムネイルのサイズを大きく修正
    private const float PREVIEW_SIZE = 128f;

    // 画像表示用のカスタムスタイル
    private GUIStyle thumbnailStyle;

    // ウィンドウメニューに項目を追加
    [MenuItem("Tools/Prefab Utility Tool")]
    public static void ShowWindow()
    {
        // 既存のウィンドウがあれば表示し、なければ新規作成
        GetWindow<PrefabToolWindow>("Prefab Tool").Show();
    }

    // ウィンドウが開かれた時や有効になった時に一度だけ呼ばれる
    private void OnEnable()
    {
        // スタイルを初期化
        InitStyles();
    }

    // 画像表示用のスタイルを初期化
    private void InitStyles()
    {
        thumbnailStyle = new GUIStyle(GUI.skin.button);
        thumbnailStyle.imagePosition = ImagePosition.ImageAbove;
        thumbnailStyle.alignment = TextAnchor.LowerCenter;
        thumbnailStyle.fixedWidth = PREVIEW_SIZE;
        thumbnailStyle.fixedHeight = PREVIEW_SIZE + 25;
    }

    // ウィンドウのGUIを描画
    void OnGUI()
    {
        if (thumbnailStyle == null) InitStyles();

        // --- 修正箇所: スクロールビューの開始 ---
        // scrollPositionに現在のスクロール位置が格納される
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // 内部コンテンツの描画開始

        GUILayout.Label("Prefab Placement & Deletion", EditorStyles.boldLabel);

        // --- プレハブリスト管理セクション ---
        DrawPrefabListManagement();

        // --- プレハブサムネイル表示セクション ---
        DrawPrefabSelectionGrid();

        // --- 選択されたプレハブの配置 ---
        GUILayout.Space(10);
        DrawPlacementControls();

        // --- 選択オブジェクト削除 ---
        GUILayout.Space(20);
        DrawDeletionControls();

        // 内部コンテンツの描画終了
        // --- 修正箇所: スクロールビューの終了 ---
        EditorGUILayout.EndScrollView();
    }

    // (中略: DrawPrefabListManagement は変更なし)
    void DrawPrefabListManagement()
    {
        GUILayout.Label("Prefab List", EditorStyles.miniBoldLabel);
        SerializedObject serializedObject = new SerializedObject(this);
        SerializedProperty property = serializedObject.FindProperty("prefabList");

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(property, true);
        serializedObject.ApplyModifiedProperties();

        if (EditorGUI.EndChangeCheck())
        {
            selectedPrefabIndex = -1;
        }
    }

    // (中略: DrawPrefabSelectionGrid は変更なし)
    void DrawPrefabSelectionGrid()
    {
        if (prefabList.Count == 0)
        {
            EditorGUILayout.HelpBox("プレハブリストにオブジェクトを追加してください。", MessageType.Info);
            return;
        }
        GUILayout.Label("Select Prefab (Click Thumbnail)", EditorStyles.miniBoldLabel);
        GUIContent[] gridContents = new GUIContent[prefabList.Count];
        for (int i = 0; i < prefabList.Count; i++)
        {
            GameObject prefab = prefabList[i];
            Texture2D preview = AssetPreview.GetAssetPreview(prefab);
            string name = prefab != null ? prefab.name : "Missing";
            gridContents[i] = new GUIContent(name, preview, name);
        }

        int columns = Mathf.FloorToInt((this.position.width - 20) / thumbnailStyle.fixedWidth);
        if (columns < 1) columns = 1;

        // SelectionGridの縦幅を事前に確保
        float gridHeight = Mathf.CeilToInt((float)prefabList.Count / columns) * thumbnailStyle.fixedHeight;

        selectedPrefabIndex = GUILayout.SelectionGrid(
            selectedPrefabIndex,
            gridContents,
            columns,
            thumbnailStyle,
            GUILayout.Height(gridHeight)
        );

        if (selectedPrefabIndex >= 0 && selectedPrefabIndex < prefabList.Count)
        {
            GameObject selectedPrefab = prefabList[selectedPrefabIndex];
            if (selectedPrefab != null)
            {
                EditorGUILayout.HelpBox("選択中: " + selectedPrefab.name, MessageType.None);
            }
        }
    }

    // プレハブ配置コントロールの描画と処理 (変更なし)
    void DrawPlacementControls()
    {
        GameObject prefabToPlace = (selectedPrefabIndex >= 0 && selectedPrefabIndex < prefabList.Count)
            ? prefabList[selectedPrefabIndex]
            : null;

        EditorGUI.BeginDisabledGroup(prefabToPlace == null);

        GUILayout.Label("Placement Settings", EditorStyles.boldLabel);

        // --- 1. 配置オフセット入力フィールド ---
        GUILayout.Label("Placement Offset (隣接配置時の間隔):", EditorStyles.miniBoldLabel);
        placementOffset = EditorGUILayout.Vector3Field("", placementOffset);

        // --- 2. 任意の座標配置 ---
        GUILayout.Label("1. Custom Position Placement:", EditorStyles.miniBoldLabel);
        customPosition = EditorGUILayout.Vector3Field("Target Position", customPosition);

        if (GUILayout.Button("Place at Custom Position"))
        {
            PlacePrefab(prefabToPlace, customPosition);
        }

        // --- 3. 隣接配置 ---
        GUILayout.Space(10);
        GUILayout.Label("2. Adjacent Placement:", EditorStyles.miniBoldLabel);

        GameObject selectedObj = Selection.activeGameObject;
        if (selectedObj == null)
        {
            EditorGUILayout.HelpBox("Hierarchyで既存のオブジェクトを選択してください。", MessageType.Info);
        }
        else
        {
            EditorGUILayout.ObjectField("Base Object:", selectedObj, typeof(GameObject), true);

            // 隣接配置ボタン（X/Y/Zの+/−方向）をグリッドで配置
            DrawAdjacentButtons(prefabToPlace, selectedObj.transform.position);
        }

        EditorGUI.EndDisabledGroup();
    }

    // 隣接配置ボタンの描画 (変更なし)
    void DrawAdjacentButtons(GameObject prefabToPlace, Vector3 basePosition)
    {
        GUILayout.BeginVertical(EditorStyles.helpBox);

        // X軸 (+と−)
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("← X-"))
            PlacePrefab(prefabToPlace, basePosition - Vector3.right * placementOffset.x);
        GUILayout.Label("X-Axis", EditorStyles.boldLabel, GUILayout.Width(70));
        if (GUILayout.Button("X+ →"))
            PlacePrefab(prefabToPlace, basePosition + Vector3.right * placementOffset.x);
        GUILayout.EndHorizontal();

        // Y軸 (+と−)
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("↓ Y-"))
            PlacePrefab(prefabToPlace, basePosition - Vector3.up * placementOffset.y);
        GUILayout.Label("Y-Axis", EditorStyles.boldLabel, GUILayout.Width(70));
        if (GUILayout.Button("Y+ ↑"))
            PlacePrefab(prefabToPlace, basePosition + Vector3.up * placementOffset.y);
        GUILayout.EndHorizontal();

        // Z軸 (+と−)
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Bck Z-"))
            PlacePrefab(prefabToPlace, basePosition - Vector3.forward * placementOffset.z);
        GUILayout.Label("Z-Axis", EditorStyles.boldLabel, GUILayout.Width(70));
        if (GUILayout.Button("Z+ Fwd"))
            PlacePrefab(prefabToPlace, basePosition + Vector3.forward * placementOffset.z);
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
    }

    // 削除ボタンの描画と処理 (変更なし)
    void DrawDeletionControls()
    {
        GUILayout.Label("Selected Object Deletion", EditorStyles.miniBoldLabel);

        if (GUILayout.Button("Delete Selected GameObjects"))
        {
            DeleteSelectedObjects();
        }
    }

    // プレハブ配置処理 (変更なし)
    void PlacePrefab(GameObject prefab, Vector3 position)
    {
        if (prefab == null) return;

        if (EditorSceneManager.GetActiveScene().rootCount == 0 && EditorSceneManager.GetActiveScene().name == "")
        {
            Debug.LogError("アクティブなシーンが存在しないか、保存されていません。");
            return;
        }

        // プレハブをシーンにインスタンス化
        GameObject newObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab);

        // 配置場所を設定
        newObject.transform.position = position;

        // Undo機能に登録
        Undo.RegisterCreatedObjectUndo(newObject, "Place Prefab: " + prefab.name);

        // シーンビューで作成したオブジェクトを選択
        Selection.activeGameObject = newObject;
    }

    // 選択オブジェクト削除処理 (変更なし)
    void DeleteSelectedObjects()
    {
        GameObject[] selectedObjects = Selection.gameObjects;

        if (selectedObjects.Length == 0)
        {
            Debug.LogWarning("削除するオブジェクトが選択されていません。");
            return;
        }

        Undo.SetCurrentGroupName("Delete Selected Objects");
        int groupIndex = Undo.GetCurrentGroup();

        foreach (GameObject obj in selectedObjects)
        {
            Undo.DestroyObjectImmediate(obj);
        }

        Undo.CollapseUndoOperations(groupIndex);
    }
}