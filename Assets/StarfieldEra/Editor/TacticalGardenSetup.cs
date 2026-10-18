using UnityEditor;
using UnityEngine;
namespace StarfieldEra.Editor
{
    public static class TacticalGardenSetup
    {
        const string Folder="Assets/StarfieldEra/Environment/TacticalGarden";
        static Material Mat(string name,Color color)
        {
            string path=Folder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m){m=new Material(Shader.Find("StarfieldEra/Storybook Models"));AssetDatabase.CreateAsset(m,path);}m.SetColor("_BaseColor",color);m.enableInstancing=true;EditorUtility.SetDirty(m);return m;
        }
        static GameObject Shape(Transform root,string name,Vector3 p,Vector3 size,Material m,PrimitiveType type=PrimitiveType.Sphere)
        {var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(root,false);go.transform.localPosition=p;go.transform.localScale=size;Object.DestroyImmediate(go.GetComponent<Collider>());go.GetComponent<Renderer>().sharedMaterial=m;return go;}
        [System.Serializable] sealed class Entry {public string name,texture;}
        [System.Serializable] sealed class Report {public Entry[] materials;}
        public static void ImportWebMine()
        {
            const string folder="Assets/StarfieldEra/Environment/GeneratedGarden/PotatoMine";
            string path=folder+"/PotatoMine.fbx";
            AssetDatabase.Refresh();var importer=(ModelImporter)AssetImporter.GetAtPath(path);
            importer.importAnimation=false;importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.meshCompression=ModelImporterMeshCompression.Low;importer.isReadable=false;importer.addCollider=false;importer.SaveAndReimport();
            var report=JsonUtility.FromJson<Report>(System.IO.File.ReadAllText(folder+"/ImportReport.json"));var materials=new Material[report.materials.Length];
            for(int i=0;i<materials.Length;i++)
            {
                string tex=folder+"/"+report.materials[i].texture;var ti=(TextureImporter)AssetImporter.GetAtPath(tex);ti.maxTextureSize=1024;ti.mipmapEnabled=true;ti.textureCompression=TextureImporterCompression.Compressed;ti.SaveAndReimport();
                string matPath=folder+"/PotatoMine"+i+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(matPath);if(!m){m=new Material(Shader.Find("StarfieldEra/Storybook Models"));AssetDatabase.CreateAsset(m,matPath);}m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(tex));m.SetColor("_BaseColor",Color.white);m.enableInstancing=true;EditorUtility.SetDirty(m);materials[i]=m;
            }
            var root=new GameObject("PotatoMine");
            try
            {
                var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),root.transform);
                foreach(var r in model.GetComponentsInChildren<Renderer>()){var mapped=r.sharedMaterials;for(int i=0;i<mapped.Length;i++)mapped[i]=materials[Mathf.Min(i,materials.Length-1)];r.sharedMaterials=mapped;}
                var renderers=model.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);float scale=.85f/Mathf.Max(.001f,bounds.size.y);model.transform.localScale*=scale;model.transform.localPosition=new Vector3(-bounds.center.x,-bounds.min.y,-bounds.center.z)*scale;
                var prefab=PrefabUtility.SaveAsPrefabAsset(root,"Assets/StarfieldEra/Resources/Environment/PotatoMine.prefab");var library=AssetDatabase.LoadAssetAtPath<GardenArtLibrary>("Assets/StarfieldEra/Resources/GardenDefenseArt.asset");library.potatoMine=prefab;EditorUtility.SetDirty(library);AssetDatabase.SaveAssets();
            }
            finally{Object.DestroyImmediate(root);}
        }
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Stop Play before building art.");
            System.IO.Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            var shell=Mat("Honey walnut",new Color(.67f,.39f,.15f));var groove=Mat("Walnut grooves",new Color(.36f,.19f,.07f));var potato=Mat("Potato skin",new Color(.72f,.53f,.29f));var soil=Mat("Garden soil",new Color(.27f,.19f,.105f));var white=Mat("Ivory eyes",new Color(1,.97f,.85f));var ink=Mat("Dark eyes",new Color(.055f,.065f,.045f));var red=Mat("Mine red cap",new Color(.87f,.15f,.06f));var steel=Mat("Mine stem",new Color(.37f,.45f,.4f));
            var library=AssetDatabase.LoadAssetAtPath<GardenArtLibrary>("Assets/StarfieldEra/Resources/GardenDefenseArt.asset");
            foreach(bool nut in new[]{true,false})
            {
                var root=new GameObject(nut?"Walnut":"PotatoMine");
                try
                {
                    Shape(root.transform,"Earth mound",new Vector3(0,.04f,0),new Vector3(nut?1.4f:1.2f,.12f,nut?1:1.05f),soil);
                    Shape(root.transform,nut?"Walnut shell":"Potato body",new Vector3(0,nut?.77f:.22f,0),nut?new Vector3(1.22f,1.5f,.95f):new Vector3(1,.48f,.85f),nut?shell:potato);
                    for(int i=0;i<(nut?10:6);i++)
                    {
                        float a=i*2.39996f;var p=new Vector3(Mathf.Cos(a)*(nut?.48f:.5f),.08f,Mathf.Sin(a)*.38f);Shape(root.transform,"Soil pebble",p,new Vector3(.15f,.12f,.14f),soil);
                        if(nut){var ridge=Shape(root.transform,"Shell fold",new Vector3(Mathf.Cos(a)*.5f,.8f,Mathf.Sin(a)*.34f),new Vector3(.055f,1.08f,.045f),groove);ridge.transform.localRotation=Quaternion.Euler(Mathf.Sin(a)*12,0,Mathf.Cos(a)*9);}
                    }
                    foreach(int side in new[]{-1,1})
                    {
                        var p=new Vector3(side*(nut?.23f:.2f),nut?.98f:.31f,nut?.42f:.36f);
                        Shape(root.transform,"Eye",p,nut?new Vector3(.29f,.36f,.12f):new Vector3(.22f,.22f,.09f),white);
                        Shape(root.transform,"Pupil",p+new Vector3(0,-.02f,.065f),new Vector3(.1f,nut?.18f:.11f,.055f),ink);
                        Shape(root.transform,"Eye glint",p+new Vector3(-.025f,.035f,.1f),Vector3.one*.035f,white);
                    }
                    Shape(root.transform,"Small mouth",new Vector3(0,nut?.61f:.16f,nut?.46f:.4f),new Vector3(.22f,.055f,.04f),ink);
                    if(!nut){Shape(root.transform,"Detonator stem",new Vector3(0,.55f,0),new Vector3(.065f,.45f,.065f),steel,PrimitiveType.Cylinder);Shape(root.transform,"Red trigger cap",new Vector3(0,.79f,0),new Vector3(.29f,.18f,.29f),red);}
                    var prefab=PrefabUtility.SaveAsPrefabAsset(root,"Assets/StarfieldEra/Resources/Environment/"+(nut?"Walnut":"PotatoMine")+".prefab");
                    if(nut)library.walnut=prefab;else if(!library.potatoMine)library.potatoMine=prefab;
                }
                finally{Object.DestroyImmediate(root);}
            }
            EditorUtility.SetDirty(library);AssetDatabase.SaveAssets();
        }
    }
}
