using UnityEditor;
using UnityEngine;
namespace StarfieldEra.Editor
{
    public static class GardenIconBaker
    {
        public static void Bake(int only=-1)
        {
            var library=AssetDatabase.LoadAssetAtPath<GardenArtLibrary>("Assets/StarfieldEra/Resources/GardenDefenseArt.asset");
            var rig=new GameObject("Garden icon studio");var cameraRoot=new GameObject("Icon camera");var camera=cameraRoot.AddComponent<Camera>();
            var lightRoot=new GameObject("Icon light");var light=lightRoot.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.4f;light.cullingMask=1<<31;lightRoot.transform.rotation=Quaternion.Euler(40,-35,0);
            var rt=new RenderTexture(192,192,24,RenderTextureFormat.ARGB32);camera.targetTexture=rt;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;camera.orthographic=true;camera.cullingMask=1<<31;camera.nearClipPlane=.05f;camera.farClipPlane=30;
            var previous=RenderTexture.active;var fog=Shader.GetGlobalVector("_StarfieldWeatherFog");var vision=Shader.GetGlobalVector("_StarfieldExplorationVision");float night=Shader.GetGlobalFloat("_StarfieldNight");
            try
            {
                Shader.SetGlobalVector("_StarfieldWeatherFog",Vector4.zero);Shader.SetGlobalVector("_StarfieldExplorationVision",Vector4.zero);Shader.SetGlobalFloat("_StarfieldNight",0);
                for(int kind=0;kind<9;kind++)
                {
                    if(only>=0&&kind!=only)continue;
                    if(kind==2||!library.Unit(kind))continue;
                    var model=Object.Instantiate(library.Unit(kind),rig.transform);model.transform.position=Vector3.right*10000;
                    foreach(var t in model.GetComponentsInChildren<Transform>())t.gameObject.layer=31;
                    var renderers=model.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
                    float size=Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z);camera.orthographicSize=size*.72f;cameraRoot.transform.position=bounds.center+new Vector3(1.6f,1.1f,3)*size;cameraRoot.transform.LookAt(bounds.center);camera.Render();RenderTexture.active=rt;
                    var texture=new Texture2D(192,192,TextureFormat.RGBA32,false);texture.ReadPixels(new Rect(0,0,192,192),0,0);texture.Apply();string path="Assets/StarfieldEra/Resources/UI/Garden"+kind+".png";System.IO.File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);Object.DestroyImmediate(model);AssetDatabase.ImportAsset(path);
                    var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.maxTextureSize=256;importer.SaveAndReimport();library.unitIcons[kind]=AssetDatabase.LoadAssetAtPath<Sprite>(path);
                }
                EditorUtility.SetDirty(library);AssetDatabase.SaveAssets();
            }
            finally{RenderTexture.active=previous;camera.targetTexture=null;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(cameraRoot);Object.DestroyImmediate(rig);Object.DestroyImmediate(lightRoot);Shader.SetGlobalVector("_StarfieldWeatherFog",fog);Shader.SetGlobalVector("_StarfieldExplorationVision",vision);Shader.SetGlobalFloat("_StarfieldNight",night);}
        }
    }
}
