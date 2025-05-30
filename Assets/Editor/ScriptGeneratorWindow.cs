using UnityEditor;
using UnityEngine;
using System.IO;

public class ScriptGeneratorWindow : EditorWindow
{
    string scriptName = "NewScript";
    string inheritType = "MonoBehaviour";
    string folderPath = "Assets";  // 初期保存先

    [MenuItem("Tools/Script Generator")]
    public static void ShowWindow()
    {
        GetWindow<ScriptGeneratorWindow>("Script Generator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Script Generator", EditorStyles.boldLabel);

        scriptName = EditorGUILayout.TextField("Class Name", scriptName);
        inheritType = EditorGUILayout.TextField("Inherit From", inheritType);

        GUILayout.Space(10);
        EditorGUILayout.LabelField("Save Folder:", folderPath);

        if (GUILayout.Button("Choose Folder"))
        {
            string path = EditorUtility.OpenFolderPanel("Select Script Save Folder", "Assets", "");
            if (!string.IsNullOrEmpty(path))
            {
                // 絶対パスからAssets相対パスに変換
                if (path.StartsWith(Application.dataPath))
                {
                    folderPath = "Assets" + path.Substring(Application.dataPath.Length);
                }
                else
                {
                    Debug.LogWarning("You must select a folder inside the Assets directory.");
                }
            }
        }

        GUILayout.Space(10);
        if (GUILayout.Button("Generate Script"))
        {
            CreateScript(scriptName, inheritType);
        }
    }

    void CreateScript(string name, string baseClass)
    {
        string fullPath = Path.Combine(folderPath, name + ".cs");

        if (File.Exists(fullPath))
        {
            Debug.LogWarning("Script already exists!");
            return;
        }

        string content =
$@"using UnityEngine;

public class {name} : {baseClass}
{{
    void Start() {{ }}

    void Update() {{ }}
}}";

        File.WriteAllText(fullPath, content);
        AssetDatabase.Refresh();

        Debug.Log($"Script '{name}.cs' created at: {folderPath}");
    }
}
