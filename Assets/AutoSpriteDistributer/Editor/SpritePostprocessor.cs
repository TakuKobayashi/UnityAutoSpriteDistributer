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
        //対象のディレクトリへのパス
        private const string SpriteDirectoryPath = "Assets/Sprites/";

        // SpriteDBがある場所
        private const string SpriteDBDirectoryPath = "Assets/ScriptableObjects/SpriteTagdbs/";

        /// <summary>
        /// <para>ファイルが編集、追加、削除されたら、importバーが完了した後に呼ばれる</para>
        /// </summary>
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            HashSet<string> importAndDeletePathes = new HashSet<string>(importedAssets);
            for (int i = 0; i < deletedAssets.Length; ++i)
            {
                importAndDeletePathes.Add(deletedAssets[i]);
            }
            if (importAndDeletePathes.Count > 0 && Array.Exists(importAndDeletePathes.ToArray(), path => path.Contains(SpriteDirectoryPath)))
            {
                BuildSpriteDB();
            }
        }

        /// <summary>
        /// <para>Textureファイルのインポート設定 Textureファイルがインポートされる直前(importバーが表示される前)に呼び出される</para>
        /// </summary>
        private void OnPreprocessTexture()
        {
            if (!this.assetImporter.assetPath.Contains(SpriteDirectoryPath))
            {
                return;
            }
            UpdatePackingSpriteInfo();
        }

        /// <summary>
        /// <para>SpritePackerでPackingするためにSprite以下に存在するSprite全てをフォルダ名でTagを自動的につけちゃう</para>
        /// </summary>
        public static void ReimportAllSprite()
        {
            string[] pathes = AssetDatabase.GetAllAssetPaths();
            for (int i = 0; i < pathes.Length; ++i)
            {
                string path = pathes[i];
                if (path.Contains(SpriteDirectoryPath))
                {
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                }
            }
        }

        /// <summary>
        /// <para>Spriteを参照して、Tagに対応したSpriteDBを作る</para>
        /// </summary>
        public static void BuildSpriteDB()
        {
            string[] pathes = AssetDatabase.GetAllAssetPaths();

            HashSet<string> currentDbFilepathes = new HashSet<string>(Directory.GetFiles(SpriteDBDirectoryPath));

            Dictionary<string, HashSet<Sprite>> tagSprites = new Dictionary<string, HashSet<Sprite>>();

            for (int i = 0; i < pathes.Length; ++i)
            {
                string path = pathes[i];
                Match match = Regex.Match(path, @"" + SpriteDirectoryPath + ".+.png");
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
            List<UnityScriptableObject> spritedbs = new List<UnityScriptableObject>();

            List<string> spriteTags = tagSprites.Keys.ToList();
            AssetDatabase.StartAssetEditing();
            for (int i = 0; i < spriteTags.Count; ++i)
            {
                string tag = spriteTags[i];
                UnityScriptableObject spriteDB = LoadOrCreateSpriteDb(tag);
                List<Sprite> sps = tagSprites[tag].ToList();
                sps.Sort((a, b) => string.Compare(a.name, b.name));
                spriteDB.SetObjects(sps.ToArray());
                spritedbs.Add(spriteDB);
                // 現在存在するタグのDBファイルは残し続けるため、削除候補から除外する
                currentDbFilepathes.RemoveWhere(dbPath => dbPath.Contains(tag));
            }
            spritedbs.Sort((a, b) => string.Compare(a.name, b.name));
            // 使われていないAssetDBファイルは消し飛ばす
            List<string> deleteFilePathes = currentDbFilepathes.ToList();
            for (int i = 0; i < deleteFilePathes.Count; ++i)
            {
                File.Delete(deleteFilePathes[i]);
            }
            AssetDatabase.StopAssetEditing();
            //変更をUnityEditorに伝える//
            for (int i = 0; i < spritedbs.Count; ++i)
            {
                EditorUtility.SetDirty(spritedbs[i]);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(tagSprites.Sum(ts => ts.Value.Count) + " Sprites were imported.");
        }

        private static UnityScriptableObject LoadOrCreateSpriteDb(string tag)
        {
            string spriteDBFilePath = SpriteDBDirectoryPath + tag + ".asset";
            UnityScriptableObject spriteDB = AssetDatabase.LoadAssetAtPath(spriteDBFilePath, typeof(UnityScriptableObject)) as UnityScriptableObject;
            if (spriteDB == null)
            {
                spriteDB = ScriptableObject.CreateInstance<UnityScriptableObject>();
                AssetDatabase.CreateAsset(spriteDB, spriteDBFilePath);
            }
            return spriteDB;
        }

        //SpritePackerでPackingできるように対象の(Importした)Textureの状態を更新する
        private void UpdatePackingSpriteInfo()
        {
            //インポート時のTextureファイルを設定するクラス
            TextureImporter textureImporter = this.assetImporter as TextureImporter;
            //対象のディレクトリ以外はスルー
            if (!textureImporter.assetPath.Contains(SpriteDirectoryPath) || !string.IsNullOrEmpty(textureImporter.spritePackingTag))
            {
                return;
            }

            //親のディレクトリ名取得
            string directoryName = Path.GetFileName(Path.GetDirectoryName(textureImporter.assetPath));

            //テクスチャの設定
            textureImporter.textureType = TextureImporterType.Sprite; //テクスチャタイプをSpriteに

            //SliceしていないテクスチャのみPackingTagを付加する
            Vector4 borderVec = textureImporter.spriteBorder;
            bool slice = (borderVec.magnitude > 0f) ? true : false;
            if (!slice)
                textureImporter.spritePackingTag = directoryName;              //タグをディレクトリ名に設定

            //圧縮用ディレクトリかそうじゃないかでフォーマットを変える
            //if(directoryName.Contains("Compressed")){
            //  textureImporter.textureFormat = TextureImporterFormat.AutomaticCompressed;
            //}
            //else{
            //  textureImporter.textureFormat = TextureImporterFormat.AutomaticTruecolor;
            //}

            //その他の設定
            //textureImporter.mipmapEnabled       = false;            //MipMapを作成しないように
            //textureImporter.spritePixelsPerUnit = 100;              //Pixels Per Unitを変更
            //textureImporter.filterMode          = FilterMode.Point; //Filter ModeをPointに変更
        }
    }
}