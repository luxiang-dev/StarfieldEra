using System;
using UnityEditor;
using UnityEngine;
namespace StarfieldEra.Editor
{
    public static class LocalEnvironmentSetup
    {
        const string Folder="Assets/StarfieldEra/Environment/LocalModels";
        [MenuItem("StarfieldEra/Set up local environment models")]
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play before importing environment models.");
            Ensure("Assets/StarfieldEra/Resources/Environment");
            foreach(var name in new[]{"PineTree","ResidentHouse"})
            {
                string path=Folder+"/"+name+".fbx";var importer=AssetImporter.GetAtPath(path) as ModelImporter;
                if(!importer)throw new InvalidOperationException("Missing converted model: "+path);
                importer.importAnimation=false;importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.isReadable=false;importer.meshCompression=ModelImporterMeshCompression.Low;importer.SaveAndReimport();
                string texturePath=Folder+"/"+name+"_BaseColor.png";var textureImporter=(TextureImporter)AssetImporter.GetAtPath(texturePath);
                textureImporter.maxTextureSize=2048;textureImporter.mipmapEnabled=true;textureImporter.sRGBTexture=true;textureImporter.textureCompression=TextureImporterCompression.Compressed;textureImporter.SaveAndReimport();
                string matPath=Folder+"/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if(!material){material=new Material(Shader.Find("StarfieldEra/Storybook Models"));AssetDatabase.CreateAsset(material,matPath);}
                material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));material.SetColor("_BaseColor",Color.white);material.enableInstancing=true;EditorUtility.SetDirty(material);
                var root=new GameObject(name);
                try
                {
                    var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),root.transform);
                    foreach(var r in model.GetComponentsInChildren<Renderer>())r.sharedMaterial=material;
                    var renderers=model.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
                    float factor=1/Mathf.Max(.001f,bounds.size.y);model.transform.localScale*=factor;model.transform.position+=new Vector3(-bounds.center.x,-bounds.min.y,-bounds.center.z)*factor;
                    PrefabUtility.SaveAsPrefabAsset(root,"Assets/StarfieldEra/Resources/Environment/"+name+".prefab");
                    Debug.Log(name+" prepared; source bounds "+bounds.size+", normalized height 1m");
                }
                finally{UnityEngine.Object.DestroyImmediate(root);}
            }
            AssetDatabase.SaveAssets();
        }
        static void Ensure(string folder)
        {if(AssetDatabase.IsValidFolder(folder))return;int slash=folder.LastIndexOf('/');Ensure(folder.Substring(0,slash));AssetDatabase.CreateFolder(folder.Substring(0,slash),folder.Substring(slash+1));}
    }
}
