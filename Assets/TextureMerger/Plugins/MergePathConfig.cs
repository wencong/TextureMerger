#if UNITY_EDITOR

using UnityEngine;
using System.Collections;
using UnityEditor;
using System;
using System.IO;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace UnityTM {
    public static class MergePathConfig {
	    //public static string asset_root_path = "Assets/_Game/Resources";
	    //public static string artwork_path = "Packages/ArtWorks";
	
	    //public static string map_path = "Packages/ArtWorks/Map/Maps";
	    //public static string model_path = "Packages/ArtWorks/Map/Model";
	
	    // dst folders
        private static string merge_root_path = null;
        private static string merge_map_path = null;
        private static string merge_model_path = null;
        private static string resource_model_path = null;
	
	    private static void _CreateDirectory(string path) {
		    if (!Directory.Exists(path)) {
			    Directory.CreateDirectory(path);
		    }
	    }

	    public static void InitMergePath(string model_path, string merge_root) {
            resource_model_path = model_path;

            merge_root_path = string.Format("{0}/_MergeMap", merge_root);
		    _CreateDirectory(merge_root_path);
		
		    merge_map_path = string.Format("{0}/Maps", merge_root_path);
		    _CreateDirectory(merge_map_path);
		
		    merge_model_path = string.Format("{0}/Models", merge_root_path);
		    _CreateDirectory(merge_model_path);
		
	    }

	    public static string GetCurrentSceneName() {
		    //string scene_path = EditorApplication.currentScene;
            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.GetActiveScene();
            string scene_name = scene.name;
		    return scene_name;
	    }

	    public static string GetCurrentScenePath() {
		    //return EditorApplication.currentScene;
            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.GetActiveScene();
            return scene.path;
	    }

        public static string GetResourceModelPath() {
		    //return string.Format("{0}/{1}", asset_root_path, resource_path);
            return resource_model_path;
	    }
	
	    public static string GetMergeMapPath() {
		    return merge_map_path;
	    }
	
	    // 获取model文件夹下当前场景的文件目录
	    public static string GetMergeModelPath(string scene_name) {
		    if (merge_model_path != null) {
			    string model_path = string.Format("{0}/{1}", merge_model_path, scene_name);
			    _CreateDirectory(model_path);
			    return model_path;
		    }
		    return null;
	    }
	
	    public static string GetMergePrefabPath(string scene_name) {
		    string prefab_path = GetMergeModelPath(scene_name);
		    if (prefab_path != null) {
			    string mat_path = string.Format("{0}/Prefabs", prefab_path);
			    _CreateDirectory(mat_path);
			    return mat_path;
		    }
		    return null;
	    }
	
	    public static string GetMergeMatPath(string scene_name) {
		    string fbx_path = GetMergeModelPath(scene_name);
		    if (fbx_path != null) {
			    string mat_path = string.Format("{0}/Materials", fbx_path);
			    _CreateDirectory(mat_path);
			    return mat_path;
		    }
		    return null;
	    }
	
	    public static string GetMergeTexturePath(string scene_name) {
		    string fbx_path = GetMergeModelPath(scene_name);
		    if (fbx_path != null) {
			    string tex_path = string.Format("{0}/Textures", fbx_path);
			    _CreateDirectory(tex_path);
			    return tex_path;
		    }
		    return null;
	    }
    }
}

#endif