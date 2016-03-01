using UnityEngine;
using System.Collections;
using UnityTM;

public class TextureMerge {
    public static void Merge(Transform trans) {
        TextureMerger.Instance().Init("");

        TextureMerger.Instance().AddMergeShader(new MergeDiffuse("Mobile/Diffuse"));
        TextureMerger.Instance().AddMergeShader(new MergeTransCutoutDiffuse("Legacy Shaders/Transparent/Cutout/Diffuse"));
        TextureMerger.Instance().AddMergeShader(new MergeAlphaEmission("Kingsoft/Scene/Special/AlphaEmission"));
        TextureMerger.Instance().AddMergeShader(new MergeAlphaReflection("Kingsoft/Scene/AlphaReflection"));
        TextureMerger.Instance().AddMergeShader(new MergeOpaqueReflection("Kingsoft/Scene/OpaqueReflection"));

        if (trans != null) {
            TextureMerger.Instance().MergeRT(trans);
        }
        Resources.UnloadUnusedAssets();
    }
}
