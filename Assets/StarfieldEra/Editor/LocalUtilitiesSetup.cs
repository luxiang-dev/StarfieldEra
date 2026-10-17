using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
namespace StarfieldEra.Editor
{
    public static class LocalUtilitiesSetup
    {
        const string Folder="Assets/StarfieldEra/Environment/LocalModels/Utilities";
        [MenuItem("StarfieldEra/Set up local rabbit, pillar and production buildings")]
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play before importing local models.");
            foreach(var name in new[]{"MeadowRabbit","AncientPillar","Lumberyard","Quarry"})
            {
                string path=Folder+"/"+name+".fbx";
                var importer=(ModelImporter)AssetImporter.GetAtPath(path);if(!importer)throw new InvalidOperationException("Missing model: "+path);
                importer.importAnimation=false;importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.meshCompression=ModelImporterMeshCompression.Low;importer.isReadable=false;importer.addCollider=false;importer.SaveAndReimport();
                string texturePath=Folder+"/"+name+"_BaseColor.png";var texture=(TextureImporter)AssetImporter.GetAtPath(texturePath);texture.maxTextureSize=2048;texture.sRGBTexture=true;texture.mipmapEnabled=true;texture.textureCompression=TextureImporterCompression.Compressed;texture.SaveAndReimport();
                string materialPath=Folder+"/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if(!material){material=new Material(Shader.Find("StarfieldEra/Storybook Models"));AssetDatabase.CreateAsset(material,materialPath);}
                material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));material.SetColor("_BaseColor",Color.white);material.enableInstancing=true;EditorUtility.SetDirty(material);
                var root=new GameObject(name);
                try
                {
                    var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),root.transform);
                    foreach(var renderer in model.GetComponentsInChildren<Renderer>()){renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.On;renderer.receiveShadows=true;}
                    var renderers=model.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
                    float scale=1/Mathf.Max(.001f,bounds.size.y);model.transform.localScale*=scale;model.transform.position+=new Vector3(-bounds.center.x,-bounds.min.y,-bounds.center.z)*scale;
                    if(name=="MeadowRabbit")root.AddComponent<LocalRabbitMotion>();
                    PrefabUtility.SaveAsPrefabAsset(root,"Assets/StarfieldEra/Resources/Environment/"+name+".prefab");
                    Debug.Log(name+" ready: normalized 1 m height, original footprint ratio "+bounds.size);
                }
                finally{UnityEngine.Object.DestroyImmediate(root);}
            }
            AssetDatabase.SaveAssets();
        }
    }
}
