using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace StarfieldEra.Editor
{
    public static class GeneratedGardenSetup
    {
        [Serializable] sealed class MaterialEntry { public string name,texture; }
        [Serializable] sealed class Report { public MaterialEntry[] materials; }
        [MenuItem("StarfieldEra/Import generated garden models")]
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play before importing models.");
            var library=AssetDatabase.LoadAssetAtPath<GardenArtLibrary>("Assets/StarfieldEra/Resources/GardenDefenseArt.asset");
            foreach(var name in new[]{"TwinPea","IcePea","ConeZombie","BucketZombie","BruteZombie","Chomper","CampLantern"})
            {
                string folder="Assets/StarfieldEra/Environment/GeneratedGarden/"+name,path=folder+"/"+name+".fbx";
                if(!File.Exists(path))throw new FileNotFoundException("Generated model is missing",path);
                bool zombie=name.EndsWith("Zombie");
                var importer=(ModelImporter)AssetImporter.GetAtPath(path);importer.importAnimation=false;importer.animationType=zombie?ModelImporterAnimationType.Generic:ModelImporterAnimationType.None;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.meshCompression=ModelImporterMeshCompression.Low;importer.isReadable=false;importer.addCollider=false;importer.optimizeGameObjects=false;importer.SaveAndReimport();
                var report=JsonUtility.FromJson<Report>(File.ReadAllText(folder+"/ImportReport.json"));
                var materials=report.materials.Select((entry,i)=>
                {
                    string texture=folder+"/"+entry.texture;var ti=(TextureImporter)AssetImporter.GetAtPath(texture);ti.maxTextureSize=2048;ti.textureCompression=TextureImporterCompression.Compressed;ti.mipmapEnabled=true;ti.SaveAndReimport();
                    string matPath=folder+"/"+name+i+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);if(!mat){mat=new Material(Shader.Find("StarfieldEra/Storybook Models"));AssetDatabase.CreateAsset(mat,matPath);}mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texture));mat.SetColor("_BaseColor",Color.white);mat.enableInstancing=true;EditorUtility.SetDirty(mat);return mat;
                }).ToArray();
                var root=new GameObject(name);
                try
                {
                    var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),root.transform);model.name=name+" supplied model";
                    var renderers=model.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;
                    foreach(var r in renderers){bounds.Encapsulate(r.bounds);r.sharedMaterials=r.sharedMaterials.Select((_,i)=>materials[Mathf.Min(i,materials.Length-1)]).ToArray();}
                    float height=name=="CampLantern"?2.8f:name=="BruteZombie"?2.35f:zombie?1.9f:name=="Chomper"?1.8f:1.65f;float scale=height/Mathf.Max(.001f,bounds.size.y);model.transform.localScale*=scale;model.transform.localPosition=new Vector3(-bounds.center.x,-bounds.min.y,-bounds.center.z)*scale;
                    if(zombie&&!model.GetComponent<Animator>())model.AddComponent<Animator>();
                    var prefab=PrefabUtility.SaveAsPrefabAsset(root,"Assets/StarfieldEra/Resources/Environment/"+name+".prefab");
                    if(zombie)
                    {
                        int index=name=="ConeZombie"?0:name=="BucketZombie"?1:2;
                        library.variantWalk[index]=GardenAnimationRetarget.Bake(library.zombie,prefab,library.walk,folder+"/Walk.anim");
                        library.variantDismay[index]=GardenAnimationRetarget.Bake(library.zombie,prefab,library.dismay,folder+"/Dismay.anim");
                    }
                    if(name=="TwinPea")library.twinPea=prefab;else if(name=="IcePea")library.icePea=prefab;else if(name=="ConeZombie")library.coneZombie=prefab;else if(name=="BucketZombie")library.bucketZombie=prefab;else if(name=="BruteZombie")library.bruteZombie=prefab;else if(name=="Chomper")library.chomper=prefab;else library.lantern=prefab;
                }
                finally{UnityEngine.Object.DestroyImmediate(root);}
            }
            EditorUtility.SetDirty(library);AssetDatabase.SaveAssets();Debug.Log("Generated garden models: 7 prefabs ready.");
        }
    }
}
