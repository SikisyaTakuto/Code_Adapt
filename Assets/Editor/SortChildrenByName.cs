using UnityEditor;
using UnityEngine;
using System.Linq; // OrderByを使用するために必要

public class SortChildrenByName : EditorWindow
{
    private GameObject parentObject;

    [MenuItem("Tools/Sort Children By Name")]
    public static void ShowWindow()
    {
        GetWindow<SortChildrenByName>("Sort Children By Name");
    }

    private void OnGUI()
    {
        GUILayout.Label("親オブジェクトの子を名前順にソート", EditorStyles.boldLabel);

        parentObject = (GameObject)EditorGUILayout.ObjectField("親オブジェクト", parentObject, typeof(GameObject), true);

        if (parentObject == null)
        {
            EditorGUILayout.HelpBox("ソートしたい親オブジェクトをここにドラッグ＆ドロップしてください。", MessageType.Info);
            return;
        }

        if (GUILayout.Button("子オブジェクトを名前順にソート"))
        {
            SortChildren(parentObject);
        }
    }

    private static void SortChildren(GameObject parent)
    {
        if (parent == null)
        {
            Debug.LogWarning("親オブジェクトが指定されていません。");
            return;
        }

        // 子オブジェクトのリストを取得
        // GetComponentsInChildren(true)を使うと非アクティブな子オブジェクトも取得できるが、
        // 今回は直接の子のみを対象とするため、子オブジェクトのTransformをループで取得
        var children = new Transform[parent.transform.childCount];
        for (int i = 0; i < parent.transform.childCount; i++)
        {
            children[i] = parent.transform.GetChild(i);
        }

        // 名前でソート
        var sortedChildren = children.OrderBy(t => t.name).ToArray();

        // ソートされた順序で子オブジェクトを再配置
        // Undo.RecordObjectで変更を記録し、Undo/Redoできるようにする
        Undo.RecordObject(parent.transform, "Sort Children By Name");
        for (int i = 0; i < sortedChildren.Length; i++)
        {
            sortedChildren[i].SetSiblingIndex(i);
        }

        Debug.Log($"{parent.name} の子オブジェクトが名前順にソートされました。");
    }
}