using UnityEditor;
using UnityEngine;

// �K�� Editor �t�H���_���ɔz�u���Ă�������
public class Tilemap3DEditor : EditorWindow
{
    // === �����o�ϐ��i�N���X�̃t�B�[���h�Ƃ��Ē�`�j===
    private float tileSize = 1.0f;
    private GameObject tilePrefab;
    private Transform tileParent;

    // �E�B���h�E���j���[�ɒǉ�
    [MenuItem("Tools/3D Tilemap Editor")]
    public static void ShowWindow()
    {
        // �����̃E�B���h�E���擾�܂��͐V�K�쐬
        GetWindow<Tilemap3DEditor>("3D Tilemap");
    }

    private void OnGUI()
    {
        // �^�C���ݒ�
        GUILayout.Label("Tile Settings", EditorStyles.boldLabel);
        // �ϐ� tileSize, tilePrefab, tileParent �͂����ŃA�N�Z�X�\
        tileSize = EditorGUILayout.FloatField("Tile Size", tileSize);
        tilePrefab = (GameObject)EditorGUILayout.ObjectField("Tile Prefab", tilePrefab, typeof(GameObject), false);
        tileParent = (Transform)EditorGUILayout.ObjectField("Parent Transform", tileParent, typeof(Transform), true);

        // �V�[���r���[�̍ĕ`������� (�ݒ�ύX���ɃO���b�h�\�����X�V)
        if (GUI.changed)
        {
            SceneView.RepaintAll();
        }
    }

    // �V�[���r���[�̃C�x���g���������邽�߂ɓo�^
    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    // �V�[���r���[�̃C�x���g�̓o�^������
    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    // === Scene�r���[�̃C�x���g�����iOnSceneGUI�̎����j===
    private void OnSceneGUI(SceneView sceneView)
    {
        Event guiEvent = Event.current;

        // 1. �O���b�h�̕`�� (�ϐ� tileSize �������ŃA�N�Z�X�\)
        DrawGrid(10, tileSize);

        // 2. �}�E�X���͂̏���
        if (tilePrefab != null && tileParent != null) // �ϐ� tilePrefab �� tileParent �������ŃA�N�Z�X�\
        {
            // Raycast�Œn�ʁiXZ���ʁj��̃O���b�h�ʒu�����
            Ray ray = HandleUtility.GUIPointToWorldRay(guiEvent.mousePosition);
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

            if (groundPlane.Raycast(ray, out float distance))
            {
                Vector3 worldPos = ray.GetPoint(distance);

                // �O���b�h�Z�����W�ɃX�i�b�v (�ϐ� tileSize �������ŃA�N�Z�X�\)
                Vector3 gridPos = SnapToGrid(worldPos, tileSize);

                // 3. �z�u�v���r���[�̕`�� (�ϐ� tileSize �������ŃA�N�Z�X�\)
                DrawPlacementPreview(gridPos, tileSize);

                // 4. �}�E�X�{�^���ł̔z�u/�폜
                if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0) // ���N���b�N
                {
                    PlaceTile(gridPos);
                    guiEvent.Use(); // �C�x���g�������Unity�̕W�������h��
                }
            }
        }

        // �V�[���r���[�̑���i�J�����ړ��Ȃǁj���p���ł���悤�ɂ���
        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
    }

    // === �w���p�[���\�b�h�i�N���X�̃����o�֐��Ƃ��Ē�`�j===

    // ���[���h���W���O���b�h�ɃX�i�b�v
    private Vector3 SnapToGrid(Vector3 worldPos, float size)
    {
        float x = Mathf.Round(worldPos.x / size) * size;
        float y = 0;
        float z = Mathf.Round(worldPos.z / size) * size;
        return new Vector3(x, y, z);
    }

    // �O���b�h��`��
    private void DrawGrid(int range, float size)
    {
        Handles.color = new Color(1f, 1f, 1f, 0.2f); // ������

        // X���̃��C��
        for (int i = -range; i <= range; i++)
        {
            Vector3 start = new Vector3(i * size, 0, -range * size);
            Vector3 end = new Vector3(i * size, 0, range * size);
            Handles.DrawLine(start, end);
        }

        // Z���̃��C��
        for (int i = -range; i <= range; i++)
        {
            Vector3 start = new Vector3(-range * size, 0, i * size);
            Vector3 end = new Vector3(range * size, 0, i * size);
            Handles.DrawLine(start, end);
        }
    }

    // �z�u����^�C�����n�C���C�g�\��
    private void DrawPlacementPreview(Vector3 gridPos, float size)
    {
        Handles.color = new Color(0.2f, 0.8f, 0.2f, 0.5f); // �������̗�

        Vector3 center = gridPos + Vector3.up * size * 0.5f;
        Handles.CubeHandleCap(0, center, Quaternion.identity, size, EventType.Repaint);
    }

    // ���ۂɃ^�C����z�u
    private void PlaceTile(Vector3 gridPos)
    {
        // UNDO/REDO �̂��߂ɕK�{ (tilePrefab �� tileParent �������ŃA�N�Z�X�\)
        if (tilePrefab == null || tileParent == null) return;

        // PrefabUtility.InstantiatePrefab ���g�p����Prefab����C���X�^���X��
        GameObject newTile = PrefabUtility.InstantiatePrefab(tilePrefab, tileParent) as GameObject;

        if (newTile != null)
        {
            // �쐬��Undo�\�ɂ���
            Undo.RegisterCreatedObjectUndo(newTile, "Place 3D Tile");

            newTile.transform.position = gridPos;
            // �g�����X�t�H�[���ύX��Undo�\�ɂ���
            Undo.RegisterCompleteObjectUndo(newTile.transform, "Move 3D Tile");
        }
    }
}