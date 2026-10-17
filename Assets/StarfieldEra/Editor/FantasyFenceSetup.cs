using System;
using UnityEditor;
using UnityEngine;
namespace StarfieldEra.Editor
{
    public static class FantasyFenceSetup
    {
        [MenuItem("StarfieldEra/Import painted fantasy fence")]
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play before importing the fence.");
            const string folder="Assets/StarfieldEra/Environment/LocalModels/";
            var importer=(ModelImporter)AssetImporter.GetAtPath(folder+"FantasyFence.fbx");
            importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.importAnimation=false;importer.meshCompression=ModelImporterMeshCompression.Low;importer.SaveAndReimport();
            var texture=(TextureImporter)AssetImporter.GetAtPath(folder+"FantasyFence_BaseColor.png");texture.maxTextureSize=2048;texture.mipmapEnabled=true;texture.sRGBTexture=true;texture.textureCompression=TextureImporterCompression.Compressed;texture.SaveAndReimport();
            var mat=AssetDatabase.LoadAssetAtPath<Material>(folder+"FantasyFence.mat");
            if(!mat){mat=new Material(Shader.Find("StarfieldEra/Storybook Models"));AssetDatabase.CreateAsset(mat,folder+"FantasyFence.mat");}
            mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"FantasyFence_BaseColor.png"));mat.SetColor("_BaseColor",Color.white);mat.enableInstancing=true;EditorUtility.SetDirty(mat);
            var root=new GameObject("FantasyFence");
            try
            {
                var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(folder+"FantasyFence.fbx"),root.transform);
                model.transform.localRotation=Quaternion.Euler(0,180,0);
                foreach(var r in model.GetComponentsInChildren<Renderer>()){r.sharedMaterial=mat;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;r.receiveShadows=true;}
                PrefabUtility.SaveAsPrefabAsset(root,"Assets/StarfieldEra/Resources/Environment/FantasyFence.prefab");
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
            AssetDatabase.SaveAssets();
        }
    }
}
