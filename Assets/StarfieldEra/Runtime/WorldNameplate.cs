using TMPro;
using UnityEngine;
namespace StarfieldEra
{
    /// <summary>Passive world-space labels share their rounded image and outlined font material.</summary>
    public sealed class WorldNameplate:MonoBehaviour
    {
        static Sprite panel;static Material lettering;static int instances;
        Camera view;
        public static WorldNameplate Create(Transform anchor,string title,TMP_FontAsset font,Camera camera,float height)
        {
            var root=new GameObject("World nameplate",typeof(RectTransform),typeof(Canvas),typeof(WorldNameplate));root.transform.SetParent(anchor,false);root.transform.localPosition=Vector3.up*height;root.transform.localScale=Vector3.one*.01f;
            var plate=root.GetComponent<WorldNameplate>();plate.view=camera;
            var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=camera;canvas.sortingOrder=3;canvas.additionalShaderChannels=AdditionalCanvasShaderChannels.TexCoord1|AdditionalCanvasShaderChannels.Normal|AdditionalCanvasShaderChannels.Tangent;
            float width=Mathf.Clamp(title.Length*28+34,120,480);root.GetComponent<RectTransform>().sizeDelta=new Vector2(width,52);
            if(!panel)
            {
                var texture=new Texture2D(32,32,TextureFormat.RGBA32,false){name="Shared rounded nameplate",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
                for(int y=0;y<32;y++)for(int x=0;x<32;x++){float dx=Mathf.Max(Mathf.Abs(x-15.5f)-7.5f,0),dy=Mathf.Max(Mathf.Abs(y-15.5f)-7.5f,0);texture.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(8-Mathf.Sqrt(dx*dx+dy*dy))));}texture.Apply(false,true);
                panel=Sprite.Create(texture,new Rect(0,0,32,32),Vector2.one*.5f,100,0,SpriteMeshType.FullRect,new Vector4(9,9,9,9));
            }
            var backing=new GameObject("Rounded dark label background",typeof(RectTransform),typeof(UnityEngine.UI.Image));var back=backing.GetComponent<RectTransform>();back.SetParent(root.transform,false);Stretch(back);var image=backing.GetComponent<UnityEngine.UI.Image>();image.sprite=panel;image.type=UnityEngine.UI.Image.Type.Sliced;image.color=new Color(.035f,.075f,.06f,.61f);image.raycastTarget=false;
            var label=new GameObject("Outlined label",typeof(RectTransform),typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();label.transform.SetParent(root.transform,false);Stretch(label.rectTransform);label.rectTransform.offsetMin=new Vector2(10,4);label.rectTransform.offsetMax=new Vector2(-10,-4);label.font=font;label.fontSize=26;label.alignment=TextAlignmentOptions.Center;label.enableAutoSizing=false;label.text=title;label.color=new Color(1,.96f,.81f);label.raycastTarget=false;label.overflowMode=TextOverflowModes.Ellipsis;
            if(!lettering){lettering=new Material(font.material){name="Shared outlined world labels"};lettering.EnableKeyword("OUTLINE_ON");lettering.SetColor(ShaderUtilities.ID_OutlineColor,new Color(.025f,.045f,.03f,1));lettering.SetFloat(ShaderUtilities.ID_OutlineWidth,.16f);}
            label.fontSharedMaterial=lettering;return plate;
        }
        static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
        void Awake(){instances++;}
        void LateUpdate(){if(view)transform.rotation=view.transform.rotation;}
        void OnDestroy(){if(--instances>0)return;if(lettering)Destroy(lettering);if(panel){Destroy(panel.texture);Destroy(panel);}lettering=null;panel=null;}
    }
}
