using System;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityTM {
    public abstract class MergeBase {
        private string shader_name = "";
        private bool bSaveAsset = false;

        // mat dictionaty
        private List<Material> m_merge_mats = new List<Material>();
        private Dictionary<Material, List<Renderer>> m_mat_dict = new Dictionary<Material, List<Renderer>>();
        private Dictionary<Mesh, Mesh> m_modifyMesh = new Dictionary<Mesh, Mesh>();

        public abstract bool MergeMaterialsWithSameShader(Material[] mats, string shader_name, ref Material mergeMat, ref Rect[] rect);

        public MergeBase(string shader_name) {
            this.shader_name = shader_name;
        }

        public void ClearData() {
            m_merge_mats.Clear();
            m_mat_dict.Clear();
            m_modifyMesh.Clear();
        }

        public bool Merge(bool bSaveAsset = false) {
            TS.BeginSample("Merge " + shader_name);
            this.bSaveAsset = bSaveAsset;

            //UnityEngine.Debug.Log(string.Format("Merge Scene:{0} Shader:{1}...", MergeUtils.GetCurrentSceneName(), shader_name));
            bool ret = __Merge();
            ClearData();

            TS.EndSample();

            return ret;
        }

        public bool PickUpMergeMat(List<Renderer> renderers) {
            TS.BeginSample("PickUpMergeMat " + shader_name);
            for (int i = 0; i < renderers.Count; ++i) {
                Renderer renderer = renderers[i];
                if (renderer.sharedMaterials.Length > 1) {
                    //UnityEngine.Debug.LogErrorFormat("{0} contain {1} materials", renderer.gameObject.name, renderer.sharedMaterials.Length.ToString());
                }
                else {
                    Material mat = renderer.sharedMaterial;
                    if (mat.shader.name == shader_name) {
                        if (!m_mat_dict.ContainsKey(mat)) {
                            m_mat_dict.Add(mat, new List<Renderer>() { renderer });
                            m_merge_mats.Add(mat);
                        }
                        else {
                            m_mat_dict[mat].Add(renderer);
                        }
                    }
                }
            }
            TS.EndSample();
            return true;
        }

        private bool __Merge() {
            if (m_merge_mats.Count <= 1) {
                //UnityEngine.Debug.LogWarning(string.Format("{0} Material Count: {1}, don't need merge", shader_name, m_merge_mats.Count));
                return false;
            }

            //return MergePrefabWithSameShader(prefab_list.ToArray(), shader_name);
            Material mergeMat = null;
            Rect[] rect = new Rect[1];

            bool ret = MergeMaterialsWithSameShader(m_merge_mats.ToArray(), shader_name, ref mergeMat, ref rect);
            if (ret == false) {
                //UnityEngine.Debug.LogError("Merge Material failed");
                return false;
            }

            TS.BeginSample("ModifyUV " + shader_name);
            for (int i = 0; i < m_merge_mats.Count; ++i) {
                Material mat = m_merge_mats[i];
                List<Renderer> renderers = m_mat_dict[mat];

                for (int j = 0; j < renderers.Count; ++j) {

                    Renderer renderer = renderers[j];
                    MeshFilter mf = renderer.GetComponent<MeshFilter>();

                    if (mf.sharedMesh == null) {
                        UnityEngine.Debug.LogErrorFormat("{0} mesh is null", renderer.name);
                    }

                    if (mf.sharedMesh.name.StartsWith("Combined Mesh")) {
                        continue;
                    }

                    Mesh newMesh = null;
                    if (!m_modifyMesh.TryGetValue(mf.sharedMesh, out newMesh)) {
                        newMesh = __ModifyMeshUV(mf.sharedMesh, rect[i]);
                        m_modifyMesh.Add(mf.sharedMesh, newMesh);
                    }
                    if (newMesh == null) {
                        //UnityEngine.Debug.LogErrorFormat("{0} ");
                        continue;
                    }
                        
                    mf.sharedMesh = newMesh;
                    renderer.sharedMaterial = mergeMat;
                    
                    if (bSaveAsset) {
#if UNITY_EDITOR
                        try {
                            string assetPath = AssetDatabase.GetAssetPath(renderer.gameObject);
                            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                        }
                        catch (Exception ex) {
                            UnityEngine.Debug.LogException(ex);
                        }
#endif
                    }
                }
                    
            }

            TS.EndSample();
            //MergeUtils.DeleteAllFileInDirectory(merge_material_path, "*.mat", "mergeMat");
            return true;
        }

        private Mesh __ModifyMeshUV(Mesh mesh, Rect rect) {

            Mesh modifyMesh = UnityEngine.Object.Instantiate<Mesh>(mesh);
            modifyMesh.name = modifyMesh.name.Replace("(Clone)", "");

            Vector2[] uva = mesh.uv;
            Vector2[] uvb = new Vector2[uva.Length];

            for (int j = 0; j < uva.Length; ++j) {
                if (uva[j].x > 1.0f || uva[j].y > 1.0f ||
                    uva[j].x < 0.0f || uva[j].y < 0.0f) {
                    UnityEngine.Debug.LogError(mesh.name + " UV error");
                    return null;
                }
                uvb[j].x = uva[j].x * rect.width + rect.x;
                uvb[j].y = uva[j].y * rect.height + rect.y;
            }
            modifyMesh.uv = uvb;

            return modifyMesh;
        }
        /*
        private bool __IsNeedMerge(GameObject prefab) {
            Renderer renderer = prefab.GetComponent<Renderer>();

            if (renderer != null) {
                if (renderer.sharedMaterials.Length > 1) {
                    Debug.LogError(string.Format("{0} contains to many materials", prefab.name));
                    return false;
                }

                if (renderer.sharedMaterials.Length != 0) {
                    Material mat = renderer.sharedMaterials[0];
                    if (mat.shader != null && shader_name == mat.shader.name) {
                        return true;
                    }
                }
            }

            for (int i = 0; i < prefab.transform.childCount; ++i) {
                bool ret = __IsNeedMerge(prefab.transform.GetChild(i).gameObject);
                if (ret) {
                    return true;
                }
            }

            return false;
        }*/

        private bool bFirstTex = true;
        private List<Vector2> texSizes = new List<Vector2>();

        public void ModifyTextureSize(List<Texture2D> texs) {
            if (bFirstTex) {
                return;
            }

            if (texSizes.Count != texs.Count) {
                return;
            }

            for (int i = 0; i < texs.Count; ++i) {
                Texture2D tex2d = texs[i];
                Vector2 size = texSizes[i];
                if ((tex2d.width * 2) != size.x || (tex2d.height * 2) != size.y) {
                    Texture2D newTex2d = UnityEngine.Object.Instantiate<Texture2D>(tex2d);
                    try {
                        bool ret = newTex2d.Resize((int)size.x / 2, (int)size.y / 2);
                        if (ret) {
                            texs[i] = newTex2d;
                        }
                        else {
                            UnityEngine.Debug.LogErrorFormat("Resize {0} failed", tex2d.name);
                        }                    
                    }
                    catch (Exception ex) {
                        UnityEngine.Debug.LogException(ex);
                    }
                    
                }
            }
            
        }

        public Texture2D MergeTextures(List<Texture2D> texs, ref Rect[] rects, string shader_name, int maxSize = 2048, bool bColorTex = false) {
            if (!bFirstTex && !bColorTex) {
                ModifyTextureSize(texs);
            }

            TS.BeginSample("MergeTextures " + shader_name);
            List<Texture2D> mergeTexs = new List<Texture2D>();
            List<Texture2D> instTexs = new List<Texture2D>();

            for (int i = 0; i < texs.Count; ++i) {
                Texture2D tex = texs[i];
                //处理贴图重复的情况
                if (mergeTexs.Contains(tex)) {
                    try {
                        Texture2D instTex = UnityEngine.Object.Instantiate(tex) as Texture2D;
                        mergeTexs.Add(instTex);
                        instTexs.Add(instTex);
                    }
                    catch (Exception ex) {
                        UnityEngine.Debug.LogException(ex);
#if UNITY_EDITOR
                        string path = AssetDatabase.GetAssetPath(tex);
                        UnityEngine.Debug.Log(path);
#endif                  
                        rects = null;
                        return null;
                    }
                    
                }
                else {
                    mergeTexs.Add(tex);
                }
            }

            // 记录所有贴图大小的比例，对第二张进行缩放
            if (bFirstTex) {
                for (int i = 0; i < mergeTexs.Count; ++i) {
                    Vector2 size = new Vector2(mergeTexs[i].width, mergeTexs[i].height);
                    texSizes.Add(size);
                }
                bFirstTex = false;
            }

            Texture2D mergeTex = new Texture2D(maxSize, maxSize, texs[0].format, false);
            try {
                rects = mergeTex.PackTextures(mergeTexs.ToArray(), 0, maxSize, true);
            }
            catch (Exception ex) {
                UnityEngine.Debug.LogException(ex);
                rects = null;
                return null;
            }
            

            if (bSaveAsset) {
#if UNITY_EDITOR
                string s_ext = shader_name.Replace('/', '_');
                string merge_texture_path = MergePathConfig.GetMergeTexturePath(MergeUtils.GetCurrentSceneName());
                string texture_path = string.Format("{0}/mergetexture_{1}.png", merge_texture_path, s_ext);
                texture_path = MergeUtils.__SaveTexture2Png(mergeTex, texture_path);
                if (string.IsNullOrEmpty(texture_path)) {
                    return null;
                }
                UnityEngine.Object.DestroyImmediate(mergeTex);
                mergeTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texture_path);
#endif
            }

            for (int i = 0; i < instTexs.Count; ++i) {
#if UNITY_EDITOR
                UnityEngine.Object.DestroyImmediate(instTexs[i]);
#else
                UnityEngine.Object.Destroy(instTexs[i]);
#endif
            }
            mergeTexs.Clear();

            TS.EndSample();
            return mergeTex;
        }

        public Material CreateMaterial(string shader_name) {
            TS.BeginSample("CreateMergeMat " + shader_name);
            Material mergeMat = new Material(Shader.Find(shader_name));
            if (mergeMat == null) {
                UnityEngine.Debug.LogError(string.Format("mergeMat create failed, shader: {0}", shader_name));
                return null;
            }
            string s_ext = shader_name.Replace('/', '_');
            mergeMat.name = string.Format("mergeMat_{0}", s_ext);

            if (bSaveAsset) {
#if UNITY_EDITOR
                string merge_material_path = MergePathConfig.GetMergeMatPath(MergeUtils.GetCurrentSceneName());
                string material_path = string.Format("{0}/{1}.mat", merge_material_path, mergeMat.name);
                material_path = MergeUtils.__SaveMaterial(mergeMat, material_path);
                if (string.IsNullOrEmpty(material_path)) {
                    UnityEngine.Debug.LogError("Save MergeMat Failed");
                    return null;
                }
#endif
            }
            TS.EndSample();
            return mergeMat;
        }

        public int[] CovertToColorSize(int width, int height) {
            int[] ret = new int[2];
            ret[0] = height / 32;
            ret[1] = width / 32;

            return ret;
        }
    }
}
