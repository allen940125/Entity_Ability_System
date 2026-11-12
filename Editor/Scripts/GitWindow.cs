using UnityEditor;
using UnityEngine;

namespace YourName.GitTool
{
    public class GitWindow : EditorWindow
    {
        [MenuItem("Tools/My Git Tool")]
        public static void ShowWindow()
        {
            GetWindow<GitWindow>("Git Tool");
        }

        private void OnGUI()
        {
            GUILayout.Label("My Custom Git Tool", EditorStyles.boldLabel);
            if (GUILayout.Button("Test Button"))
            {
                Debug.Log("Git Tool Ready!");
            }
        }
    }
}
