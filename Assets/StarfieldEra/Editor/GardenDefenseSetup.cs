using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace StarfieldEra.Editor
{
    public static class GardenDefenseSetup
    {
        public static void BakeLabels()
        {
            const string path="Assets/StarfieldEra/Art/GardenChinese.asset";
            var fallback=AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(path);
            if(!fallback)
            {
                fallback=TMPro.TMP_FontAsset.CreateFontAsset(AssetDatabase.LoadAssetAtPath<Font>("Assets/StarfieldEra/Art/FarmChinese.otf"),48,5,UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,512,512,TMPro.AtlasPopulationMode.Dynamic);
                fallback.name="GardenChinese";
                if(!fallback.TryAddCharacters("傍僵唤墓墙射尸惊敌畅良豆豌钟闲",out var missing))throw new InvalidOperationException("Missing garden glyphs: "+missing);
                fallback.atlasPopulationMode=TMPro.AtlasPopulationMode.Static;AssetDatabase.CreateAsset(fallback,path);AssetDatabase.AddObjectToAsset(fallback.material,fallback);
                foreach(var texture in fallback.atlasTextures)AssetDatabase.AddObjectToAsset(texture,fallback);
            }
            var main=AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/StarfieldEra/Art/FarmChinese.asset");
            if(main.fallbackFontAssetTable==null)main.fallbackFontAssetTable=new System.Collections.Generic.List<TMPro.TMP_FontAsset>();
            if(!main.fallbackFontAssetTable.Contains(fallback)){main.fallbackFontAssetTable.Add(fallback);EditorUtility.SetDirty(main);}
            AssetDatabase.SaveAssets();
        }
        [MenuItem("StarfieldEra/Set up supplied garden defense models")]
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play before importing garden models.");
            var library=AssetDatabase.LoadAssetAtPath<GardenArtLibrary>("Assets/StarfieldEra/Resources/GardenDefenseArt.asset");
            if(!library){library=ScriptableObject.CreateInstance<GardenArtLibrary>();AssetDatabase.CreateAsset(library,"Assets/StarfieldEra/Resources/GardenDefenseArt.asset");}
            foreach(string name in new[]{"Peashooter","GardenWall","ZombieTomb","Zombie"})
            {
                string folder="Assets/StarfieldEra/Environment/LocalModels/"+name,path=folder+"/"+name+".fbx";
                var importer=(ModelImporter)AssetImporter.GetAtPath(path);importer.importAnimation=name=="Zombie";importer.animationType=ModelImporterAnimationType.Generic;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.meshCompression=ModelImporterMeshCompression.Low;importer.isReadable=false;importer.addCollider=false;importer.optimizeGameObjects=false;importer.SaveAndReimport();
                var texPath=folder+"/"+name+"_BaseColor0.png";var ti=(TextureImporter)AssetImporter.GetAtPath(texPath);ti.maxTextureSize=2048;ti.textureCompression=TextureImporterCompression.Compressed;ti.mipmapEnabled=true;ti.SaveAndReimport();
                var matPath=folder+"/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);if(!mat){mat=new Material(Shader.Find("StarfieldEra/Storybook Models"));AssetDatabase.CreateAsset(mat,matPath);}mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));mat.SetColor("_BaseColor",Color.white);mat.enableInstancing=true;EditorUtility.SetDirty(mat);
                var root=new GameObject(name);
                try
                {
                    var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),root.transform);model.name=name+" supplied model";
                    var renderers=model.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var r in renderers){bounds.Encapsulate(r.bounds);r.sharedMaterials=r.sharedMaterials.Select(_=>mat).ToArray();}
                    float size=name=="GardenWall"?Mathf.Max(bounds.size.x,bounds.size.z):bounds.size.y;float height=name=="Zombie"?1.9f:name=="Peashooter"?1.65f:name=="GardenWall"?3:1.8f;float scale=height/Mathf.Max(.001f,size);model.transform.localScale*=scale;model.transform.localPosition=new Vector3(-bounds.center.x,-bounds.min.y,-bounds.center.z)*scale;
                    if(name=="Zombie")
                    {
                        if(!model.GetComponent<Animator>())model.AddComponent<Animator>();
                        var clips=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
                        library.walk=clips.FirstOrDefault(c=>c.name.Contains("ZombieWalk"));library.dismay=clips.FirstOrDefault(c=>c.name.Contains("ZombieDismay"));
                        if(!library.walk||!library.dismay)throw new InvalidOperationException("Missing supplied zombie actions: "+string.Join(",",clips.Select(c=>c.name)));
                    }
                    var prefab=PrefabUtility.SaveAsPrefabAsset(root,"Assets/StarfieldEra/Resources/Environment/"+name+".prefab");
                    if(name=="Peashooter")library.peashooter=prefab;else if(name=="GardenWall")library.wall=prefab;else if(name=="ZombieTomb")library.tomb=prefab;else library.zombie=prefab;
                }
                finally{UnityEngine.Object.DestroyImmediate(root);}
            }
            BakeLabels();EditorUtility.SetDirty(library);AssetDatabase.SaveAssets();Debug.Log("Supplied garden defense art and both zombie animations are ready.");
        }
    }
}
