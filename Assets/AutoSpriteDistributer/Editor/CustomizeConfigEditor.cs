using UnityEngine;
using UnityEditor;

namespace AutoSpriteDistributer
{
    public class CustomizeConfigEditor : EditorWindow
    {
        private int enableAutoSetTag = 0;
        private int enableAutoExportSCriptableObject = 0;
        private string spriteDirectoryPath = "Assets/AutoSpriteDistributer/Sprites/";

        [MenuItem("Tools/AutoSpriteDistributerConfig")]
        static void ShowSettingWindow()
        {
            EditorWindow.GetWindow(typeof(CustomizeConfigEditor));
        }

        void OnGUI()
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Enable to automatically convert sprite file and attach sprite tag");
            enableAutoSetTag = EditorGUILayout.Toggle(PlayerPrefs.GetInt(SpritePostprocessor.EnableAutomaticKey, 0) == 1) ? 1 : 0;
            PlayerPrefs.SetInt(SpritePostprocessor.EnableAutomaticKey, enableAutoSetTag);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Search Sprite File Root Directory");
            spriteDirectoryPath = (string)EditorGUILayout.TextField(PlayerPrefs.GetString(SpritePostprocessor.SpriteRootDirectoryKey, spriteDirectoryPath));
            PlayerPrefs.SetString(SpritePostprocessor.SpriteRootDirectoryKey, spriteDirectoryPath);
            GUILayout.EndHorizontal();

            if(enableAutoSetTag == 0){
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(new GUIContent("Convert And Attach")))
                {
                    SpritePostprocessor.ConvertAndAttachAllSprite();
                }
                GUILayout.EndHorizontal();
            }

            PlayerPrefs.Save();
        }
    }
}