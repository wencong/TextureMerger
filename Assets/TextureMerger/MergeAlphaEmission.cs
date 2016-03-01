using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityTM;


public class MergeAlphaEmission : MergeBase {
    public MergeAlphaEmission(string shader_name)
        : base(shader_name) {

    }
    public override bool MergeMaterialsWithSameShader(Material[] mats, string shader_name, ref Material mergeMat, ref Rect[] rects) {

        try {
            List<Texture2D> list_main_texture = new List<Texture2D>();
            List<Texture2D> list_emission_texture = new List<Texture2D>();

            mergeMat = CreateMaterial(shader_name);
            if (mergeMat == null) {
                Debug.LogError(string.Format("mergeMat create failed, shader {0}", shader_name));
                return false;
            }

            for (int i = 0; i < mats.Length; ++i) {
                Material mat = mats[i];
                list_main_texture.Add(mat.GetTexture("_MainTex") as Texture2D);
                list_emission_texture.Add(mat.GetTexture("_LightCtrl") as Texture2D);
            }

            //MergeUtils.SetTexturesReadable(list_main_texture, true);
            Texture2D mergeTex = MergeTextures(list_main_texture, ref rects, shader_name + "_MainTex");
            if (rects == null) {
                return false;
            }
            mergeMat.SetTexture("_MainTex", mergeTex);
            //MergeUtils.SetTexturesReadable(list_main_texture, false);

            Rect[] rs = new Rect[1];
            //MergeUtils.SetTexturesReadable(list_emission_texture, true);
            Texture2D mergeEmissTex = MergeTextures(list_emission_texture, ref rs, shader_name + "_EmissionTex");
            if (rs == null) {
                return false;
            }

            mergeMat.SetTexture("_LightCtrl", mergeEmissTex);
            //MergeUtils.SetTexturesReadable(list_emission_texture, false);

            return true;
        }
        catch (Exception e) {
            Debug.LogException(e);
            return false;
        }
    }
}

