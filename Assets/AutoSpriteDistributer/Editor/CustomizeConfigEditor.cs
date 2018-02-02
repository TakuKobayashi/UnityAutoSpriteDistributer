using UnityEngine;
using UnityEditor;

namespace AutoSpriteDistributer
{
    public class CustomizeConfigEditor : EditorWindow
    {
        private int enableAutoSetTag = 0;
        private int enableAutoExportSCriptableObject = 0;
        private string spriteDirectoryPath = "Assets/Sprites/";
        private string exportScriptableObjectsDirectoryPath = "Assets/ScriptableObjects/SpriteTagdbs/";

        [MenuItem("Tools/AutoSpriteDistributerConfig")]
        static void ShowSettingWindow()
        {
            EditorWindow.GetWindow(typeof(CustomizeConfigEditor));
        }

        void OnGUI()
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Enable to automatically set sprite tag");
            enableAutoSetTag = EditorGUILayout.Toggle(PlayerPrefs.GetInt("AutoSpriteDistributer_Enable_Auto_Set_Tag", 0) == 1) ? 1 : 0;
            PlayerPrefs.SetInt("AutoSpriteDistributer_Enable_Auto_Set_Tag", enableAutoSetTag);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Sprite Root Directory");
            spriteDirectoryPath = (string)EditorGUILayout.TextField(PlayerPrefs.GetString("AutoSpriteDistributer_Sprite_Root_Directory", spriteDirectoryPath));
            PlayerPrefs.SetString("AutoSpriteDistributer_Sprite_Root_Directory", spriteDirectoryPath);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Enable to automatically export refer to sprites");
            enableAutoExportSCriptableObject = EditorGUILayout.Toggle(PlayerPrefs.GetInt("AutoSpriteDistributer_Enable_Auto_Export_Reference", 0) == 1) ? 1 : 0;
            PlayerPrefs.SetInt("AutoSpriteDistributer_Enable_Auto_Export_Reference", enableAutoExportSCriptableObject);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Export ScriptableObjects Root Directory");
            exportScriptableObjectsDirectoryPath = (string)EditorGUILayout.TextField(PlayerPrefs.GetString("AutoSpriteDistributer_Export_ScriptableObjects_Root_Directory", exportScriptableObjectsDirectoryPath));
            PlayerPrefs.SetString("AutoSpriteDistributer_Export_ScriptableObjects_Root_Directory", spriteDirectoryPath);
            GUILayout.EndHorizontal();

            PlayerPrefs.Save();
        }
    }
}