using System;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace StarfieldEra.Editor
{
    public static class WeatherSetup
    {
        const string Folder="Assets/StarfieldEra/Resources/Weather";
        const int Size=256;
        [MenuItem("StarfieldEra/Set up weather data and icons")]
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play to create weather assets.");
            Directory.CreateDirectory(Folder);
            foreach(WeatherType type in Enum.GetValues(typeof(WeatherType)))
            {
                foreach(bool sketch in new[]{false,true})
                {
                    string path=Folder+"/"+type+(sketch?"Sketch":"Color")+".png";
                    var texture=Draw(type,sketch);File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);
                    AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
                    var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
                }
                string assetPath=Folder+"/"+type+".asset";var data=AssetDatabase.LoadAssetAtPath<WeatherDataSO>(assetPath);
                if(!data){data=ScriptableObject.CreateInstance<WeatherDataSO>();WeatherCatalog.Configure(data,type);AssetDatabase.CreateAsset(data,assetPath);}
                data.fullColorIcon=AssetDatabase.LoadAssetAtPath<Sprite>(Folder+"/"+type+"Color.png");data.sketchIcon=AssetDatabase.LoadAssetAtPath<Sprite>(Folder+"/"+type+"Sketch.png");EditorUtility.SetDirty(data);
            }
            AssetDatabase.SaveAssets();
        }
        static Texture2D Draw(WeatherType type,bool sketch)
        {
            var pixels=new Color[Size*Size];var ink=new Color(.30f,.28f,.23f,.95f);
            void Disk(float x,float y,float radius,Color color,bool outline=false)
            {
                for(int py=Mathf.Max(0,(int)(y-radius-2));py<Mathf.Min(Size,y+radius+2);py++)for(int px=Mathf.Max(0,(int)(x-radius-2));px<Mathf.Min(Size,x+radius+2);px++)
                {
                    float distance=Vector2.Distance(new Vector2(px,py),new Vector2(x,y));float a=outline?Mathf.Clamp01(2.2f-Mathf.Abs(distance-radius)):Mathf.Clamp01(radius+.5f-distance);
                    if(a>0){var c=color;c.a*=a;int index=py*Size+px;pixels[index]=Color.Lerp(pixels[index],c,c.a);}
                }
            }
            void Line(float x,float y,float a,float b,Color color,float width=3)
            {int count=Mathf.CeilToInt(Vector2.Distance(new Vector2(x,y),new Vector2(a,b))*1.4f);for(int i=0;i<=count;i++){float t=count==0?0:(float)i/count;Disk(Mathf.Lerp(x,a,t),Mathf.Lerp(y,b,t),width*.5f,color);}}
            bool sunny=type==WeatherType.Sunny,cloudy=type==WeatherType.Cloudy;
            if(sunny||cloudy)
            {
                float cx=sunny?128:88,cy=sunny?135:172,r=sunny?49:35;
                var gold=new Color(1,.69f,.19f);Disk(cx,cy,r,sketch?ink:gold,sketch);
                for(int i=0;i<12;i++){float angle=i*Mathf.PI/6;Line(cx+Mathf.Cos(angle)*(r+11),cy+Mathf.Sin(angle)*(r+11),cx+Mathf.Cos(angle)*(r+26),cy+Mathf.Sin(angle)*(r+26),sketch?ink:gold,sketch?2:5);}
                if(sketch)for(int i=0;i<7;i++)Line(cx-r*.65f+i*9,cy-r*.5f,cx-r*.35f+i*9,cy+r*.5f,new Color(.38f,.34f,.27f,.27f),1);
            }
            if(!sunny)
            {
                var color=sketch?ink:type>=WeatherType.HeavyRain?new Color(.31f,.41f,.50f):new Color(.77f,.83f,.82f);
                // Overlapping round strokes retain the loose contours of the handbook's ornaments.
                if(!sketch)
                {Disk(74,132,29,color);Disk(111,154,39,color);Disk(155,153,36,color);Disk(184,130,28,color);for(int y=103;y<139;y++)Line(74,y,184,y,color,2);Line(78,113,180,113,new Color(.6f,.7f,.74f,.8f),6);}
                else
                {
                    bool Inside(float x,float y)=>Vector2.Distance(new Vector2(x,y),new Vector2(74,132))<=29||Vector2.Distance(new Vector2(x,y),new Vector2(111,154))<=39||Vector2.Distance(new Vector2(x,y),new Vector2(155,153))<=36||Vector2.Distance(new Vector2(x,y),new Vector2(184,130))<=28||(x>=74&&x<=184&&y>=103&&y<=139);
                    for(int y=99;y<196;y++)for(int x=42;x<216;x++)if(Inside(x,y))pixels[y*Size+x]=!Inside(x-2,y)||!Inside(x+2,y)||!Inside(x,y-2)||!Inside(x,y+2)?ink:Color.clear;
                    for(int i=0;i<9;i++)Line(80+i*11,113,89+i*11,125,new Color(.36f,.32f,.26f,.32f),1);
                }
                if(!cloudy)
                {
                    int count=type==WeatherType.LightRain?3:5;
                    for(int i=0;i<count;i++){float x=78+i*(100f/(count-1));Line(x+7,88,x-4,62,sketch?ink:new Color(.23f,.58f,.78f),sketch?2:5);if(type>=WeatherType.HeavyRain)Line(x+2,48,x-4,32,sketch?ink:new Color(.27f,.61f,.80f),sketch?2:4);}
                }
                if(type==WeatherType.Thunderstorm)
                {var colorBolt=sketch?ink:new Color(1,.77f,.23f);Line(139,112,121,83,colorBolt,sketch?4:11);Line(121,83,145,83,colorBolt,sketch?4:11);Line(145,83,124,53,colorBolt,sketch?4:11);}
            }
            var texture=new Texture2D(Size,Size,TextureFormat.RGBA32,false);texture.SetPixels(pixels);texture.Apply();return texture;
        }
    }
}
