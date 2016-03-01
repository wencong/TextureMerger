using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityTM;

public class MergeAlphaReflection : MergeBase{
    public MergeAlphaReflection(string shader_name)
        : base(shader_name) {
    }

    public override bool MergeMaterialsWithSameShader(Material[] mats, string shader_name, ref Material mergeMat, ref Rect[] rects) {
            try {
            List<Texture2D> list_main_texture = new List<Texture2D>();
            List<Texture2D> list_reflect_texture = new List<Texture2D>();
            Cubemap cubeEnvMap = null;
            List<Texture2D> list_reflect_color_texture = new List<Texture2D>();

            mergeMat = CreateMaterial(shader_name + "_Merge");
            if (mergeMat == null) {
                Debug.LogError(string.Format("mergeMat create failed, shader: {0}", shader_name));
                return false;
            }

            for (int i = 0; i < mats.Length; ++i) {
                Material mat = mats[i];
                Texture2D mainTex = mat.GetTexture("_MainTex") as Texture2D;
                list_main_texture.Add(mainTex);

                list_reflect_texture.Add(mat.GetTexture("_LightCtrl") as Texture2D);
                if (cubeEnvMap == null) {
                    cubeEnvMap = mat.GetTexture("_EnvReflection") as Cubemap;
                }
                Color reflectColor = mat.GetColor("_EnvReflectionColorR");

                int[] size = CovertToColorSize(mainTex.width, mainTex.height);
                int length = size[0] * size[1];
                Texture2D color_tex = new Texture2D(size[0], size[1], TextureFormat.RGBA32, false);
                Color[] pixels = new Color[size[0] * size[1]];
                for (int j = 0; j < length; ++j) {
                    pixels[j] = reflectColor;
                }
                color_tex.SetPixels(pixels);
                list_reflect_color_texture.Add(color_tex);
            }

            //MergeUtils.SetTexturesReadable(list_main_texture, true);
            Texture2D mergeTexMain = MergeTextures(list_main_texture, ref rects, shader_name + "_Main");
            if (rects == null) {
                return false;
            }
            mergeMat.SetTexture("_MainTex", mergeTexMain);
            //MergeUtils.SetTexturesReadable(list_main_texture, false);

            Rect[] r = new Rect[1];
            //MergeUtils.SetTexturesReadable(list_reflect_texture, true);
            Texture2D mergeTexReflect = MergeTextures(list_reflect_texture, ref r, shader_name + "_Reflect");
            if (r == null) {
                return false;
            }
            mergeMat.SetTexture("_LightCtrl", mergeTexReflect);
            //MergeUtils.SetTexturesReadable(list_reflect_texture, false);

            mergeMat.SetTexture("_EnvReflection", cubeEnvMap);

            Texture2D mergeTexColor = MergeTextures(list_reflect_color_texture, ref r, shader_name + "_Color", 1024, true);
            if (r == null) {
                return false;
            }
            mergeMat.SetTexture("_EnvColorTex", mergeTexColor);

            return true;
        }
        catch (Exception e) {
            Debug.LogException(e);
            return false;
        }
    }
}

