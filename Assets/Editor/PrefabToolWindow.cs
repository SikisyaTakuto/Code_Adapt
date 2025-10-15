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
    // 4. カスタム回転入力用
    private Vector3 customRotation = Vector3.zero;
    // 5. カスタムスケール入力用
    private Vector3 customScale = Vector3.one;

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

        //スクロールビューの開始
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        GUILayout.Label("プレハブの配置と削除", EditorStyles.boldLabel);

        //プレハブリスト管理セクション
        DrawPrefabListManagement();

        //プレハブサムネイル表示セクション
        //ここでGridを描画し、イベント処理も行います
        int oldSelectedIndex = selectedPrefabIndex;
        DrawPrefabSelectionGrid();

        //プレハブリストの削除処理（イベントドリブン）
        HandlePrefabDeletionEvent(oldSelectedIndex);

        //選択されたプレハブの配置
        GUILayout.Space(10);
        DrawPlacementControls();

        //選択オブジェクト削除
        GUILayout.Space(20);
        DrawDeletionControls();

        //スクロールビューの終了
        EditorGUILayout.EndScrollView();
    }

    // プレハブの削除イベント処理
    void HandlePrefabDeletionEvent(int previousIndex)
    {
        // 現在のイベントを取得
        Event currentEvent = Event.current;

        // プレハブ選択グリッドを描画した領域（Rect）を取得します。
        // SelectionGridの描画後に `GUILayoutUtility.GetLastRect()` を呼ぶことで、
        // 直前の描画領域を取得できますが、Grid内の個々のボタンの領域が必要です。
        // SelectionGridは内部でボタンを処理するため、より一般的な方法として
        // Deleteキーと右クリックをチェックします。

        // 1. Deleteキーによる削除
        // 選択インデックスが有効で、かつDeleteキーが押された場合
        if (selectedPrefabIndex != -1 &&
            currentEvent.type == EventType.KeyDown &&
            (currentEvent.keyCode == KeyCode.Delete || currentEvent.keyCode == KeyCode.Backspace))
        {
            // Grid上で何かを選択した直後で、DeleteキーがGridのフォーカス内にあると仮定して処理します。
            DeletePrefabFromList(selectedPrefabIndex);
            currentEvent.Use(); // イベントを消費し、他のコンポーネントが処理しないようにする
            return;
        }

        // 2. 右クリックによる選択
        // クリックイベントで、右ボタン (1) が押された場合
        if (currentEvent.type == EventType.MouseDown && currentEvent.button == 1)
        {
            // マウス位置がGrid領域内にあるかを確認する必要がありますが、
            // SelectionGridはクリックしたインデックスを自動でselectedPrefabIndexにセットします。
            // SelectionGridの**直後**でマウスダウンイベントを捕捉し、
            // selectedPrefabIndexが変化した（つまりクリックされた）か確認します。

            if (selectedPrefabIndex != previousIndex)
            {
                // 右クリックで新しいプレハブが選択された場合
                currentEvent.Use(); // イベントを消費し、GUIのコンテキストメニューが出ないようにする
            }
        }

        // 3. コンテキストメニュー (右クリック削除) の提供
        // 右クリックで新しいプレハブが選択された、あるいは以前から選択されている状態で、
        // かつ現在のイベントがコンテキストメニュー要求（マウスの右ボタンクリックなど）の場合
        if (selectedPrefabIndex != -1 && currentEvent.type == EventType.ContextClick)
        {
            // コンテキストメニューを作成
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("リストから削除"), false, () => DeletePrefabFromList(selectedPrefabIndex));
            menu.ShowAsContext();

            currentEvent.Use(); // イベントを消費
        }
    }


    // プレハブリストから削除するヘルパーメソッド
    void DeletePrefabFromList(int index)
    {
        if (index < 0 || index >= prefabList.Count) return;

        // Undo機能に登録 (リストの状態変更をUndoできるように)
        Undo.RecordObject(this, "プレハブをリストから削除: " + prefabList[index].name);

        prefabList.RemoveAt(index);
        selectedPrefabIndex = -1; // 選択を解除

        // GUIを再描画
        Repaint();
    }


    void DrawPrefabListManagement()
    {
        GUILayout.Label("プレハブリスト", EditorStyles.miniBoldLabel);
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

    // プレハブ選択グリッドの描画
    void DrawPrefabSelectionGrid()
    {
        if (prefabList.Count == 0)
        {
            EditorGUILayout.HelpBox("プレハブリストにオブジェクトを追加してください。", MessageType.Info);
            return;
        }
        GUILayout.Label("プレハブを選択 (サムネイルをクリック)", EditorStyles.miniBoldLabel);

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

    // プレハブ配置コントロールの描画と処理
    void DrawPlacementControls()
    {
        GameObject prefabToPlace = (selectedPrefabIndex >= 0 && selectedPrefabIndex < prefabList.Count)
            ? prefabList[selectedPrefabIndex]
            : null;

        EditorGUI.BeginDisabledGroup(prefabToPlace == null);

        GUILayout.Label("Placement Settings", EditorStyles.boldLabel);

        //1. 配置オフセット入力フィールド
        GUILayout.Label("配置オフセット (隣接配置時の間隔):", EditorStyles.miniBoldLabel);
        placementOffset = EditorGUILayout.Vector3Field("", placementOffset);

        // 任意の回転入力フィールド
        GUILayout.Space(5);
        GUILayout.Label("回転 (オイラー角 XYZ):", EditorStyles.miniBoldLabel);
        customRotation = EditorGUILayout.Vector3Field("", customRotation);

        // 任意のスケール入力フィールド
        GUILayout.Space(5);
        GUILayout.Label("スケール (ローカルスケール):", EditorStyles.miniBoldLabel);
        customScale = EditorGUILayout.Vector3Field("", customScale);

        // スケールを負の値にしないための簡単なバリデーション
        customScale.x = Mathf.Max(0.001f, customScale.x);
        customScale.y = Mathf.Max(0.001f, customScale.y);
        customScale.z = Mathf.Max(0.001f, customScale.z);


        //2. 任意の座標配置
        GUILayout.Space(10);
        GUILayout.Label("1. 任意座標への配置:", EditorStyles.miniBoldLabel);
        customPosition = EditorGUILayout.Vector3Field("目標座標", customPosition);

        if (GUILayout.Button("指定座標に配置"))
        {
            PlacePrefab(prefabToPlace, customPosition);
        }

        //3. 隣接配置
        GUILayout.Space(10);
        GUILayout.Label("2. 隣接配置:", EditorStyles.miniBoldLabel);

        GameObject selectedObj = Selection.activeGameObject;
        if (selectedObj == null)
        {
            EditorGUILayout.HelpBox("Hierarchyで既存のオブジェクトを選択してください。", MessageType.Info);
        }
        else
        {
            EditorGUILayout.ObjectField("基準オブジェクト:", selectedObj, typeof(GameObject), true);

            // 隣接配置ボタン（X/Y/Zの+/−方向）をグリッドで配置
            DrawAdjacentButtons(prefabToPlace, selectedObj.transform.position);
        }

        EditorGUI.EndDisabledGroup();
    }

    // 隣接配置ボタンの描画
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

    // 削除ボタンの描画と処理
    void DrawDeletionControls()
    {
        GUILayout.Label("選択オブジェクトの削除", EditorStyles.miniBoldLabel);

        if (GUILayout.Button("選択中のゲームオブジェクトを削除"))
        {
            DeleteSelectedObjects();
        }
    }

    // プレハブ配置処理
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

        // Undo機能に登録
        Undo.RegisterCreatedObjectUndo(newObject, "プレハブを配置:" + prefab.name);

        // 配置場所を設定
        newObject.transform.position = position;

        // 回転を設定
        newObject.transform.rotation = Quaternion.Euler(customRotation);

        // スケールを設定
        newObject.transform.localScale = customScale;

        // シーンビューで作成したオブジェクトを選択
        Selection.activeGameObject = newObject;
    }

    // 選択オブジェクト削除処理
    void DeleteSelectedObjects()
    {
        GameObject[] selectedObjects = Selection.gameObjects;

        if (selectedObjects.Length == 0)
        {
            Debug.LogWarning("削除するオブジェクトが選択されていません。");
            return;
        }

        Undo.SetCurrentGroupName("選択オブジェクトを削除");
        int groupIndex = Undo.GetCurrentGroup();

        foreach (GameObject obj in selectedObjects)
        {
            Undo.DestroyObjectImmediate(obj);
        }

        Undo.CollapseUndoOperations(groupIndex);
    }
}