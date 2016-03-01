using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using UnityTM;
using System.Text;
using System.IO;
using System;

public class MergePanel {
    private static void ModifTextureReadable(string path, bool isReadable) {
        TextureImporter mainImporter = AssetImporter.GetAtPath(path) as TextureImporter;
        if (mainImporter == null) {
            UnityEngine.Debug.LogErrorFormat(path);
            return;
        }
        if (mainImporter.isReadable != isReadable) {
            mainImporter.isReadable = isReadable;
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        }
    }

    [MenuItem("Tools/贴图合并/设置所有贴图可读")]
    public static void ModifyAllTexturesReadable() {
		
        string[] supportext = new string[] {".png", ".tga", ".psd", ".tif", ".jpg"};

        string[] texPaths = new string[] { "Packages/Artworks/Map/Common/Textures",
                                           "Packages/Artworks/Map/Model" };

		/*
        for (int i = 0; i < texPaths.Length; ++i) {
            string texPath = texPaths[i];
            texPath = PathConfig.ExportPackagesPath(texPath);

            Dictionary<string, string> dict = new Dictionary<string, string>();
            PathConfig.ScanResourceAssetPath(texPath, dict, true, supportext);

            foreach (var pair in dict) {
                string path = pair.Value;
                string realPath = PathConfig.FormatAssetPath(path);
                //Texture2D sourceTexture = AssetDatabase.LoadAssetAtPath(realPath, typeof(Texture2D)) as Texture2D;

                ModifTextureReadable(realPath, true);
            }
        }*/
	}

    [MenuItem("Tools/贴图合并/当前场景")]
    public static void MergeCurrentScene() {

		/*
        string model_path = PathConfig.ExportPackagesPath("Packages/ArtWorks/Map/Model");
        string merge_root = PathConfig.ExportPackagesPath("Packages/ArtWorks");

        MergePathConfig.InitMergePath(model_path, merge_root);
       
        bool ret = MergeUtils.CopyCurrentSceneAndOpenIt();
        if (ret) {
            RemoveBrackets();
            string scene_name = MergeUtils.GetCurrentSceneName();

            TextureMerger.Instance().Init(scene_name);

            TextureMerger.Instance().AddMergeShader(new MergeDiffuse("Mobile/Diffuse"));
            TextureMerger.Instance().AddMergeShader(new MergeTransCutoutDiffuse("Legacy Shaders/Transparent/Cutout/Diffuse"));
            TextureMerger.Instance().AddMergeShader(new MergeAlphaEmission("Kingsoft/Scene/Special/AlphaEmission"));
            TextureMerger.Instance().AddMergeShader(new MergeAlphaReflection("Kingsoft/Scene/AlphaReflection"));
            TextureMerger.Instance().AddMergeShader(new MergeOpaqueReflection("Kingsoft/Scene/OpaqueReflection"));

            GameObject model = GameObject.Find("Environment/Models");
            if (model != null) {
                TextureMerger.Instance().Merge();
            }
            

            Debug.LogFormat("{0} Merge OK", scene_name);
            EditorUtility.DisplayDialog(scene_name, "Merge Finish", "OK");
        }
        */
    }

	[MenuItem("KingsoftTools/场景工具/一键去括号")]
    public static void RemoveBrackets() {
        GameObject[] obj_array = GameObject.FindObjectsOfType<GameObject>();
        int nTotalNum = 0;
        for (int i = 0; i < obj_array.Length; ++i) {
            GameObject gameObject = obj_array[i];
            int pos = gameObject.name.IndexOf('(');
            if (pos != -1) {
                gameObject.name = gameObject.name.Substring(0, pos - 1);
                nTotalNum++;
            }
        }

        Debug.LogFormat("{0} GameObject remove brackets", nTotalNum.ToString());
        if (nTotalNum > 0) {
            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
	}

    [MenuItem("KingsoftTools/场景工具/检查模型UV")]
    public static void CheckMeshUV() {
        List<string> igonreMesh = new List<string>(){ "denglu_ShiTou_01", "emei_ShiTou_01", "emei_ShiTou_02", "emei_ShiTou_04", "Emei_DiXing_01" };
        List<string> igonreDir = new List<string>() { "Animations_Models" };

        string[] fbxs = Directory.GetFiles("Assets/_Game/Resources/Packages/ArtWorks/Map/Model", "*.fbx", SearchOption.AllDirectories);

        string allErrorMesh = "";
        for (int i = 0; i < fbxs.Length; ++i) {
            try {
                for (int j = 0; j < igonreDir.Count; ++j) {
                    if (fbxs[i].Contains(igonreDir[j])) {
                        continue;
                    }
                }
                Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(fbxs[i]);
                if (mesh == null) {
                    UnityEngine.Debug.LogFormat("Load File:{0}", fbxs[i]);
                    continue;
                }
                if (igonreMesh.Contains(mesh.name)) {
                    continue;
                }
                Vector2[] uva = mesh.uv;
                for (int j = 0; j < uva.Length; ++j) {
                    if (uva[j].x > 1.0f || uva[j].y > 1.0f ||
                        uva[j].x < 0.0f || uva[j].y < 0.0f) {
                        UnityEngine.Debug.LogError(mesh.name + " UV error");
                        allErrorMesh += string.Format("{0}\r\n", mesh.name);
                        break;
                    }
                }
            }
            catch (Exception ex) {
                Debug.LogException(ex);
            }
        }
        Debug.Log(allErrorMesh);
    }
}
