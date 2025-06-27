using UnityEngine;
using System.Collections.Generic;
using System.IO; // �t�@�C����e�L�X�g���������߂̖��O���

// �G�̏���ێ����邽�߂̃f�[�^�N���X�iCSV��1�s = 1�̂̓G�j
[System.Serializable]
public class EnemyData
{
    public string Name; // �G�̖��O
    public int HP;      // �G�̗̑�
    public int Attack;  // �G�̍U����
}

// �v���n�u�Ɩ��O�̑Ή������⏕�N���X
[System.Serializable]
public class EnemyPrefabEntry
{
    public string name;
    public GameObject prefab;
}

// �G�f�[�^��CSV����ǂݍ���Ń��X�g�ɕۑ�����N���X
public class EnemyDataLoader : MonoBehaviour
{
    // �ǂݍ��܂ꂽ�G�f�[�^���i�[���郊�X�g�i���̃X�N���v�g������A�N�Z�X�\�j
    public List<EnemyData> enemyList = new List<EnemyData>();

    // �����̃v���n�u��Inspector�Őݒ�
    public List<EnemyPrefabEntry> enemyPrefabEntries = new List<EnemyPrefabEntry>();

    // �����I�ɖ��O���v���n�u�̎����ɕϊ����Ďg�p
    private Dictionary<string, GameObject> enemyPrefabDict = new Dictionary<string, GameObject>();

    // �Q�[���J�n���i�܂��͂��̃X�N���v�g���L�������ꂽ���j�Ɏ��s�����
    void Start()
    {
        // �v���n�u�G���g�����玫�����쐬�iName �� Prefab�j
        foreach (var entry in enemyPrefabEntries)
        {
            if (!enemyPrefabDict.ContainsKey(entry.name))
            {
                enemyPrefabDict.Add(entry.name, entry.prefab);
            }
        }


        LoadEnemyData(); // �G�f�[�^��ǂݍ��ފ֐����Ăяo��

        // �G��CSV�̃f�[�^�������������A������
        for (int i = 0; i < enemyList.Count; i++)
        {
            SpawnEnemy(enemyList[i], i);
        }
    }

    // Resources�t�H���_����CSV�t�@�C������G�f�[�^��ǂݍ��ޏ���
    void LoadEnemyData()
    {
        // Resources�t�H���_����"EnemyData.csv"��TextAsset�Ƃ��ēǂݍ��ށi�g���q�͕s�v�j
        TextAsset csvFile = Resources.Load<TextAsset>("EnemyData");

        // �t�@�C����������Ȃ������ꍇ�̓G���[���b�Z�[�W��\�����ď������I��
        if (csvFile == null)
        {
            Debug.LogError("CSV�t�@�C����������܂���BResources/EnemyData.csv ���m�F���Ă��������B");
            return;
        }

        // �ǂݍ���CSV�e�L�X�g��1�s�������ł���悤�ɂ���iStringReader���g�p�j
        StringReader reader = new StringReader(csvFile.text);

        bool isFirstLine = true; // �ŏ���1�s�ځi�w�b�_�[�j���X�L�b�v���邽�߂̃t���O

        // �ǂݍ��ލs���Ȃ��Ȃ�܂ŌJ��Ԃ�
        while (reader.Peek() > -1)
        {
            // 1�s���̕������ǂݍ���
            string line = reader.ReadLine();

            // 1�s�ځi�w�b�_�[�j�̓f�[�^�Ƃ��Ĉ���Ȃ��̂ŃX�L�b�v
            if (isFirstLine)
            {
                isFirstLine = false;
                continue;
            }

            // �J���}�ŕ�����𕪊����Ċe��̃f�[�^�ɕ�����
            // ��: "1,Goblin,100,10" �� ["1", "Goblin", "100", "10"]
            string[] values = line.Split(',');

            // �f�[�^�̗񐔂����Ғʂ肩�`�F�b�N�i0:ID, 1:Name, 2:HP, 3:Attack�j
            if (values.Length >= 4)
            {
                // ���������l���g����EnemyData�̃C���X�^���X���쐬���A���X�g�ɒǉ�
                EnemyData data = new EnemyData
                {
                    Name = values[1],                  // 2��ځF���O
                    HP = int.Parse(values[2]),         // 3��ځFHP�i������ �� �����ɕϊ��j
                    Attack = int.Parse(values[3])      // 4��ځF�U���́i������ �� �����ɕϊ��j
                };

                enemyList.Add(data); // ���X�g�ɒǉ�
            }
        }

        // �ǂݍ��񂾓G�̐����f�o�b�O�\���i�m�F�p�j
        Debug.Log("�G�f�[�^��ǂݍ��݂܂����B���F" + enemyList.Count);
    }

    void SpawnEnemy(EnemyData data, int index)
    {
        Vector3 spawnPos = new Vector3(index * 2f, 0, 0);

        // �G���ɑΉ�����v���n�u���擾
        if (!enemyPrefabDict.TryGetValue(data.Name, out GameObject prefab))
        {
            Debug.LogError($"�v���n�u��������܂���F{data.Name}");
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
            Debug.LogError("Enemy �X�N���v�g���v���n�u�ɃA�^�b�`����Ă��܂���I");
        }
    }
}
