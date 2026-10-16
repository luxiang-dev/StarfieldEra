using System;
using UnityEditor;
using UnityEngine;
namespace StarfieldEra.Editor
{
    public static class ResourceModelsSetup
    {
        [Serializable] sealed class MaterialEntry {public string name,texture;}
        [Serializable] sealed class Report {public MaterialEntry[] materials;}
        [MenuItem("StarfieldEra/Set up supplied resource models")]
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play before importing buildings.");
            AssetDatabase.Refresh();
            foreach(var name in new[]{"Warehouse","TownBoard","AnimalTrap","PastureGate"})
            {
                string folder="Assets/StarfieldEra/Environment/LocalModels/"+name,path=folder+"/"+name+".fbx";
                var report=JsonUtility.FromJson<Report>(System.IO.File.ReadAllText(folder+"/ImportReport.json"));
                var importer=(ModelImporter)AssetImporter.GetAtPath(path);
                importer.importAnimation=false;importer.addCollider=false;importer.isReadable=false;importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.meshCompression=ModelImporterMeshCompression.Low;importer.SaveAndReimport();
                foreach(var entry in report.materials)
                {
                    string texturePath=folder+"/"+entry.texture;
                    var textureImporter=(TextureImporter)AssetImporter.GetAtPath(texturePath);textureImporter.maxTextureSize=2048;textureImporter.sRGBTexture=true;textureImporter.mipmapEnabled=true;textureImporter.textureCompression=TextureImporterCompression.Compressed;textureImporter.SaveAndReimport();
                    string matPath=folder+"/"+entry.texture.Replace(".png",".mat");var mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);
                    if(!mat){mat=new Material(Shader.Find("StarfieldEra/Storybook Models"));AssetDatabase.CreateAsset(mat,matPath);}
                    mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));mat.SetColor("_BaseColor",Color.white);mat.enableInstancing=true;EditorUtility.SetDirty(mat);
                    importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),entry.name),mat);
                }
                importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;importer.SaveAndReimport();
                var root=new GameObject(name);
                try
                {
                    var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),root.transform);
                    model.name=name+" model";
                    var renderers=model.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;
                    foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
                    float scale=name=="PastureGate"?1:1/Mathf.Max(.001f,bounds.size.y);model.transform.localScale*=scale;if(name!="PastureGate")model.transform.localPosition=new Vector3(-bounds.center.x,-bounds.min.y,-bounds.center.z)*scale;
                    PrefabUtility.SaveAsPrefabAsset(root,"Assets/StarfieldEra/Resources/Environment/"+name+".prefab");
                }
                finally{UnityEngine.Object.DestroyImmediate(root);}
            }
            AssetDatabase.SaveAssets();
        }
    }
}
