using System.Collections.Generic;
using UnityEngine;

// �}�b�v�ҏW�c�[���Ɏg�p�����R���|�[�l���g
public class TileBrush : MonoBehaviour
{
    // �z�u�\�ȃv���n�u�̃��X�g�i�����I���ɑΉ��j
    public List<GameObject> tilePrefabs = new List<GameObject>();

    // ���ݑI�𒆂̃v���n�u�̃C���f�b�N�X
    public int selectedPrefabIndex = 0;

    // �O���b�h�̃T�C�Y�iX: ������, Y: �������j
    public Vector2Int gridSize = new Vector2Int(10, 10);

    // �����iY���j�����̍ő�u���b�N��
    public int maxHeight = 5;

    // �u���b�N�Ԃ̋����i�Ԋu�j
    public float tileSpacing = 1f;

    // �����S�����[�h�̗L��/����
    public bool eraserMode = false;
}

// �^�C���̈ʒu�Ǝg�p�����v���n�u���L�^���邽�߂̃f�[�^�\��
[System.Serializable]
public class PlacedTileData
{
    public Vector3 position;   // ���[���h���W��̈ʒu
    public int prefabIndex;   // tilePrefabs���X�g���̃C���f�b�N�X
}
