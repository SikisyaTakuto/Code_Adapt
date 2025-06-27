using UnityEngine;
using System.Collections.Generic;
using System.IO; // ファイルやテキストを扱うための名前空間

// 敵の情報を保持するためのデータクラス（CSVの1行 = 1体の敵）
[System.Serializable]
public class EnemyData
{
    public string Name; // 敵の名前
    public int HP;      // 敵の体力
    public int Attack;  // 敵の攻撃力
}

// プレハブと名前の対応を取る補助クラス
[System.Serializable]
public class EnemyPrefabEntry
{
    public string name;
    public GameObject prefab;
}

// 敵データをCSVから読み込んでリストに保存するクラス
public class EnemyDataLoader : MonoBehaviour
{
    // 読み込まれた敵データを格納するリスト（他のスクリプトからもアクセス可能）
    public List<EnemyData> enemyList = new List<EnemyData>();

    // 複数のプレハブをInspectorで設定
    public List<EnemyPrefabEntry> enemyPrefabEntries = new List<EnemyPrefabEntry>();

    // 内部的に名前→プレハブの辞書に変換して使用
    private Dictionary<string, GameObject> enemyPrefabDict = new Dictionary<string, GameObject>();

    // ゲーム開始時（またはこのスクリプトが有効化された時）に実行される
    void Start()
    {
        // プレハブエントリから辞書を作成（Name → Prefab）
        foreach (var entry in enemyPrefabEntries)
        {
            if (!enemyPrefabDict.ContainsKey(entry.name))
            {
                enemyPrefabDict.Add(entry.name, entry.prefab);
            }
        }


        LoadEnemyData(); // 敵データを読み込む関数を呼び出す

        // 敵をCSVのデータ分だけ生成し、初期化
        for (int i = 0; i < enemyList.Count; i++)
        {
            SpawnEnemy(enemyList[i], i);
        }
    }

    // Resourcesフォルダ内のCSVファイルから敵データを読み込む処理
    void LoadEnemyData()
    {
        // Resourcesフォルダ内の"EnemyData.csv"をTextAssetとして読み込む（拡張子は不要）
        TextAsset csvFile = Resources.Load<TextAsset>("EnemyData");

        // ファイルが見つからなかった場合はエラーメッセージを表示して処理を終了
        if (csvFile == null)
        {
            Debug.LogError("CSVファイルが見つかりません。Resources/EnemyData.csv を確認してください。");
            return;
        }

        // 読み込んだCSVテキストを1行ずつ処理できるようにする（StringReaderを使用）
        StringReader reader = new StringReader(csvFile.text);

        bool isFirstLine = true; // 最初の1行目（ヘッダー）をスキップするためのフラグ

        // 読み込む行がなくなるまで繰り返す
        while (reader.Peek() > -1)
        {
            // 1行分の文字列を読み込む
            string line = reader.ReadLine();

            // 1行目（ヘッダー）はデータとして扱わないのでスキップ
            if (isFirstLine)
            {
                isFirstLine = false;
                continue;
            }

            // カンマで文字列を分割して各列のデータに分ける
            // 例: "1,Goblin,100,10" → ["1", "Goblin", "100", "10"]
            string[] values = line.Split(',');

            // データの列数が期待通りかチェック（0:ID, 1:Name, 2:HP, 3:Attack）
            if (values.Length >= 4)
            {
                // 分割した値を使ってEnemyDataのインスタンスを作成し、リストに追加
                EnemyData data = new EnemyData
                {
                    Name = values[1],                  // 2列目：名前
                    HP = int.Parse(values[2]),         // 3列目：HP（文字列 → 整数に変換）
                    Attack = int.Parse(values[3])      // 4列目：攻撃力（文字列 → 整数に変換）
                };

                enemyList.Add(data); // リストに追加
            }
        }

        // 読み込んだ敵の数をデバッグ表示（確認用）
        Debug.Log("敵データを読み込みました。数：" + enemyList.Count);
    }

    void SpawnEnemy(EnemyData data, int index)
    {
        Vector3 spawnPos = new Vector3(index * 2f, 0, 0);

        // 敵名に対応するプレハブを取得
        if (!enemyPrefabDict.TryGetValue(data.Name, out GameObject prefab))
        {
            Debug.LogError($"プレハブが見つかりません：{data.Name}");
            return;
        }

        GameObject enemyGO = Instantiate(prefab, spawnPos, Quaternion.identity);

        Enemy enemyScript = enemyGO.GetComponent<Enemy>();
        if (enemyScript != null)
        {
            enemyScript.Initialize(data);
        }
        else
        {
            Debug.LogError("Enemy スクリプトがプレハブにアタッチされていません！");
        }
    }
}
