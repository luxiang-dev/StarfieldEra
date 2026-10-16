using System;
using UnityEditor;
using UnityEngine;
namespace StarfieldEra.Editor
{
    public static class NightWolfSetup
    {
        const string Folder="Assets/StarfieldEra/Environment/LocalModels/NightWolf";
        [MenuItem("StarfieldEra/Set up night raid wolf")]
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play before importing the wolf.");
            string path=Folder+"/NightWolf.fbx";var importer=AssetImporter.GetAtPath(path) as ModelImporter;if(!importer)throw new InvalidOperationException("Missing wolf model.");
            importer.importAnimation=false;importer.animationType=ModelImporterAnimationType.Generic;importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.meshCompression=ModelImporterMeshCompression.Low;importer.isReadable=false;importer.addCollider=false;importer.SaveAndReimport();
            string texPath=Folder+"/NightWolf_BaseColor.png";var tex=AssetImporter.GetAtPath(texPath) as TextureImporter;tex.maxTextureSize=2048;tex.sRGBTexture=true;tex.mipmapEnabled=true;tex.textureCompression=TextureImporterCompression.Compressed;tex.SaveAndReimport();
            string matPath=Folder+"/NightWolf.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if(!material){material=new Material(Shader.Find("StarfieldEra/Storybook Models"));AssetDatabase.CreateAsset(material,matPath);}
            material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));material.SetColor("_BaseColor",Color.white);material.enableInstancing=true;EditorUtility.SetDirty(material);
            var root=new GameObject("NightWolf");
            try
            {
                var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),root.transform);
                foreach(var renderer in model.GetComponentsInChildren<Renderer>()){renderer.sharedMaterial=material;if(renderer is SkinnedMeshRenderer skin)skin.updateWhenOffscreen=true;}
                root.AddComponent<NightWolfMotion>();
                PrefabUtility.SaveAsPrefabAsset(root,"Assets/StarfieldEra/Resources/Environment/NightWolf.prefab");
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
            AssetDatabase.SaveAssets();Debug.Log("Night raid wolf prefab ready.");
        }
    }
}
