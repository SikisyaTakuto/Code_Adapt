using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// �w�肵���R���|�[�l���g������GameObject���q�G�����L�[���猟������G�f�B�^�E�B���h�E
/// </summary>
public class ComponentFinderWindow : EditorWindow
{
    // ��������R���|�[�l���g���i�f�t�H���g�� "BoxCollider"�j
    private string componentTypeName = "BoxCollider";

    // �������ʕ\���p�X�N���[���ʒu
    private Vector2 scrollPos;

    // �������ʂƂ��Č�������GameObject�̃��X�g
    private List<GameObject> results = new List<GameObject>();

    // �G�f�B�^���j���[�ɁuTools/Component Finder�v���ڂ�ǉ�
    [MenuItem("Tools/Component Finder")]
    public static void ShowWindow()
    {
        // �E�B���h�E���J���i�^�C�g���� "Component Finder"�j
        GetWindow<ComponentFinderWindow>("Component Finder");
    }

    // �G�f�B�^�E�B���h�E��GUI�`�揈��
    void OnGUI()
    {
        // �^�C�g�����x��
        GUILayout.Label("Search for Component in Hierarchy", EditorStyles.boldLabel);

        // �R���|�[�l���g�����̓t�B�[���h
        componentTypeName = EditorGUILayout.TextField("Component Type", componentTypeName);

        // �����{�^��
        if (GUILayout.Button("Search"))
        {
            FindComponentsInScene(componentTypeName);
        }

        // ���ʂ�����ꍇ�ɕ\��
        if (results.Count > 0)
        {
            GUILayout.Label($"Found {results.Count} objects", EditorStyles.boldLabel);

            // �������ʂ��X�N���[���\��
            scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Height(300));

            foreach (var obj in results)
            {
                // �I�u�W�F�N�g���̃{�^����\�����A�N���b�N�őI����Ping�\��
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
    /// �w�肳�ꂽ�R���|�[�l���g�������I�u�W�F�N�g���V�[�������猟������
    /// </summary>
    /// <param name="typeName">��������R���|�[�l���g�̌^���i��F"BoxCollider"�j</param>
    void FindComponentsInScene(string typeName)
    {
        // �O��̌������ʂ��N���A
        results.Clear();

        // �V�[�����̂��ׂĂ�GameObject���擾
        var allObjects = GameObject.FindObjectsOfType<GameObject>();

        foreach (var obj in allObjects)
        {
            // ��\���I�u�W�F�N�g�̓X�L�b�v
            if (obj.hideFlags != HideFlags.None)
                continue;

            // �w�薼�̃R���|�[�l���g�������Ă��邩�m�F
            var component = obj.GetComponent(typeName);
            if (component != null)
            {
                results.Add(obj);
            }
        }
    }
}