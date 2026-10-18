using UnityEditor;
using UnityEngine;
namespace StarfieldEra.Editor
{
    public static class WildernessSetup
    {
        [MenuItem("StarfieldEra/Set up expanded wilderness")]
        public static void Build()
        {
            const string folder="Assets/StarfieldEra/Environment/LocalModels/WolfDen";
            var importer=(ModelImporter)AssetImporter.GetAtPath(folder+"/WolfDen.fbx");
            importer.importAnimation=false;importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.meshCompression=ModelImporterMeshCompression.Low;importer.SaveAndReimport();
            foreach(var path in new[]{folder+"/WolfDen_BaseColor.png","Assets/StarfieldEra/Resources/WildernessArt/WildProps.png"})
            {var tex=(TextureImporter)AssetImporter.GetAtPath(path);tex.alphaIsTransparency=true;tex.maxTextureSize=2048;tex.mipmapEnabled=true;tex.wrapMode=TextureWrapMode.Clamp;tex.textureCompression=TextureImporterCompression.Compressed;tex.SaveAndReimport();}
            var material=AssetDatabase.LoadAssetAtPath<Material>(folder+"/WolfDen.mat");
            if(!material){material=new Material(Shader.Find("StarfieldEra/Storybook Models"));AssetDatabase.CreateAsset(material,folder+"/WolfDen.mat");}
            material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/WolfDen_BaseColor.png"));material.SetColor("_BaseColor",Color.white);EditorUtility.SetDirty(material);
            var root=new GameObject("WolfDen");
            try
            {
                var visual=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(folder+"/WolfDen.fbx"),root.transform);
                var renderers=visual.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var renderer in renderers){bounds.Encapsulate(renderer.bounds);renderer.sharedMaterial=material;}
                float scale=Mathf.Min(5.5f/bounds.size.y,9/Mathf.Max(bounds.size.x,bounds.size.z));visual.transform.localScale*=scale;visual.transform.localPosition=new Vector3(-bounds.center.x,-bounds.min.y,-bounds.center.z)*scale;
                PrefabUtility.SaveAsPrefabAsset(root,"Assets/StarfieldEra/Resources/Environment/WolfDen.prefab");
            }
            finally{Object.DestroyImmediate(root);}
            AssetDatabase.SaveAssets();
        }
    }
}
