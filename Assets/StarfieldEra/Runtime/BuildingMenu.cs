using TMPro;
using UnityEngine;
namespace StarfieldEra
{
    public sealed class BuildingMenu:MonoBehaviour
    {
        FarmGame game;TMP_FontAsset font;RectTransform root,cards,footer;TextMeshProUGUI wallet,hint;
        int category;readonly UnityEngine.UI.Button[] tabs=new UnityEngine.UI.Button[4];
        public bool IsOpen=>root&&root.gameObject.activeSelf;
        public void Initialize(FarmGame owner,TMP_FontAsset asset)
        {
            game=owner;font=asset;
            root=Box(transform,"Building picker",new Vector2(-24,-106),new Vector2(732,700),new Color(.035f,.11f,.095f,.91f));root.anchorMin=root.anchorMax=root.pivot=Vector2.one;UITheme.Frame(root);
            UITheme.Icon(root,17,18,12,48,48);Label(root,"建造与规划",new Vector2(80,-12),new Vector2(480,40),26,UITheme.Paper);
            wallet=Label(root,"",new Vector2(80,-54),new Vector2(628,28),16,UITheme.Gold);
            string[] names={"全部建筑","生产与研究","居住与商贸","营地与防御"};
            for(int i=0;i<4;i++){int id=i;tabs[i]=Button(root,names[i],new Vector2(20+i*174,-94),new Vector2(164,38),()=>{category=id;Rebuild();});}
            cards=Box(root,"Building category cards",new Vector2(20,-148),new Vector2(692,444),Color.clear);
            hint=Label(root,"H · 关闭菜单    R · 旋转    左键 · 放置    右键 · 取消",new Vector2(22,-604),new Vector2(688,28),16,UITheme.Gold);
            footer=Box(root,"Building menu footer",new Vector2(20,-650),new Vector2(692,38),Color.clear);
            Button(footer,"地形改造",new Vector2(0,0),new Vector2(164,38),()=>{Close();game.Management.Open("land");});
            Button(footer,"拆除建筑",new Vector2(174,0),new Vector2(164,38),()=>game.Construction.Choose(HexConstruction.DemolitionKind));
            Button(footer,"拆除围栏",new Vector2(348,0),new Vector2(164,38),()=>{Close();game.Fences.Choose(2);});
            Button(footer,"返回世界",new Vector2(522,0),new Vector2(164,38),Close);
            root.gameObject.SetActive(false);
        }
        bool InCategory(int kind)=>category==0||category==1&&(kind<3||kind>=6&&kind<=9)||category==2&&(kind>=4&&kind<=5||kind>=10&&kind<=11)||category==3&&(kind>=12||kind==3);
        void Rebuild()
        {
            for(int i=cards.childCount-1;i>=0;i--){cards.GetChild(i).gameObject.SetActive(false);Destroy(cards.GetChild(i).gameObject);}
            for(int i=0;i<tabs.Length;i++)tabs[i].GetComponent<UnityEngine.UI.Image>().color=i==category?new Color(.31f,.36f,.22f,.95f):UITheme.Card;
            int slot=0;
            for(int kind=0;kind<15;kind++)
            {
                if(!InCategory(kind))continue;int choice=kind;
                string name=kind<12?HexConstruction.Names[kind]:kind==12?"木围栏":kind==13?"牧场门":"捕兽陷阱";
                string cost=kind<12?"木"+HexConstruction.WoodCost[kind]+" 石"+HexConstruction.StoneCost[kind]+" 金"+HexConstruction.GoldCost[kind]:kind==12?"木材 2":kind==13?"木材 4":"木材 2 · 石料 1";
                var card=Button(cards,"",new Vector2(slot%4*174,-(slot/4)*112),new Vector2(164,102),()=>Choose(choice));card.name=name;
                UITheme.Icon(card.transform,kind<14?kind:22,6,8,58,58,false,kind<14);
                Label(card.transform,name,new Vector2(70,-12),new Vector2(88,30),18,UITheme.Paper);
                Label(card.transform,kind<12?BuildingFootprints.Counts[kind]+"格建筑":kind==14?"资源点守卫":"拖动连续建造",new Vector2(70,-45),new Vector2(88,24),12,new Color(.72f,.82f,.73f));
                Label(card.transform,cost,new Vector2(12,-76),new Vector2(145,23),14,UITheme.Gold);slot++;
            }
            int rows=Mathf.Max(1,Mathf.CeilToInt(slot/4f));float body=rows*112;
            root.sizeDelta=new Vector2(732,252+body);cards.sizeDelta=new Vector2(692,body-4);hint.rectTransform.anchoredPosition=new Vector2(22,-(156+body));footer.anchoredPosition=new Vector2(20,-(202+body));Fit();
        }
        void Choose(int kind)
        {Close();if(kind<12)game.Construction.Choose(kind);else if(kind<14)game.Fences.Choose(kind-12);else game.Night.ChooseTrap();}
        public void Show()
        {
            game.Management.Close();game.Adventure.Hud.HideContextOnly();game.Land.Cancel();game.Fences.Cancel();
            wallet.text="木材 "+game.State.adventure.wood+" · 石料 "+game.State.adventure.stone+" · 金币 "+game.State.coins;
            Rebuild();root.gameObject.SetActive(true);root.SetAsLastSibling();
            Fit();Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        }
        void Fit(){var canvas=GetComponent<Canvas>();float factor=canvas?canvas.scaleFactor:1;root.localScale=Vector3.one*Mathf.Max(.25f,Mathf.Min(1,(Screen.height/Mathf.Max(.1f,factor)-190)/root.sizeDelta.y));}
        public void Close(){if(root)root.gameObject.SetActive(false);}
        RectTransform Box(Transform parent,string name,Vector2 pos,Vector2 size,Color color)
        {var go=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image));var rect=go.GetComponent<RectTransform>();rect.SetParent(parent,false);rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=pos;rect.sizeDelta=size;var image=go.GetComponent<UnityEngine.UI.Image>();image.color=color;UITheme.Round(image);return rect;}
        TextMeshProUGUI Label(Transform parent,string text,Vector2 pos,Vector2 size,float points,Color color)
        {var go=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI));var rect=go.GetComponent<RectTransform>();rect.SetParent(parent,false);rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=pos;rect.sizeDelta=size;var label=go.GetComponent<TextMeshProUGUI>();label.font=font;label.fontSize=points;label.text=text;label.color=color;label.raycastTarget=false;return label;}
        UnityEngine.UI.Button Button(Transform parent,string name,Vector2 pos,Vector2 size,UnityEngine.Events.UnityAction action)
        {var rect=Box(parent,name,pos,size,UITheme.Card);var button=rect.gameObject.AddComponent<UnityEngine.UI.Button>();button.targetGraphic=rect.GetComponent<UnityEngine.UI.Image>();button.onClick.AddListener(action);UITheme.Frame(rect);UITheme.Button(button);if(name!="")Label(rect,name,new Vector2(6,-4),size-new Vector2(12,4),17,UITheme.Paper).alignment=TextAlignmentOptions.Center;return button;}
    }
}
