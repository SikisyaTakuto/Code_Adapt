using UnityEngine;
using UnityEditor;
using System.Linq;
using System.IO;
using System.Collections.Generic;

// TileBrush �R���|�[�l���g�ɑ΂���J�X�^���G�f�B�^
[CustomEditor(typeof(TileBrush))]
public class TileBrushEditor : Editor
{
    private TileBrush brush;

    void OnEnable()
    {
        // �Ώ�TileBrush�擾 & SceneGUI�C�x���g�o�^
        brush = (TileBrush)target;
        SceneView.duringSceneGui += OnSceneGUI;
    }

    void OnDisable()
    {
        // �C�x���g����
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    // Scene�r���[��ł̃}�E�X����ɂ��z�u/�폜����
    void OnSceneGUI(SceneView sceneView)
    {
        // === �O���b�h�`�� ===
        Handles.color = new Color(0f, 1f, 0f, 0.2f); // �΁E����
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

        // ���N���b�N����Alt�L�[�������Ă��Ȃ��ꍇ�̂ݏ���
        if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
        {
            Debug.Log("���N���b�N���o");
            // �}�E�X�ʒu���烌�C���΂�
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
            {
                Debug.Log("Raycast�q�b�g: " + hit.point);

                Vector3 point = hit.point;

                // �O���b�h�C���f�b�N�X���Z�o�i�O���b�h�X�i�b�v�j
                Vector3 localPos = point - brush.transform.position;
                int x = Mathf.RoundToInt(localPos.x / brush.tileSpacing);
                int z = Mathf.RoundToInt(localPos.z / brush.tileSpacing);
                int y = Mathf.RoundToInt(localPos.y / brush.tileSpacing);

                Debug.Log($"Raw hit point: {point}");
                Debug.Log($"Brush origin: {brush.transform.position}");
                Debug.Log($"Calculated grid index: x={x}, y={y}, z={z}");

                // �͈͓�������
                if (x >= 0 && x < brush.gridSize.x &&
                    z >= 0 && z < brush.gridSize.y &&
                    y >= 0 && y < brush.maxHeight)
                {
                    // ���ۂ̔z�u�ʒu���Z�o
                    Vector3 gridPos = brush.transform.position + new Vector3(x, y, z) * brush.tileSpacing;

                    if (brush.eraserMode)
                    {
                        // �����S�����[�h���F�Y���ʒu�̃I�u�W�F�N�g���������č폜
                        var toDelete = brush.transform.Cast<Transform>().FirstOrDefault(t => Vector3.Distance(t.position, gridPos) < 0.1f);
                        if (toDelete != null)
                        {
                            Undo.DestroyObjectImmediate(toDelete.gameObject);
                        }
                    }
                    else
                    {
                        // �ʏ탂�[�h�F�v���n�u��z�u
                        if (brush.tilePrefabs.Count == 0) return;

                        GameObject prefab = brush.tilePrefabs[brush.selectedPrefabIndex];
                        if (prefab == null)
                        {
                            Debug.LogWarning("�I�����ꂽ�v���n�u�� null �ł�");
                            return;
                        }

                        GameObject tile = (GameObject)PrefabUtility.InstantiatePrefab(prefab, brush.transform);
                        if (tile == null)
                        {
                            Debug.LogError("PrefabUtility.InstantiatePrefab �Ɏ��s���܂���");
                            return;
                        }

                        tile.transform.position = gridPos;
                        Undo.RegisterCreatedObjectUndo(tile, "Place Tile");

                        Debug.Log($"�v���n�u {prefab.name} �� {gridPos} �ɔz�u���܂���");
                    }
                }

                // �C�x���g������i���̑�����u���b�N�j
                e.Use();
            }
        }
    }

    public override void OnInspectorGUI()
    {
        // �f�t�H���g�̃C���X�y�N�^�[�`��
        DrawDefaultInspector();

        // �v���n�u�I��p�̃h���b�v�_�E��
        if (brush.tilePrefabs.Count > 0)
        {
            string[] names = brush.tilePrefabs.Select(p => p != null ? p.name : "None").ToArray();
            brush.selectedPrefabIndex = EditorGUILayout.Popup("�I�𒆃v���n�u", brush.selectedPrefabIndex, names);
        }

        // �����S�����[�h�̃g�O��
        brush.eraserMode = EditorGUILayout.Toggle("�����S�����[�h", brush.eraserMode);

        EditorGUILayout.Space();

        // �w���v�\��
        EditorGUILayout.HelpBox(
            "Scene�r���[��ŃN���b�N���ăv���n�u��z�u�B\n" +
            "�����S�����[�h�ō폜�\�B\n" +
            "Y�������i�����j�ɂ��z�u�\�B\n",
            MessageType.Info);
    }
}
