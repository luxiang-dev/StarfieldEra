using TMPro;
using UnityEngine;
namespace StarfieldEra
{
    /// <summary>Small, independent construction picker; world placement starts after choosing a card.</summary>
    public sealed class BuildingMenu:MonoBehaviour
    {
        FarmGame game;TMP_FontAsset font;RectTransform root;TextMeshProUGUI wallet;
        Sprite[] icons;Texture2D atlas;
        public bool IsOpen=>root&&root.gameObject.activeSelf;
        public void Initialize(FarmGame owner,TMP_FontAsset asset)
        {
            game=owner;font=asset;atlas=Resources.Load<Texture2D>("UI/BuildingIcons");
            if(atlas){icons=new Sprite[16];int w=atlas.width/4,h=atlas.height/4;for(int i=0;i<16;i++)icons[i]=Sprite.Create(atlas,new Rect(i%4*w,(3-i/4)*h,w,h),Vector2.one*.5f,100);}
            root=Box(transform,"Building picker",new Vector2(-24,-104),new Vector2(666,646),new Color(.06f,.16f,.15f,.99f));
            root.anchorMin=root.anchorMax=root.pivot=Vector2.one;HudOrnament.Border(root);
            Label(root,"H · 选择建筑",new Vector2(20,-12),new Vector2(420,40),26,new Color(1,.86f,.58f));
            wallet=Label(root,"",new Vector2(20,-56),new Vector2(620,30),17,Color.white);
            for(int i=0;i<12;i++)
            {
                int kind=i;float x=20+i%3*210,y=-96-i/3*112;
                var card=Button(root,HexConstruction.Names[i],new Vector2(x,y),new Vector2(202,104),()=>game.Construction.Choose(kind),i);
                Label(card,HexConstruction.Names[i],new Vector2(70,-8),new Vector2(128,32),21,new Color(1,.91f,.72f));
                Label(card,"木"+HexConstruction.WoodCost[i]+" 石"+HexConstruction.StoneCost[i]+" 金"+HexConstruction.GoldCost[i],new Vector2(8,-68),new Vector2(190,30),16,Color.white);
            }
            Label(root,"选建筑 → 鼠标选址 · R旋转 · 左键建造 · 右键取消",new Vector2(20,-548),new Vector2(626,30),17,new Color(.94f,.85f,.65f));
            Button(root,"围栏 / 门",new Vector2(20,-592),new Vector2(146,38),()=>{Close();game.Adventure.Hud.Show("pasture");});
            Button(root,"地形改造",new Vector2(180,-592),new Vector2(146,38),()=>{Close();game.Management.Open("land");});
            Button(root,"拆除建筑",new Vector2(340,-592),new Vector2(146,38),()=>game.Construction.Choose(HexConstruction.DemolitionKind));
            Button(root,"关闭",new Vector2(500,-592),new Vector2(146,38),Close);
            root.gameObject.SetActive(false);
        }
        public void Show()
        {
            game.Management.Close();game.Adventure.Hud.HideContextOnly();game.Land.Cancel();game.Fences.Cancel();
            wallet.text="木材 "+game.State.adventure.wood+" · 石料 "+game.State.adventure.stone+" · 金币 "+game.State.coins+"    产出按天结算";
            root.gameObject.SetActive(true);root.SetAsLastSibling();
            var canvas=GetComponent<Canvas>();float factor=canvas?canvas.scaleFactor:1;
            root.localScale=Vector3.one*Mathf.Min(1,(Screen.height/Mathf.Max(.1f,factor)-200)/646f);
            Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        }
        public void Close(){if(root)root.gameObject.SetActive(false);}
        RectTransform Box(Transform parent,string name,Vector2 pos,Vector2 size,Color color)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image));var rect=go.GetComponent<RectTransform>();rect.SetParent(parent,false);rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=pos;rect.sizeDelta=size;go.GetComponent<UnityEngine.UI.Image>().color=color;return rect;
        }
        TextMeshProUGUI Label(Transform parent,string text,Vector2 pos,Vector2 size,float points,Color color)
        {
            var go=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI));var rect=go.GetComponent<RectTransform>();rect.SetParent(parent,false);rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=pos;rect.sizeDelta=size;
            var label=go.GetComponent<TextMeshProUGUI>();label.font=font;label.fontSize=points;label.text=text;label.color=color;label.raycastTarget=false;return label;
        }
        RectTransform Button(Transform parent,string name,Vector2 pos,Vector2 size,UnityEngine.Events.UnityAction action,int icon=-1)
        {
            var rect=Box(parent,name,pos,size,new Color(.16f,.29f,.25f));var button=rect.gameObject.AddComponent<UnityEngine.UI.Button>();button.targetGraphic=rect.GetComponent<UnityEngine.UI.Image>();button.onClick.AddListener(action);
            if(icon>=0&&icons!=null){var image=Box(rect,"Building icon",new Vector2(8,-4),new Vector2(60,60),Color.white).GetComponent<UnityEngine.UI.Image>();image.sprite=icons[icon];image.preserveAspect=true;image.raycastTarget=false;}
            else if(icon<0)Label(rect,name,new Vector2(6,-4),size-new Vector2(12,4),18,Color.white).alignment=TextAlignmentOptions.Center;
            return rect;
        }
        void OnDestroy(){if(icons!=null)foreach(var sprite in icons)if(sprite)Destroy(sprite);}
    }
}
