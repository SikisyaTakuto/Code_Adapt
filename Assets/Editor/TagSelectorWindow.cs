using UnityEditor;
using UnityEngine;

public class TagSelectorWindow : EditorWindow
{
    string selectedTag = "Untagged";
    int selectedIndex = 0;

    [MenuItem("Tools/Tag Selector")]
    public static void ShowWindow()
    {
        GetWindow<TagSelectorWindow>("Tag Selector");
    }

    void OnGUI()
    {
        string[] tags = UnityEditorInternal.InternalEditorUtility.tags;
        selectedIndex = EditorGUILayout.Popup("Select Tag", selectedIndex, tags);
        selectedTag = tags[selectedIndex];

        if (GUILayout.Button("Log Selected Tag"))
        {
            Debug.Log("Selected Tag: " + selectedTag);
        }
    }
}
