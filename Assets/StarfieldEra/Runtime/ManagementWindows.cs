using TMPro;
using UnityEngine;
namespace StarfieldEra
{
    public sealed partial class ManagementHud
    {
        sealed class WindowLayout
        {
            public RectTransform backdrop,window,content;
            public TextMeshProUGUI heading,wallet,feedback,introduction;
            public UnityEngine.UI.Image emblem;
        }
        WindowLayout handbookLayout,operationsLayout;
        public bool IsHandbookOpen=>handbookLayout!=null&&handbookLayout.backdrop.gameObject.activeSelf;
        public bool IsOperationsOpen=>operationsLayout!=null&&operationsLayout.backdrop.gameObject.activeSelf;
        void CreateOperationsWindow()
        {
            handbookLayout=new WindowLayout{backdrop=overlay,window=window,content=content,heading=heading,wallet=wallet,feedback=feedback,introduction=pageIntro,emblem=pageEmblem};
            var root=Box(transform,"World operations backdrop",0,0,0,0,new Color(.015f,.035f,.03f,.22f),true);root.anchorMin=Vector2.zero;root.anchorMax=Vector2.one;root.offsetMin=root.offsetMax=Vector2.zero;
            var panel=Box(root,"World operations window",0,0,1260,800,new Color(.035f,.105f,.09f,.95f),true);panel.anchorMin=panel.anchorMax=panel.pivot=new Vector2(.5f,.5f);panel.anchoredPosition=Vector2.zero;UITheme.Frame(panel);
            var header=Box(panel,"Operations header",12,12,1236,82,new Color(.085f,.19f,.15f,.86f));
            var emblem=UITheme.Icon(header,8,16,12,54,54);var title=Text(header,"",86,16,620,48,29,cream);var balance=Text(header,"",724,12,290,60,17,gold);
            Button(header,"返回游戏",1032,16,184,46,Close);
            operationsLayout=new WindowLayout{backdrop=root,window=panel,content=Box(panel,"Operations page",46,140,1168,598,Color.clear),heading=title,wallet=balance,emblem=emblem,introduction=Text(panel,"",46,104,1168,36,18,new Color(.76f,.84f,.74f)),feedback=Text(panel,"",46,756,1168,32,16,gold)};
            root.gameObject.SetActive(false);
        }
        public static bool IsOperationsPage(string target)=>target=="land"||target=="building"||target=="production"||target=="buildings"||target=="camp";
        static string OperationsTitle(string target)=>target=="land"?"土地建造 · 改造与生态":target=="production"?"资源点经营 · 生产与库存":target=="buildings"?"建筑管理 · 自建建筑":target=="camp"?"营地与守卫 · 建设与防御":"建筑管理 · 生产与建设";
        void OperationsWorkspace()
        {
            Chip("资源点经营",0,()=>Open("production"),page=="production");Chip("自建建筑",158,()=>Open("buildings"),page=="buildings");Chip("营地与守卫",316,OpenCamp,page=="camp");Chip("开始建造",474,game.Construction.Open,false);
            if(page=="production")ResourcePage();else if(page=="buildings")Buildings();else CampPage();
        }
        void SelectWindow(string target)
        {
            handbookLayout.backdrop.gameObject.SetActive(false);operationsLayout.backdrop.gameObject.SetActive(false);
            var layout=IsOperationsPage(target)?operationsLayout:handbookLayout;
            overlay=layout.backdrop;window=layout.window;content=layout.content;heading=layout.heading;wallet=layout.wallet;feedback=layout.feedback;pageIntro=layout.introduction;pageEmblem=layout.emblem;
        }
    }
}
