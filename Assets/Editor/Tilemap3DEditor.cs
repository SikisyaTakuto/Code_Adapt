using UnityEditor;
using UnityEngine;

// 必ず Editor フォルダ内に配置してください
public class Tilemap3DEditor : EditorWindow
{
    // === メンバ変数（クラスのフィールドとして定義）===
    private float tileSize = 1.0f;
    private GameObject tilePrefab;
    private Transform tileParent;

    // ウィンドウメニューに追加
    [MenuItem("Tools/3D Tilemap Editor")]
    public static void ShowWindow()
    {
        // 既存のウィンドウを取得または新規作成
        GetWindow<Tilemap3DEditor>("3D Tilemap");
    }

    private void OnGUI()
    {
        // タイル設定
        GUILayout.Label("Tile Settings", EditorStyles.boldLabel);
        // 変数 tileSize, tilePrefab, tileParent はここでアクセス可能
        tileSize = EditorGUILayout.FloatField("Tile Size", tileSize);
        tilePrefab = (GameObject)EditorGUILayout.ObjectField("Tile Prefab", tilePrefab, typeof(GameObject), false);
        tileParent = (Transform)EditorGUILayout.ObjectField("Parent Transform", tileParent, typeof(Transform), true);

        // シーンビューの再描画を強制 (設定変更時にグリッド表示を更新)
        if (GUI.changed)
        {
            SceneView.RepaintAll();
        }
    }

    // シーンビューのイベントを処理するために登録
    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    // シーンビューのイベントの登録を解除
    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    // === Sceneビューのイベント処理（OnSceneGUIの実装）===
    private void OnSceneGUI(SceneView sceneView)
    {
        Event guiEvent = Event.current;

        // 1. グリッドの描画 (変数 tileSize がここでアクセス可能)
        DrawGrid(10, tileSize);

        // 2. マウス入力の処理
        if (tilePrefab != null && tileParent != null) // 変数 tilePrefab と tileParent がここでアクセス可能
        {
            // Raycastで地面（XZ平面）上のグリッド位置を特定
            Ray ray = HandleUtility.GUIPointToWorldRay(guiEvent.mousePosition);
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

            if (groundPlane.Raycast(ray, out float distance))
            {
                Vector3 worldPos = ray.GetPoint(distance);

                // グリッドセル座標にスナップ (変数 tileSize がここでアクセス可能)
                Vector3 gridPos = SnapToGrid(worldPos, tileSize);

                // 3. 配置プレビューの描画 (変数 tileSize がここでアクセス可能)
                DrawPlacementPreview(gridPos, tileSize);

                // 4. マウスボタンでの配置/削除
                if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0) // 左クリック
                {
                    PlaceTile(gridPos);
                    guiEvent.Use(); // イベントを消費してUnityの標準操作を防ぐ
                }
            }
        }

        // シーンビューの操作（カメラ移動など）を継続できるようにする
        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
    }

    // === ヘルパーメソッド（クラスのメンバ関数として定義）===

    // ワールド座標をグリッドにスナップ
    private Vector3 SnapToGrid(Vector3 worldPos, float size)
    {
        float x = Mathf.Round(worldPos.x / size) * size;
        float y = 0;
        float z = Mathf.Round(worldPos.z / size) * size;
        return new Vector3(x, y, z);
    }

    // グリッドを描画
    private void DrawGrid(int range, float size)
    {
        Handles.color = new Color(1f, 1f, 1f, 0.2f); // 薄い白

        // X軸のライン
        for (int i = -range; i <= range; i++)
        {
            Vector3 start = new Vector3(i * size, 0, -range * size);
            Vector3 end = new Vector3(i * size, 0, range * size);
            Handles.DrawLine(start, end);
        }

        // Z軸のライン
        for (int i = -range; i <= range; i++)
        {
            Vector3 start = new Vector3(-range * size, 0, i * size);
            Vector3 end = new Vector3(range * size, 0, i * size);
            Handles.DrawLine(start, end);
        }
    }

    // 配置するタイルをハイライト表示
    private void DrawPlacementPreview(Vector3 gridPos, float size)
    {
        Handles.color = new Color(0.2f, 0.8f, 0.2f, 0.5f); // 半透明の緑

        Vector3 center = gridPos + Vector3.up * size * 0.5f;
        Handles.CubeHandleCap(0, center, Quaternion.identity, size, EventType.Repaint);
    }

    // 実際にタイルを配置
    private void PlaceTile(Vector3 gridPos)
    {
        // UNDO/REDO のために必須 (tilePrefab と tileParent がここでアクセス可能)
        if (tilePrefab == null || tileParent == null) return;

        // PrefabUtility.InstantiatePrefab を使用してPrefabからインスタンス化
        GameObject newTile = PrefabUtility.InstantiatePrefab(tilePrefab, tileParent) as GameObject;

        if (newTile != null)
        {
            // 作成をUndo可能にする
            Undo.RegisterCreatedObjectUndo(newTile, "Place 3D Tile");

            newTile.transform.position = gridPos;
            // トランスフォーム変更をUndo可能にする
            Undo.RegisterCompleteObjectUndo(newTile.transform, "Move 3D Tile");
        }
    }
}