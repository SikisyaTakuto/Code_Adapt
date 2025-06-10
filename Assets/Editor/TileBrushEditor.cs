using UnityEngine;
using UnityEditor;
using System.Linq;
using System.IO;
using System.Collections.Generic;

// TileBrush コンポーネントに対するカスタムエディタ
[CustomEditor(typeof(TileBrush))]
public class TileBrushEditor : Editor
{
    private TileBrush brush;

    void OnEnable()
    {
        // 対象TileBrush取得 & SceneGUIイベント登録
        brush = (TileBrush)target;
        SceneView.duringSceneGui += OnSceneGUI;
    }

    void OnDisable()
    {
        // イベント解除
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    // Sceneビュー上でのマウス操作による配置/削除処理
    void OnSceneGUI(SceneView sceneView)
    {
        // === グリッド描画 ===
        Handles.color = new Color(0f, 1f, 0f, 0.2f); // 緑・透明
        for (int x = 0; x < brush.gridSize.x; x++)
        {
            for (int z = 0; z < brush.gridSize.y; z++)
            {
                for (int y = 0; y < brush.maxHeight; y++)
                {
                    Vector3 pos = brush.transform.position + new Vector3(x, y, z) * brush.tileSpacing;
                    Handles.DrawWireCube(pos, Vector3.one * brush.tileSpacing * 0.95f);
                }
            }
        }

        Event e = Event.current;

        // 左クリックかつAltキーを押していない場合のみ処理
        if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
        {
            Debug.Log("左クリック検出");
            // マウス位置からレイを飛ばす
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
            {
                Debug.Log("Raycastヒット: " + hit.point);

                Vector3 point = hit.point;

                // グリッドインデックスを算出（グリッドスナップ）
                Vector3 localPos = point - brush.transform.position;
                int x = Mathf.RoundToInt(localPos.x / brush.tileSpacing);
                int z = Mathf.RoundToInt(localPos.z / brush.tileSpacing);
                int y = Mathf.RoundToInt(localPos.y / brush.tileSpacing);

                Debug.Log($"Raw hit point: {point}");
                Debug.Log($"Brush origin: {brush.transform.position}");
                Debug.Log($"Calculated grid index: x={x}, y={y}, z={z}");

                // 範囲内か判定
                if (x >= 0 && x < brush.gridSize.x &&
                    z >= 0 && z < brush.gridSize.y &&
                    y >= 0 && y < brush.maxHeight)
                {
                    // 実際の配置位置を算出
                    Vector3 gridPos = brush.transform.position + new Vector3(x, y, z) * brush.tileSpacing;

                    if (brush.eraserMode)
                    {
                        // 消しゴムモード時：該当位置のオブジェクトを検索して削除
                        var toDelete = brush.transform.Cast<Transform>().FirstOrDefault(t => Vector3.Distance(t.position, gridPos) < 0.1f);
                        if (toDelete != null)
                        {
                            Undo.DestroyObjectImmediate(toDelete.gameObject);
                        }
                    }
                    else
                    {
                        // 通常モード：プレハブを配置
                        if (brush.tilePrefabs.Count == 0) return;

                        GameObject prefab = brush.tilePrefabs[brush.selectedPrefabIndex];
                        if (prefab == null)
                        {
                            Debug.LogWarning("選択されたプレハブが null です");
                            return;
                        }

                        GameObject tile = (GameObject)PrefabUtility.InstantiatePrefab(prefab, brush.transform);
                        if (tile == null)
                        {
                            Debug.LogError("PrefabUtility.InstantiatePrefab に失敗しました");
                            return;
                        }

                        tile.transform.position = gridPos;
                        Undo.RegisterCreatedObjectUndo(tile, "Place Tile");

                        Debug.Log($"プレハブ {prefab.name} を {gridPos} に配置しました");
                    }
                }

                // イベントを消費（他の操作をブロック）
                e.Use();
            }
        }
    }

    public override void OnInspectorGUI()
    {
        // デフォルトのインスペクター描画
        DrawDefaultInspector();

        // プレハブ選択用のドロップダウン
        if (brush.tilePrefabs.Count > 0)
        {
            string[] names = brush.tilePrefabs.Select(p => p != null ? p.name : "None").ToArray();
            brush.selectedPrefabIndex = EditorGUILayout.Popup("選択中プレハブ", brush.selectedPrefabIndex, names);
        }

        // 消しゴムモードのトグル
        brush.eraserMode = EditorGUILayout.Toggle("消しゴムモード", brush.eraserMode);

        EditorGUILayout.Space();

        // ヘルプ表示
        EditorGUILayout.HelpBox(
            "Sceneビュー上でクリックしてプレハブを配置。\n" +
            "消しゴムモードで削除可能。\n" +
            "Y軸方向（高さ）にも配置可能。\n",
            MessageType.Info);
    }
}
