using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace AutoSpriteDistributer{
    public class SpritePostprocessor : AssetPostprocessor
    {
        /// <summary>
        /// <para>Once the file has been edited, added, deleted, it will be called after the import will be completed.</para>
        /// </summary>
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (PlayerPrefs.GetInt("AutoSpriteDistributer_Enable_Auto_Export_Reference", 0) == 0) return;
            string spriteDirPath = PlayerPrefs.GetString("AutoSpriteDistributer_Export_ScriptableObjects_Root_Directory");
            HashSet<string> importAndDeletePathes = new HashSet<string>(importedAssets);
            for (int i = 0; i < deletedAssets.Length; ++i)
            {
                importAndDeletePathes.Add(deletedAssets[i]);
            }
            if (importAndDeletePathes.Count > 0 && Array.Exists(importAndDeletePathes.ToArray(), path => path.Contains(spriteDirPath)))
            {
                BuildSpritesScriptableObject();
            }
        }

        /// <summary>
        /// <para>Called just before the texture file is imported.</para>
        /// </summary>
        private void OnPreprocessTexture()
        {
            if (PlayerPrefs.GetInt("AutoSpriteDistributer_Enable_Auto_Set_Tag", 0) == 0) return;
            string spriteDirPath = PlayerPrefs.GetString("AutoSpriteDistributer_Export_ScriptableObjects_Root_Directory");
            if (!this.assetImporter.assetPath.Contains(spriteDirPath))
            {
                return;
            }
            UpdatePackingSpriteInfo();
        }

        /// <summary>
        /// <para>Automatically add tags by folder name to all Sprites</para>
        /// </summary>
        public static void ReimportAllSprite()
        {
            string spriteDirPath = PlayerPrefs.GetString("AutoSpriteDistributer_Export_ScriptableObjects_Root_Directory");
            string[] pathes = AssetDatabase.GetAllAssetPaths();
            for (int i = 0; i < pathes.Length; ++i)
            {
                string path = pathes[i];
                if (path.Contains(spriteDirPath))
                {
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                }
            }
        }

        /// <summary>
        /// <para>Refer to sprite and create the ScriptableObject correspond to the tag</para>
        /// </summary>
        public static void BuildSpritesScriptableObject()
        {
            string[] pathes = AssetDatabase.GetAllAssetPaths();
            string spriteDirPath = PlayerPrefs.GetString("AutoSpriteDistributer_Export_ScriptableObjects_Root_Directory");

            HashSet<string> currentsoFilepathes = new HashSet<string>(Directory.GetFiles(PlayerPrefs.GetString("AutoSpriteDistributer_Export_ScriptableObjects_Root_Directory")));

            Dictionary<string, HashSet<Sprite>> tagSprites = new Dictionary<string, HashSet<Sprite>>();

            for (int i = 0; i < pathes.Length; ++i)
            {
                string path = pathes[i].ToLower();
                Match match = Regex.Match(path, @"" + spriteDirPath + ".+.(png|jpg|jpeg)");
                if (match.Success)
                {
                    TextureImporter spriteImporter = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (spriteImporter != null && spriteImporter.textureType == TextureImporterType.Sprite && !string.IsNullOrEmpty(spriteImporter.spritePackingTag))
                    {
                        if (!tagSprites.ContainsKey(spriteImporter.spritePackingTag))
                        {
                            tagSprites.Add(spriteImporter.spritePackingTag, new HashSet<Sprite>());
                        }
                        tagSprites[spriteImporter.spritePackingTag].Add(AssetDatabase.LoadAssetAtPath<Sprite>(path));
                    }
                }
            }
            List<UnityScriptableObject> spritesos = new List<UnityScriptableObject>();

            List<string> spriteTags = tagSprites.Keys.ToList();
            AssetDatabase.StartAssetEditing();
            for (int i = 0; i < spriteTags.Count; ++i)
            {
                string tag = spriteTags[i];
                UnityScriptableObject spriteScriptableObject = LoadOrCreateSpriteScriptableObject(tag);
                List<Sprite> sps = tagSprites[tag].ToList();
                sps.Sort((a, b) => string.Compare(a.name, b.name));
                spriteScriptableObject.SetObjects(sps.ToArray());
                spritesos.Add(spriteScriptableObject);
                // Since the asset file of the existing tag is kept, it is excluded from the deletion candidate
                currentsoFilepathes.RemoveWhere(soPath => soPath.Contains(tag));
            }
            spritesos.Sort((a, b) => string.Compare(a.name, b.name));
            // Delete unused Asset files
            List<string> deleteFilePathes = currentsoFilepathes.ToList();
            for (int i = 0; i < deleteFilePathes.Count; ++i)
            {
                File.Delete(deleteFilePathes[i]);
            }
            AssetDatabase.StopAssetEditing();
            //Call the changes to UnityEditor
            for (int i = 0; i < spritesos.Count; ++i)
            {
                EditorUtility.SetDirty(spritesos[i]);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(tagSprites.Sum(ts => ts.Value.Count) + " Sprites were imported.");
        }

        private static UnityScriptableObject LoadOrCreateSpriteScriptableObject(string tag)
        {
            string spriteAssetFilePath = PlayerPrefs.GetString("AutoSpriteDistributer_Export_ScriptableObjects_Root_Directory") + tag + ".asset";
            UnityScriptableObject spriteScriptableObject = AssetDatabase.LoadAssetAtPath(spriteAssetFilePath, typeof(UnityScriptableObject)) as UnityScriptableObject;
            if (spriteScriptableObject == null)
            {
                spriteScriptableObject = ScriptableObject.CreateInstance<UnityScriptableObject>();
                AssetDatabase.CreateAsset(spriteScriptableObject, spriteAssetFilePath);
            }
            return spriteScriptableObject;
        }

        //Edit the setting to be imported texture file so that it can be packed with SpritePacker.
        private void UpdatePackingSpriteInfo()
        {
            string spriteDirPath = PlayerPrefs.GetString("AutoSpriteDistributer_Export_ScriptableObjects_Root_Directory");
            //Class to set Texture file on import.
            TextureImporter textureImporter = this.assetImporter as TextureImporter;
            //Ignore others except the target directories.
            if (!textureImporter.assetPath.Contains(spriteDirPath) || !string.IsNullOrEmpty(textureImporter.spritePackingTag))
            {
                return;
            }

            //Get the parent directory name
            string directoryName = Path.GetFileName(Path.GetDirectoryName(textureImporter.assetPath));

            //setting the texture
            textureImporter.textureType = TextureImporterType.Sprite; //change textureType to Sprite

            //Add only PackingTag for textures not sliced.
            Vector4 borderVec = textureImporter.spriteBorder;
            bool slice = (borderVec.magnitude > 0f) ? true : false;
            if (!slice)
            {
                textureImporter.spritePackingTag = directoryName;              //Set the tag by directory name
            }
        }
    }
}