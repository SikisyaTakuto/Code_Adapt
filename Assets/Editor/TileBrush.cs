using System.Collections.Generic;
using UnityEngine;

// マップ編集ツールに使用されるコンポーネント
public class TileBrush : MonoBehaviour
{
    // 配置可能なプレハブのリスト（複数選択に対応）
    public List<GameObject> tilePrefabs = new List<GameObject>();

    // 現在選択中のプレハブのインデックス
    public int selectedPrefabIndex = 0;

    // グリッドのサイズ（X: 横方向, Y: 奥方向）
    public Vector2Int gridSize = new Vector2Int(10, 10);

    // 高さ（Y軸）方向の最大ブロック数
    public int maxHeight = 5;

    // ブロック間の距離（間隔）
    public float tileSpacing = 1f;

    // 消しゴムモードの有効/無効
    public bool eraserMode = false;
}

// タイルの位置と使用したプレハブを記録するためのデータ構造
[System.Serializable]
public class PlacedTileData
{
    public Vector3 position;   // ワールド座標上の位置
    public int prefabIndex;   // tilePrefabsリスト内のインデックス
}
