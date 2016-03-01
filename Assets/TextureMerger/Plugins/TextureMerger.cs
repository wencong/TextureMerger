using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace UnityTM {
    public class TextureMerger {

        private static TextureMerger inst = null;
        public static TextureMerger Instance() {
            if (inst == null) {
                inst = new TextureMerger();
            }
            return inst;
        }

        private string scene_name;
        private List<MergeBase> mergeShaders = new List<MergeBase>();

        private void __ClearData() {
            mergeShaders.Clear();
            //m_mat_dict.Clear();
        }

        public void AddMergeShader(MergeBase mergeShader) {
            mergeShaders.Add(mergeShader);
        }

        public void Init(string scene_name) {
            __ClearData();

            this.scene_name = scene_name;
        }

        public bool MergeRT(Transform model_root) {
            List<Renderer> mergeRenderers = new List<Renderer>();
            MeshRenderer[] renderers = null;

            TS.BeginSample("GetAllRenderer");
            renderers = model_root.GetComponentsInChildren<MeshRenderer>();
            TS.EndSample();

            //如果打了静态批次，就不能用贴图合并了
            for (int i = 0; i < 3; ++i) {
                Renderer r = renderers[i];
                MeshFilter mf = r.GetComponent<MeshFilter>();

                if (mf != null && mf.sharedMesh != null &&
                    mf.sharedMesh.name.StartsWith("Combined Mesh")) {
                        return false;
                }
            }

            for (int i = 0; i < renderers.Length; ++i) {
                Renderer r = renderers[i];
                if (r.sharedMaterials.Length > 1) {
                    //Debug.LogErrorFormat("{0} materials count more than 1", r.gameObject.name);
                    continue;
                }
                mergeRenderers.Add(r);
            }

            for (int i = 0; i < mergeShaders.Count; ++i) {
                mergeShaders[i].PickUpMergeMat(mergeRenderers);

                mergeShaders[i].Merge(false);
            }

            __ClearData();

            return true;
        }

#if UNITY_EDITOR
        //data
        private Dictionary<string, string> m_scene_model_prefabs = new Dictionary<string, string>();
        private Dictionary<string, List<GameObject>> m_scene_models = new Dictionary<string, List<GameObject>>();

        //private Dictionary<Material, List<GameObject>> m_mat_dict = new Dictionary<Material, List<GameObject>>();
        public bool Merge() {
            List<GameObject> list_gameobject = __QuerySceneModels();

            bool ret = __PickUpMergeModel(list_gameobject) ;
            if (!ret) {
                return false;
            }

            ret = __Merge();
            if (!ret) {
                return false;
            }

            ret = __RelinkPrefabs();
            if (!ret) {
                return false;
            }

            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.ImportAsset(scene.path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            ret = EditorSceneManager.SaveScene(scene);

            if (!ret) {
                Debug.Log("Save Scene Failed...");
                return false;
            }

            return true;
        }

        private bool __Merge() {
            for (int i = 0; i < mergeShaders.Count; ++i) {
                mergeShaders[i].Merge(true);
            }

            return true;
        }
        
        private bool __RelinkPrefabs(bool destory = true) {
            foreach (var pair in m_scene_model_prefabs) {
                string prefab_name = pair.Key;
                string prefab_path = pair.Value;

                GameObject prefab = AssetDatabase.LoadAssetAtPath(prefab_path, typeof(GameObject)) as GameObject;
                if (prefab == null) {
                    Debug.LogError(string.Format("Load Prefab Fail : {0}", prefab_path));
                    continue;
                }

                List<GameObject> models = m_scene_models[prefab_name];

                for (int i = 0; i < models.Count; ++i) {
                    GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                    //GameObject instance = GameObject.Instantiate(prefab) as GameObject;

                    if (instance == null) {
                        Debug.LogError(string.Format("Instance prefab failed: {0}", prefab_path));
                        continue;
                    }

                    try {
                        Animator ani = instance.GetComponent<Animator>();
                        if (ani) {
                            GameObject.DestroyImmediate(ani);
                        }
                    }
                    catch (Exception ex) {
                        Debug.LogException(ex);
                    }

                    GameObject model = models[i];

                    GameObjectProperty property = MergeUtils.GetGameObjectProperty(model);
                    MergeUtils.SetGameObjectProperty(instance, property, true);

                    for (int j = 0; j < model.transform.childCount; ++j) {
                        try {
                            //property = model.transform.GetChild(j).gameObject.GetGameObjectProperty();
                            //instance.transform.GetChild(j).gameObject.SetGameObjectProperty(property);
                            property = MergeUtils.GetGameObjectProperty(model.transform.GetChild(j).gameObject);
                            MergeUtils.SetGameObjectProperty(instance.transform.GetChild(j).gameObject, property);
                        }
                        catch (Exception ex) {
                            Debug.LogException(ex);
                        }
                    }

                    PrefabUtility.DisconnectPrefabInstance(instance);
                }

                if (destory) {
                    for (int i = 0; i < models.Count; ++i) {
                        GameObject.DestroyImmediate(models[i]);
                    }
                }
                else {
                    for (int i = 0; i < models.Count; ++i) {
                        models[i].SetActive(false);
                    }
                }

            }

            string prefab_dir = MergePathConfig.GetMergePrefabPath(scene_name);
            MergeUtils.DeleteAllFileInDirectory(prefab_dir, "*.prefab");
            return true;
        }

        private List<GameObject> __QuerySceneModels() {
            Dictionary<string, string> all_prefabs = MergeUtils.AllModelPrefabs;

            GameObject[] gameobj_array = GameObject.FindObjectsOfType<GameObject>();
            List<GameObject> ret_gameobj_list = new List<GameObject>();
            //Dictionary<string, string> ret_objs = new Dictionary<string, string>();

            for (int i = 0; i < gameobj_array.Length; ++i) {
                GameObject go = gameobj_array[i];
                if (all_prefabs != null && all_prefabs.ContainsKey(go.name)) {
                    ret_gameobj_list.Add(go);
                }
            }

            // move the prefab that under another perfab to same parent transform
            for (int i = 0; i < ret_gameobj_list.Count; ++i) {
                GameObject go = ret_gameobj_list[i];

                do {
                    if (go.transform.parent == null) {
                        break;
                    }
                    GameObject parent = go.transform.parent.gameObject;

                    if (!ret_gameobj_list.Contains(parent)) {
                        break;
                    }
                    go.transform.parent = parent.transform.parent;

                } while (true);
            }

            return ret_gameobj_list;
        }
        

        private bool __PickUpMergeModel(List<GameObject> gameobject_list) {
            List<GameObject> mergePrefabs = new List<GameObject>();

            for (int i = 0; i < gameobject_list.Count; ++i) {
                GameObject gameobject = gameobject_list[i];
                if (m_scene_models.ContainsKey(gameobject.name)) {
                    m_scene_models[gameobject.name].Add(gameobject);
                    continue;
                }

                string prefab_path = MergeUtils.GetPrefabPathByName(gameobject.name);
                if (string.IsNullOrEmpty(prefab_path)) {
                    Debug.LogError(string.Format("{0} prefab is not found", gameobject.name));
                    return false;
                }

                string merge_prefab_path = MergePathConfig.GetMergePrefabPath(scene_name);
                string dst_prefab_path = MergeUtils.CopyPrefab(prefab_path, merge_prefab_path);
                m_scene_model_prefabs.Add(gameobject.name, dst_prefab_path);
                m_scene_models.Add(gameobject.name, new List<GameObject>() { gameobject });

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(dst_prefab_path);
                if (prefab == null) {
                    Debug.LogErrorFormat("Load prefab Failed: {0}", dst_prefab_path);
                    return false; ;
                }
                mergePrefabs.Add(prefab);
            }

            List<Renderer> renderers = new List<Renderer>();
            for (int i = 0; i < mergePrefabs.Count; ++i) {
                __AnalyseRender(mergePrefabs[i], ref renderers);
            }

            for (int i = 0; i < mergeShaders.Count; ++i) {
                mergeShaders[i].PickUpMergeMat(renderers);
            }

            return true;
        }

        private void __AnalyseRender(GameObject go, ref List<Renderer> renderers) {
            MeshRenderer r = go.GetComponent<MeshRenderer>();
            if (r != null && r.sharedMaterials.Length == 1) {
                renderers.Add(r);
            }

            for (int i = 0; i < go.transform.childCount; ++i) {
                __AnalyseRender(go.transform.GetChild(i).gameObject, ref renderers);
            }
        }

        Dictionary<Material, List<GameObject>> m_mat_dict = new Dictionary<Material, List<GameObject>>();
        public bool CombineMesh() {
            /*
            List<GameObject> list_gameobject = __QuerySceneModels();
            for (int i = 0; i < list_gameobject.Count; ++i) {
                GameObject go = list_gameobject[i];
                __AnalyzeCombineMat(go);

                for (int j = 0; j < go.transform.childCount; ++j) {
                    __AnalyzeCombineMat(go.transform.GetChild(j).gameObject);
                }
            }
            */
            foreach (var pair in m_mat_dict) {
                Material mat = pair.Key;
                __CombineMeshWithSameMaterial(mat);
            }

            __DestroyEmptyObject();

            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.ImportAsset(scene.path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            bool ret = EditorSceneManager.SaveScene(scene);

            return true;
        }

        private void __DestroyEmptyObject() {
            List<GameObject> list_gameobject = __QuerySceneModels();
            for (int i = 0; i < list_gameobject.Count; ++i) {
                GameObject go = list_gameobject[i];
                if (go.transform.childCount == 0) {
                    if (go.GetComponent<Renderer>() == null) {
                        GameObject.DestroyImmediate(go);
                    }
                }
            }
        }
        private GameObject __CombineMeshWithSameMaterial(Material mat) {
            List<GameObject> goList = m_mat_dict[mat];

            if (goList.Count <= 1) {
                return null;
            }

            List<CombineInstance> combine = new List<CombineInstance>();
            List<GameObject> destroy = new List<GameObject>();
            int nTotalVerts = 0;

            List<CombineInstance> combineGound = new List<CombineInstance>();
            int nTotalVertsGround = 0;

            for (int i = 0; i < goList.Count; ++i) {
                GameObject go = goList[i];

                Renderer renderer = go.GetComponent<Renderer>();
                if (renderer.sharedMaterials.Length > 1) {
                    string s = string.Format("{0} has {1} materials", go.name, renderer.sharedMaterials.Length);
                    //EditorUtility.DisplayDialog("error", s, "OK", "Canlel");
                    Debug.LogErrorFormat(s);
                    return null;
                }

                MeshFilter meshFilter = go.GetComponent<MeshFilter>();
                if (meshFilter == null) {
                    string s = string.Format("{0} does not contain MeshFilter Component", go.name);
                    Debug.LogErrorFormat(s);
                    //EditorUtility.DisplayDialog("error", s, "OK", "Canlel");
                    return null;
                }

                if (meshFilter.sharedMesh == null) {
                    string s = string.Format("{0} does not contain mesh", go.name);
                    Debug.LogErrorFormat(s);
                    //EditorUtility.DisplayDialog("error", s, "OK", "Canlel");
                    return null;
                }

                //go.SetActive(false);
                //GameObject.DestroyImmediate(go);
                destroy.Add(go);

                if (LayerMask.LayerToName(go.layer) == "Ground") {
                    CombineInstance inst = new CombineInstance();
                    inst.mesh = meshFilter.sharedMesh;
                    inst.transform = meshFilter.transform.localToWorldMatrix;
                    combineGound.Add(inst);
                    nTotalVertsGround += meshFilter.sharedMesh.vertexCount;

                    if (nTotalVertsGround > 16384) {
                        __CombineMeshes(MergeUtils.GenerateUnionCode(8), combineGound.ToArray(), mat, "Ground");
                        combineGound.Clear();
                        nTotalVertsGround = 0;
                    }
                }

                else {
                    CombineInstance inst = new CombineInstance();
                    inst.mesh = meshFilter.sharedMesh;
                    inst.transform = meshFilter.transform.localToWorldMatrix;
                    combine.Add(inst);

                    nTotalVerts += meshFilter.sharedMesh.vertexCount;
                    if (nTotalVerts > 16384) {
                        __CombineMeshes(MergeUtils.GenerateUnionCode(8), combine.ToArray(), mat);
                        combine.Clear();
                        nTotalVerts = 0;
                    }
                }
            }

            if (combine.Count > 0) {
                __CombineMeshes(MergeUtils.GenerateUnionCode(8), combine.ToArray(), mat);
            }

            if (combineGound.Count > 0) {
                __CombineMeshes(MergeUtils.GenerateUnionCode(8), combineGound.ToArray(), mat, "Ground");
            }

            for (int i = 0; i < destroy.Count; ++i) {
                GameObject.DestroyImmediate(destroy[i]);
            }

                return null;
        }

        private void __CombineMeshes(string name, CombineInstance[] combine, Material mat, string layer = "Default") {
            GameObject combineGameObject = new GameObject("Combine mesh " + name);

            combineGameObject.layer = LayerMask.NameToLayer(layer);
            GameObjectUtility.SetStaticEditorFlags(combineGameObject, StaticEditorFlags.LightmapStatic);

            MeshFilter msFilter = combineGameObject.AddComponent<MeshFilter>();
            msFilter.sharedMesh = new Mesh();
            msFilter.sharedMesh.name = name + "_mesh_combine";
            try {
                msFilter.sharedMesh.CombineMeshes(combine, true, true);
                //msFilter.sharedMesh.Optimize();
            }

            catch (Exception e) {
                //EditorUtility.DisplayDialog("Error", e.ToString(), "OK", "Cancel");
                Debug.LogException(e);
            }

            //EditorUtils.SaveAsset("Assets", msFilter.sharedMesh);
            combineGameObject.AddComponent<MeshRenderer>().materials = new Material[1] { mat };

            //EditorUtility.DisplayDialog("Infomation", "Combine Successfully", "OK", "Cancel");
            Debug.Log("Combine successfully: " + msFilter.name);

            GameObject goParent = __CreateCombineParentObject();
            if (goParent) {
                combineGameObject.transform.parent = goParent.transform;
            }
        }

        private GameObject __CreateCombineParentObject() {
            GameObject goParent = GameObject.Find("Environment/Models");
            if (goParent != null) {
                GameObject combineNode = GameObject.Find("Environment/Models/Combines");
                if (combineNode == null) {
                    combineNode = new GameObject("Combines");
                    combineNode.transform.parent = goParent.transform;
                }
                return combineNode;
            }

            return null;
        }
#endif

    }
}
