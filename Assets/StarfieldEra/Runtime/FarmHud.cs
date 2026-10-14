using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace StarfieldEra
{
    public sealed class FarmHud : MonoBehaviour
    {
        FarmGame game;TMP_FontAsset font;
        RectTransform header,bottom;
        TextMeshProUGUI counters,target,message,actionLabel;
        UnityEngine.UI.Button actionButton;
        string previous="",lastMessage="";float messageUntil;
        readonly Color ink=new Color(.24f,.19f,.13f),cream=new Color(.98f,.88f,.68f);
        public void Create(FarmGame owner,TMP_FontAsset asset)
        {
            game=owner;font=asset;
            var canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);scaler.matchWidthOrHeight=.5f;
            gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            var events=new GameObject("Farm EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));events.transform.SetParent(transform);events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            header=Panel("World status",new Vector2(0,1),new Vector2(1,1),new Vector2(0,1),Vector2.zero,new Vector2(0,78),new Color(.93f,.89f,.79f,.96f));
            var emblem=new GameObject("World compass",typeof(RectTransform),typeof(CanvasRenderer),typeof(HudOrnament));var e=emblem.GetComponent<RectTransform>();e.SetParent(header,false);e.anchorMin=e.anchorMax=e.pivot=new Vector2(0,1);e.anchoredPosition=new Vector2(22,-12);e.sizeDelta=Vector2.one*52;emblem.GetComponent<HudOrnament>().color=new Color(.65f,.45f,.22f);emblem.GetComponent<HudOrnament>().raycastTarget=false;
            Label(header,"星野纪元",new Vector2(88,-10),new Vector2(330,40),26,ink);
            Label(header,"STARFIELD ERA · 经营与远行",new Vector2(90,-50),new Vector2(340,20),13,ink);
            counters=Label(header,"",new Vector2(460,-24),new Vector2(840,34),18,ink);
            Button(header,"Tab 旅人手册",new Vector2(1330,-18),new Vector2(240,42),()=>game.Management.Toggle("inventory"));
            bottom=Panel("World interaction",Vector2.zero,new Vector2(1,0),Vector2.zero,Vector2.zero,new Vector2(0,94),new Color(.06f,.16f,.15f,.94f));
            actionButton=Button(bottom,"E 交互",new Vector2(24,-24),new Vector2(170,46),game.Interact);actionLabel=actionButton.GetComponentInChildren<TextMeshProUGUI>();
            target=Label(bottom,"",new Vector2(218,-14),new Vector2(1340,32),21,cream);
            message=Label(bottom,"",new Vector2(218,-53),new Vector2(1340,26),16,new Color(.86f,.87f,.76f));
            var adventure=gameObject.AddComponent<AdventureHud>();adventure.Create(game.Adventure,font);
            var management=gameObject.AddComponent<ManagementHud>();management.Create(game,font);
        }
        void Update()
        {
            if(game.Message!=lastMessage){lastMessage=game.Message;messageUntil=Time.unscaledTime+7;}
            if(message)message.gameObject.SetActive(!game.SaveHealthy||Time.unscaledTime<messageUntil);
        }
        public void SetWorldVisible(bool visible){header.gameObject.SetActive(visible);bottom.gameObject.SetActive(visible);}
        public void Refresh()
        {
            var state=game.State;int selected=game.SelectedPlot;var crop=selected<0?null:state.plots[selected];bool managed=game.Management&&game.Management.IsOpen;
            string terrain=TerrainTraits.Hint(game.World.HexGrid.TerrainAt(game.World.Player.position),game.Expedition.MovementSpeed(game.World.HexGrid.TerrainAt(game.World.Player.position)));
            string signature=managed+"/"+game.World.Adventure.Region+"/"+terrain+"/"+game.Construction.Hint+"/"+game.Fences.Hint+"/"+game.Adventure.Revision+"/"+game.Adventure.TargetHint+"/"+game.Animals.HasNearby+"/"+state.day+"/"+state.coins+"/"+state.energy+"/"+selected+"/"+state.selectedCrop+"/"+crop?.planted+"/"+crop?.watered+"/"+crop?.growth+"/"+game.NearHouse+"/"+game.Message+"/"+game.SaveHealthy;
            if(signature==previous)return;previous=signature;
            SetWorldVisible(!managed);
            counters.text=AdventureContent.Regions[game.World.Adventure.Region]+" · 第"+state.day+"天 "+state.Season+" · "+(state.Rainy?"雨天":"晴天")+"    体力 "+state.energy+"/30    金币 "+state.coins;
            message.text=game.Message+(game.SaveHealthy?"":" · 保存失败，请检查磁盘权限。");
            string context="WASD 移动 · I 背包 · Q 任务 · Tab 手册";
            bool canInteract=false;actionLabel.text="E 交互";
            if(crop!=null)
            {
                context="农田 "+(selected+1)+" · "+(!crop.planted?"E 种植 "+FarmState.CropNames[state.selectedCrop]:crop.Ready?"E 收获 "+FarmState.CropNames[crop.kind]:crop.watered?"今天已浇水":"R 浇水");
                canInteract=crop.Ready||(!crop.planted&&state.SelectedSeeds>0);
                if(selected>=12&&!state.adventure.expandedFarm){context="南侧农田 · 需要扩建农场";canInteract=false;}
            }
            else if(game.NearHouse)context="小屋 · F 休息 · 打开背包购买种子、制作料理";
            if(game.Adventure.Target!=null){context=game.Adventure.TargetHint;canInteract=true;}
            if(game.Animals.HasNearby){context="附近动物 · E 查看与照料";canInteract=true;}
            if(game.Expedition.Fishing){context="钓鱼 · E 收线 · 光标进入绿色区域";actionLabel.text="E 收线";canInteract=true;}
            if(game.Adventure.InBattle){context="遭遇战 · 在战斗面板选择行动";canInteract=false;}
            if(game.Construction.Active){context=game.Construction.Hint;canInteract=false;}if(game.Fences.Active){context=game.Fences.Hint;canInteract=false;}
            target.text=context+" · "+terrain;actionButton.interactable=canInteract&&!managed;
            game.Adventure.Hud.Refresh();
        }
        RectTransform Panel(string name,Vector2 min,Vector2 max,Vector2 pivot,Vector2 position,Vector2 size,Color color)
        {var go=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image));var rect=go.GetComponent<RectTransform>();rect.SetParent(transform,false);rect.anchorMin=min;rect.anchorMax=max;rect.pivot=pivot;rect.anchoredPosition=position;rect.sizeDelta=size;go.GetComponent<UnityEngine.UI.Image>().color=color;go.GetComponent<UnityEngine.UI.Image>().raycastTarget=true;return rect;}
        TextMeshProUGUI Label(Transform parent,string value,Vector2 position,Vector2 size,float point,Color color)
        {var go=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI));var rect=go.GetComponent<RectTransform>();rect.SetParent(parent,false);rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=position;rect.sizeDelta=size;var label=go.GetComponent<TextMeshProUGUI>();label.font=font;label.fontSize=point;label.text=value;label.color=color;label.raycastTarget=false;label.enableAutoSizing=false;label.overflowMode=TextOverflowModes.Ellipsis;return label;}
        UnityEngine.UI.Button Button(Transform parent,string value,Vector2 position,Vector2 size,UnityEngine.Events.UnityAction callback)
        {var go=new GameObject(value,typeof(RectTransform),typeof(UnityEngine.UI.Image),typeof(UnityEngine.UI.Button));var rect=go.GetComponent<RectTransform>();rect.SetParent(parent,false);rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=position;rect.sizeDelta=size;var image=go.GetComponent<UnityEngine.UI.Image>();image.color=new Color(.14f,.27f,.23f);var button=go.GetComponent<UnityEngine.UI.Button>();button.targetGraphic=image;button.onClick.AddListener(callback);var label=Label(rect,value,new Vector2(8,-6),size-new Vector2(16,12),18,cream);label.alignment=TextAlignmentOptions.Center;HudOrnament.Border(rect);return button;}
    }
}
