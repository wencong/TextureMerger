using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityTM;

public class MergeTransCutoutDiffuse : MergeBase{
    public MergeTransCutoutDiffuse(string shader_name)
        : base(shader_name) {

    }

    public override bool MergeMaterialsWithSameShader(Material[] mats, string shader_name, ref Material mergeMat, ref Rect[] rect) {
        
        try {
            List<Texture2D> list_main_texture = new List<Texture2D>();

            mergeMat = CreateMaterial(shader_name);
            if (mergeMat == null) {
                Debug.LogError(string.Format("mergeMat create failed, shader: {0}", shader_name));
                return false;
            }

            for (int i = 0; i < mats.Length; ++i) {
                Material mat = mats[i];
                list_main_texture.Add(mat.mainTexture as Texture2D);
            }

            //MergeUtils.SetTexturesReadable(list_main_texture, true);
            Texture2D mergeTex = MergeTextures(list_main_texture, ref rect, shader_name);
            if (rect == null) {
                return false;
            }
            mergeMat.mainTexture = mergeTex;
            //MergeUtils.SetTexturesReadable(list_main_texture, false);

            return true;
        }
        catch (Exception e) {
            Debug.LogException(e);
            return false;
        }
    }
}

