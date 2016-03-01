#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;
using System.Text;

namespace UnityTM {
    public static class MeshExporterEx {
        private static string MeshToString(MeshFilter mf) {
            try {
                if (mf == null || !mf.sharedMesh) return null;
                Mesh m = mf.sharedMesh;
                Material[] mats = mf.GetComponent<Renderer>().sharedMaterials;

                StringBuilder sb = new StringBuilder();
                sb.Append("g ").Append(mf.name).Append("\n");
                foreach (Vector3 lv in m.vertices) {
                    //Vector3 wv = mf.transform.TransformPoint(lv);
                    sb.Append(string.Format("v {0} {1} {2}\n", -lv.x, lv.y, lv.z));
                }
                sb.Append("\n");

                foreach (Vector3 lv in m.normals) {
                    //Vector3 wv = mf.transform.TransformDirection(lv);
                    sb.Append(string.Format("vn {0} {1} {2}\n", -lv.x, lv.y, lv.z));
                }
                sb.Append("\n");

                foreach (Vector3 v in m.uv) {
                    sb.Append(string.Format("vt {0} {1}\n", v.x, v.y));
                }

                for (int material = 0; material < m.subMeshCount; material++) {
                    sb.Append("\n");
                    sb.Append("usemtl ").Append(mats[material].name).Append("\n");
                    sb.Append("usemap ").Append(mats[material].name).Append("\n");

                    int[] triangles = m.GetTriangles(material);
                    for (int i = 0; i < triangles.Length; i += 3) {
                        //Because we inverted the x-component, we also needed to alter the triangle winding.
                        sb.Append(string.Format("f {1}/{1}/{1} {0}/{0}/{0} {2}/{2}/{2}\n",
                            triangles[i] + 1, triangles[i + 1] + 1, triangles[i + 2] + 1));
                    }
                }
                return sb.ToString();
            }
            catch (System.Exception ex) {
                Debug.LogException(ex);
                return null;
            }

        }

        public static void ToObjFileEx(this MeshFilter mf, string filename) {
            using (StreamWriter sw = new StreamWriter(filename)) {
                sw.Write(MeshToString(mf));
            }
        }

        public static void BakeMesh(GameObject go, bool useIt) {
            if (go == null) return;
            if (!AssetDatabase.Contains(go)) {
                go = PrefabUtility.GetPrefabParent(go) as GameObject;
                if (go == null) {
                    Debug.LogWarning(go.name + "is not an prefab!");
                    return;
                }
            }
            //save mesh
            MeshFilter mf = go.GetComponent<MeshFilter>();
            if (mf == null) {
                Debug.LogWarning(string.Format("{0} has no MeshFilter!", go.name));
                return;
            }

            string path = AssetDatabase.GetAssetPath(go);
            path = path.Replace(".prefab", "Mesh.obj");
            mf.ToObjFileEx(path);
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            if (useIt) {
                Mesh mesh = AssetDatabase.LoadAssetAtPath(path, typeof(Mesh)) as Mesh;
                Mesh oldMesh = mf.sharedMesh;
                if (mesh != null) mf.sharedMesh = mesh;
                if (oldMesh != null) UnityEngine.Object.DestroyImmediate(oldMesh, true);
                AssetDatabase.SaveAssets();

            }
        }

        public static void BakeMesh(GameObject go, string path, bool useIt) {
            if (go == null || !path.StartsWith("Assets")) return;
            //save mesh
            //MeshFilter mf = go.GetComponentInChildren<MeshFilter>();
            MeshFilter mf = go.GetComponent<MeshFilter>();
            if (mf == null) return;

            mf.ToObjFileEx(path);
            //AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);

            ModelImporter mI = AssetImporter.GetAtPath(path) as ModelImporter;
            mI.importMaterials = false;
            mI.importAnimation = false;
            mI.generateSecondaryUV = true;
            mI.animationType = ModelImporterAnimationType.None;

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);

            if (useIt) {
                Mesh mesh = AssetDatabase.LoadAssetAtPath(path, typeof(Mesh)) as Mesh;
                if (mesh != null) mf.sharedMesh = mesh;
            }
        }
    }
}

#endif