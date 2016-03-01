#if UNITY_EDITOR

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace UnityTM {
    public class GameObjectProperty {
        public bool active;

        public string name;
        public StaticEditorFlags enum_static_flags;
        public string tag;
        public int layer;

        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;

        public Transform parent;

        public int lightmap_index;
        public Vector4 lightmap_offset;
    }

    public static class MergeUtils {
        public static Dictionary<string, string> all_model_prefabs = null;

        public static Dictionary<string, string> AllModelPrefabs {
            get {
                if (all_model_prefabs == null) {

                    all_model_prefabs = new Dictionary<string, string>();

                    string search_pattern = "*.prefab";
                    try {
                        string model_asset_path = MergePathConfig.GetResourceModelPath();
                        string[] prefabs = Directory.GetFiles(model_asset_path, search_pattern, SearchOption.AllDirectories);

                        for (int i = 0; i < prefabs.Length; ++i) {
                            FileInfo file_info = new FileInfo(prefabs[i]);
                            string name = file_info.Name.Substring(0, file_info.Name.Length - search_pattern.Length + 1);
                            if (file_info.Name.Contains(" ")) {
                                Debug.LogWarningFormat("prefab: {0} contain space", name);
                            }

                            if (!all_model_prefabs.ContainsKey(name)) {
                                all_model_prefabs.Add(name, prefabs[i]);
                            }
                            else {
                                Debug.LogErrorFormat("{0} name is alread exist: {1}, {2}", name, all_model_prefabs[name], prefabs[i]);
                            }
                            
                        }
                    }
                    catch (Exception ex) {
                        Debug.LogException(ex);
                    }
                }

                return all_model_prefabs;
            }
        }

        public static string GetPrefabPathByName(string prefab_name) {
            if (all_model_prefabs != null) {
                return all_model_prefabs[prefab_name];
            }
            return null;
        }

        public static string GetCurrentSceneName() {
            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.GetActiveScene();
            string scene_name = scene.name;
            return scene_name;
        }

        public static string GetCurrentScenePath() {
            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.GetActiveScene();
            return scene.path;
        }

        public static void DeleteFile(string file_path) {
            try {
                //File.Delete(file_path);
                bool ret = AssetDatabase.DeleteAsset(file_path);
                if (!ret) {
                    Debug.LogErrorFormat("Delete Asset {0} failed", file_path);
                }
            }
            catch (Exception ex) {
                Debug.LogException(ex);
            }
        }

        public static void DeleteAllFileInDirectory(string path, string ext, string ignore = "") {
            try {
                string[] files = Directory.GetFiles(path, ext);
                for (int i = 0; i < files.Length; ++i) {
                    string file = files[i];
                    if (!string.IsNullOrEmpty(ignore) && file.Contains(ignore)) {
                        continue;
                    }
                    DeleteFile(file);
                    //string metafile = file + ".meta";
                    //DeleteFile(metafile);
                }

                //AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }
            catch (Exception ex) {
                Debug.LogException(ex);
            }
        }

        public static bool CopySceneAndOpenIt(string scene_path) {
            string dst_dir = MergePathConfig.GetMergeMapPath();
            FileInfo fi = new FileInfo(scene_path);
            
            if (scene_path.Contains(dst_dir)) {
                Debug.Log(string.Format("current scene is already merge and opened, {0}", scene_path));
                return true;
            }

            try {
                string dst_scene_path = string.Format("{0}/{1}", dst_dir, fi.Name);
                if (!File.Exists(dst_scene_path)) {
                    File.Copy(scene_path, dst_scene_path);
                    AssetDatabase.ImportAsset(dst_scene_path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                }

                UnityEngine.SceneManagement.Scene scene = EditorSceneManager.OpenScene(dst_scene_path);
                if (!scene.IsValid()) {
                    Debug.Log(string.Format("Open Scene {0} Fail...", dst_scene_path));
                    return false;
                }

                Debug.Log(string.Format("Current Scene: {0}", GetCurrentScenePath()));
                return true;
            }
            catch (Exception ex) {
                Debug.LogException(ex);
            }
            return false;
        }

        public static bool CopyCurrentSceneAndOpenIt() {
            string current_scene_path = GetCurrentScenePath();
            string scene_name = GetCurrentSceneName();

            return CopySceneAndOpenIt(current_scene_path);
        }

        public static string CopyPrefab(string src_path, string dst_dir) {
            try {
                FileInfo file_info = new FileInfo(src_path);
                string dst_file_path = string.Format("{0}/{1}", dst_dir, file_info.Name);

                if (!File.Exists(dst_file_path)) {
                    File.Copy(src_path, dst_file_path);
                    AssetDatabase.ImportAsset(dst_file_path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                }
                return dst_file_path;

            }
            catch (Exception ex) {
                Debug.LogException(ex);
                return null;
            }
        }

        public static GameObjectProperty GetGameObjectProperty(GameObject go) {
            GameObjectProperty property = new GameObjectProperty();

            property.active = go.activeSelf;
            property.name = go.name;
            property.enum_static_flags = GameObjectUtility.GetStaticEditorFlags(go);
            property.tag = go.tag;
            property.layer = go.layer;

            property.position = go.transform.position;
            property.rotation = go.transform.rotation;
            property.scale = go.transform.localScale;
            property.parent = go.transform.parent;

            Renderer renderer = go.GetComponent<Renderer>();

            //Log.Info("GetProperty {0}, Renders {1}", go.name, renderers.Length);
            if (renderer != null) {
                //Log.Info("Get Light Index {0}", render.gameObject.name);
                property.lightmap_index = renderer.lightmapIndex;
                property.lightmap_offset = renderer.lightmapScaleOffset;
            }

            return property;
        }

        public static void SetGameObjectProperty(GameObject go, GameObjectProperty property, bool change_parent = false) {
            if (property.name != go.name) {
                return;
            }
            // go.name = property.name;
            GameObjectUtility.SetStaticEditorFlags(go, property.enum_static_flags);
            go.tag = property.tag;
            go.layer = property.layer;

            if (property.parent != null && change_parent == true) {
                go.transform.parent = property.parent;
                go.transform.position = property.position;
                go.transform.rotation = property.rotation;
                go.transform.localScale = property.scale;
                go.transform.hasChanged = true;
                /*
                go.transform.SetParent(property.parent);
                go.transform.Translate(property.position);
                go.transform.Rotate(property.rotation.eulerAngles);
                go.transform.localScale = property.scale;
                 * */
            }

            Renderer renderer = go.GetComponent<Renderer>();

            //Log.Info("SetProperty {0}, Renders{1}", go.name, renderers.Length);
            if (renderer != null) {
                if (property.lightmap_index != -1) {
                    //Debug.Log(string.Format("Set Light index {0}", renderer.gameObject.name));
                    renderer.lightmapIndex = property.lightmap_index;
                    renderer.lightmapScaleOffset = property.lightmap_offset;
                }
            }

            go.SetActive(property.active);
        }

        public static string __SaveTexture2Png(Texture2D tex, string path) {
            try {
                byte[] bytes = tex.EncodeToPNG();
                if (bytes == null) {
                    Debug.LogError(string.Format("{0} EncodeToPng Failed", path));
                    return "";
                }
                System.IO.File.WriteAllBytes(path, bytes);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);

                TextureImporter texImporter = AssetImporter.GetAtPath(path) as TextureImporter;
                texImporter.textureType = TextureImporterType.Advanced;
                texImporter.mipmapEnabled = false;
                texImporter.isReadable = false;
                texImporter.textureFormat = TextureImporterFormat.AutomaticTruecolor;

                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            }
            catch (Exception ex) {
                Debug.LogException(ex);
                return null;
            }
            return path;
        }

        public static string __SaveMaterial(Material mat, string path) {
            try {
                AssetDatabase.CreateAsset(mat, path);
            }
            catch (Exception ex) {
                Debug.LogException(ex);
                return null;
            }
            return path;
        }

        public static void SetTexturesReadable(List<Texture2D> list_texs, bool readable) {
            for (int i = 0; i < list_texs.Count; ++i) {
                Texture2D tex = list_texs[i];

                string path = AssetDatabase.GetAssetPath(tex);
                TextureImporter tI = AssetImporter.GetAtPath(path) as TextureImporter;

                if (tI.isReadable == readable) {
                    continue;
                }

                tI.isReadable = readable;
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            }
        }

        private static int rep = 0;
        public static string GenerateUnionCode(int codeCount) {
            string str = string.Empty;
            long num2 = DateTime.Now.Ticks + rep;
            rep++;
            System.Random random = new System.Random(((int)(((ulong)num2) & 0xffffffffL)) | ((int)(num2 >> rep)));
            for (int i = 0; i < codeCount; i++) {
                int num = random.Next();
                str = str + ((char)(0x30 + ((ushort)(num % 10)))).ToString();
            }
            return str;
        } 
    }
}

#endif