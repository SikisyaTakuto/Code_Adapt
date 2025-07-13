using UnityEditor;
using UnityEngine;
using System.Linq; // OrderBy���g�p���邽�߂ɕK�v

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
        GUILayout.Label("�e�I�u�W�F�N�g�̎q�𖼑O���Ƀ\�[�g", EditorStyles.boldLabel);

        parentObject = (GameObject)EditorGUILayout.ObjectField("�e�I�u�W�F�N�g", parentObject, typeof(GameObject), true);

        if (parentObject == null)
        {
            EditorGUILayout.HelpBox("�\�[�g�������e�I�u�W�F�N�g�������Ƀh���b�O���h���b�v���Ă��������B", MessageType.Info);
            return;
        }

        if (GUILayout.Button("�q�I�u�W�F�N�g�𖼑O���Ƀ\�[�g"))
        {
            SortChildren(parentObject);
        }
    }

    private static void SortChildren(GameObject parent)
    {
        if (parent == null)
        {
            Debug.LogWarning("�e�I�u�W�F�N�g���w�肳��Ă��܂���B");
            return;
        }

        // �q�I�u�W�F�N�g�̃��X�g���擾
        // GetComponentsInChildren(true)���g���Ɣ�A�N�e�B�u�Ȏq�I�u�W�F�N�g���擾�ł��邪�A
        // ����͒��ڂ̎q�݂̂�ΏۂƂ��邽�߁A�q�I�u�W�F�N�g��Transform�����[�v�Ŏ擾
        var children = new Transform[parent.transform.childCount];
        for (int i = 0; i < parent.transform.childCount; i++)
        {
            children[i] = parent.transform.GetChild(i);
        }

        // ���O�Ń\�[�g
        var sortedChildren = children.OrderBy(t => t.name).ToArray();

        // �\�[�g���ꂽ�����Ŏq�I�u�W�F�N�g���Ĕz�u
        // Undo.RecordObject�ŕύX���L�^���AUndo/Redo�ł���悤�ɂ���
        Undo.RecordObject(parent.transform, "Sort Children By Name");
        for (int i = 0; i < sortedChildren.Length; i++)
        {
            sortedChildren[i].SetSiblingIndex(i);
        }

        Debug.Log($"{parent.name} �̎q�I�u�W�F�N�g�����O���Ƀ\�[�g����܂����B");
    }
}