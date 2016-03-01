using UnityEngine;
using System.Collections;

public class MergeTest : MonoBehaviour {

	// Use this for initialization
    public bool combineMesh = true;

	void Start () {
        GameObject go = GameObject.Find("Environment/Models");
        if (go != null) {
            TS.BeginSample("Merge Textures");
            TextureMerge.Merge(go.transform);
            TS.EndSample();

            if (combineMesh) {
                TS.BeginSample("CombineMesh WorkWithLightMap");
                //MeshCombine.WorkWithLightMap(go.transform);
                TS.EndSample();
            }
            
            TS.TerminateSample();
        }
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
