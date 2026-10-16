using System.Collections.Generic;
using UnityEngine;
namespace StarfieldEra
{
    /// <summary>Shared illustrated icons and softly rounded, translucent UI surfaces.</summary>
    public sealed class UITheme:MonoBehaviour
    {
        public static UITheme Current {get;private set;}
        public static readonly Color Gold=new Color(.89f,.75f,.46f),Paper=new Color(.96f,.92f,.81f),Deep=new Color(.035f,.105f,.095f,.90f),Card=new Color(.09f,.22f,.19f,.86f);
        Sprite rounded;Sprite[] navigation,items,buildings,weather;readonly List<Object> owned=new List<Object>();
        public void Initialize()
        {
            Current=this;navigation=Atlas("UI/HandbookIcons",6,4);items=Atlas("UI/InventoryIcons",6,5);buildings=Atlas("UI/BuildingIcons",4,4);weather=Atlas("UI/WeatherIllustrations",5,2);
            var texture=new Texture2D(40,40,TextureFormat.RGBA32,false){name="Rounded UI surface",filterMode=FilterMode.Bilinear};var pixels=new Color[1600];
            for(int y=0;y<40;y++)for(int x=0;x<40;x++)
            {float dx=Mathf.Max(10.5f-(x+.5f),0,x+.5f-29.5f),dy=Mathf.Max(10.5f-(y+.5f),0,y+.5f-29.5f);pixels[y*40+x]=new Color(1,1,1,Mathf.Clamp01(10.5f-Mathf.Sqrt(dx*dx+dy*dy)));}
            texture.SetPixels(pixels);texture.Apply();owned.Add(texture);rounded=Sprite.Create(texture,new Rect(0,0,40,40),Vector2.one*.5f,100,0,SpriteMeshType.FullRect,new Vector4(12,12,12,12));owned.Add(rounded);
        }
        Sprite[] Atlas(string path,int columns,int rows)
        {
            var texture=Resources.Load<Texture2D>(path);if(!texture)return null;var sprites=new Sprite[columns*rows];float w=texture.width/(float)columns,h=texture.height/(float)rows;
            for(int i=0;i<sprites.Length;i++)
            {
                float inset=Inset(path,w,h),top=path=="UI/BuildingIcons"&&i>=12?h*.16f:inset;
                sprites[i]=Sprite.Create(texture,new Rect(i%columns*w+inset,(rows-1-i/columns)*h+inset,w-inset*2,h-inset-top),Vector2.one*.5f,100);owned.Add(sprites[i]);
            }
            return sprites;
        }
        float Inset(string path,float w,float h)=>path=="UI/BuildingIcons"||path=="UI/WeatherIllustrations"?0:Mathf.Min(w,h)*.05f;
        public static Sprite WeatherIcon(WeatherType type,bool sketch=false)
        {int index=Mathf.Clamp((int)type,0,4)+(sketch?5:0);if(Current&&Current.weather!=null)return Current.weather[index];var data=WeatherCatalog.Get(type);return sketch?data.sketchIcon:data.fullColorIcon;}
        public static Sprite GetIcon(int index,bool item=false,bool building=false)
        {if(!Current)return null;var atlas=building?Current.buildings:item?Current.items:Current.navigation;return atlas!=null&&index>=0&&index<atlas.Length?atlas[index]:null;}
        public static UnityEngine.UI.Image Icon(Transform parent,int index,float x,float y,float width,float height,bool item=false,bool building=false)
        {
            var go=new GameObject("Illustrated icon "+index,typeof(RectTransform),typeof(UnityEngine.UI.Image));var rect=go.GetComponent<RectTransform>();rect.SetParent(parent,false);rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(width,height);
            var image=go.GetComponent<UnityEngine.UI.Image>();image.sprite=GetIcon(index,item,building);image.preserveAspect=true;image.raycastTarget=false;image.color=image.sprite?Color.white:Color.clear;return image;
        }
        public static void Round(UnityEngine.UI.Image image)
        {if(Current&&Current.rounded){image.sprite=Current.rounded;image.type=UnityEngine.UI.Image.Type.Sliced;image.pixelsPerUnitMultiplier=1;}}
        public static void Frame(RectTransform rect)
        {Round(rect.GetComponent<UnityEngine.UI.Image>());var outline=rect.GetComponent<UnityEngine.UI.Outline>()??rect.gameObject.AddComponent<UnityEngine.UI.Outline>();outline.effectColor=new Color(.70f,.59f,.35f,.32f);outline.effectDistance=new Vector2(1,-1);}
        public static void Button(UnityEngine.UI.Button button)
        {var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1,1,.86f);colors.pressedColor=new Color(.73f,.85f,.73f);colors.selectedColor=Color.white;colors.disabledColor=new Color(.68f,.72f,.67f,.50f);colors.fadeDuration=.12f;button.colors=colors;}
        public static int PageIcon(string page)
        {switch(page){case "inventory":return 0;case "weather":return 1;case "tasks":return 2;case "craft":return 3;case "town":return 4;case "companions":return 5;case "map":return 6;case "market":return 7;case "land":return 8;case "tutorial":return 9;case "camera":return 10;default:return 11;}}
        public static int ItemIcon(string key)
        {switch(key){case "wood":return 0;case "stone":return 1;case "carrots":return 2;case "planks":return 3;case "bricks":return 4;case "berries":return 5;case "fish":return 6;case "fiber":return 7;case "crystals":return 8;case "shards":return 9;case "truffles":return 10;case "starStones":return 11;case "seed0":return 12;case "seed1":return 13;case "seed2":return 14;case "meal":return 15;case "bait":return 16;case "camp":return 17;case "hoe":return 18;case "can":return 19;case "axe":return 20;case "pick":return 21;case "hammer":return 22;case "sickle":return 23;case "shovel":return 24;case "basicRod":case "rod":return 25;case "forester":return 26;case "boots":return 27;case "amulet":return 28;default:return 29;}}
        void OnDestroy(){if(Current==this)Current=null;foreach(var asset in owned)if(asset)Destroy(asset);}
    }
}
