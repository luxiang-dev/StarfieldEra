using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace StarfieldEra
{
    public sealed class FarmHud : MonoBehaviour
    {
        FarmGame game;TMP_FontAsset font;
        RectTransform header,bottom,vital;
        TextMeshProUGUI location,date,weatherName,forecast,energyValue,healthValue,clockValue,readinessValue,coins,target,message,actionLabel;
        UnityEngine.UI.Image weatherIcon,contextIcon,energyFill,healthFill;
        UnityEngine.UI.Button actionButton;
        string previous="",lastMessage="";float messageUntil;
        readonly Color ink=new Color(.24f,.19f,.13f),cream=new Color(.98f,.88f,.68f);
        public void Create(FarmGame owner,TMP_FontAsset asset)
        {
            game=owner;font=asset;gameObject.AddComponent<UITheme>().Initialize();
            var canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);scaler.matchWidthOrHeight=.5f;
            gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            if(!FindFirstObjectByType<EventSystem>()){var events=new GameObject("Farm EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));events.transform.SetParent(transform);events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();}
            header=Panel("World status",new Vector2(0,1),new Vector2(1,1),new Vector2(0,1),Vector2.zero,new Vector2(0,90),new Color(.025f,.075f,.065f,.58f));
            var place=StatusCard(header,"Location status",20,12,250,64);UITheme.Icon(place,15,8,6,48,48);
            location=Label(place,"",new Vector2(64,-7),new Vector2(178,30),22,cream);date=Label(place,"",new Vector2(64,-40),new Vector2(178,25),15,UITheme.Gold);
            var weather=StatusCard(header,"Weather status",284,12,310,64);weatherIcon=UITheme.Icon(weather,1,8,5,54,54);
            weatherName=Label(weather,"",new Vector2(72,-7),new Vector2(228,30),21,cream);forecast=Label(weather,"",new Vector2(72,-40),new Vector2(228,24),15,new Color(.73f,.83f,.75f));
            var stamina=StatusCard(header,"Stamina status",608,12,242,64);UITheme.Icon(stamina,13,9,6,45,49);
            energyValue=Label(stamina,"",new Vector2(62,-7),new Vector2(172,28),19,cream);
            var track=StatusCard(stamina,"Stamina track",62,43,164,6);track.GetComponent<UnityEngine.UI.Image>().color=new Color(.07f,.16f,.13f,.8f);
            var fill=StatusCard(track,"Stamina fill",0,0,164,6);energyFill=fill.GetComponent<UnityEngine.UI.Image>();energyFill.color=new Color(.67f,.77f,.39f);energyFill.type=UnityEngine.UI.Image.Type.Filled;energyFill.fillMethod=UnityEngine.UI.Image.FillMethod.Horizontal;
            vital=StatusCard(transform,"Player health status",20,102,300,124);
            healthValue=Label(vital,"",new Vector2(14,-8),new Vector2(272,28),21,cream);
            var healthTrack=StatusCard(vital,"Health track",14,43,272,10);healthTrack.GetComponent<UnityEngine.UI.Image>().color=new Color(.18f,.07f,.07f,.9f);
            var healthBar=StatusCard(healthTrack,"Health fill",0,0,272,10);healthFill=healthBar.GetComponent<UnityEngine.UI.Image>();healthFill.color=new Color(.83f,.25f,.24f);healthFill.type=UnityEngine.UI.Image.Type.Filled;healthFill.fillMethod=UnityEngine.UI.Image.FillMethod.Horizontal;
            clockValue=Label(vital,"",new Vector2(14,-56),new Vector2(272,22),13,UITheme.Gold);
            readinessValue=Label(vital,"",new Vector2(14,-82),new Vector2(272,38),12,new Color(.78f,.84f,.74f));
            var purse=StatusCard(header,"Coin status",864,12,190,64);UITheme.Icon(purse,14,9,6,48,48);coins=Label(purse,"",new Vector2(66,-17),new Vector2(118,34),24,UITheme.Gold);
            var weatherButton=Button(header,"天气",new Vector2(-298,-15),new Vector2(150,58),()=>game.Management.Toggle("weather"));Right(weatherButton.GetComponent<RectTransform>());DecorateButton(weatherButton,1);
            var handbook=Button(header,"旅人手册",new Vector2(-20,-15),new Vector2(264,58),()=>game.Management.Toggle("inventory"));Right(handbook.GetComponent<RectTransform>());DecorateButton(handbook,9);
            bottom=Panel("World interaction",Vector2.zero,new Vector2(1,0),Vector2.zero,Vector2.zero,new Vector2(0,100),new Color(.025f,.075f,.065f,.62f));
            actionButton=Button(bottom,"交互",new Vector2(20,-18),new Vector2(206,64),game.Interact);actionLabel=actionButton.GetComponentInChildren<TextMeshProUGUI>();DecorateButton(actionButton,16);
            contextIcon=UITheme.Icon(bottom,15,244,13,54,54);
            target=Label(bottom,"",new Vector2(312,-13),new Vector2(842,34),21,cream);
            message=Label(bottom,"",new Vector2(312,-55),new Vector2(842,30),15,new Color(.78f,.84f,.74f));
            var build=Button(bottom,"H · 建造",new Vector2(-206,-20),new Vector2(180,58),()=>game.Construction.Open());Right(build.GetComponent<RectTransform>());DecorateButton(build,17);
            var bag=Button(bottom,"背包",new Vector2(-20,-20),new Vector2(172,58),()=>game.Management.Toggle("inventory"));Right(bag.GetComponent<RectTransform>());DecorateButton(bag,0);
            var adventure=gameObject.AddComponent<AdventureHud>();adventure.Create(game.Adventure,font);
            var management=gameObject.AddComponent<ManagementHud>();management.Create(game,font);
            if(Application.isMobilePlatform)gameObject.AddComponent<MobileControls>().Create(game,font);
        }
        void Update()
        {
            if(game.Message!=lastMessage){lastMessage=game.Message;messageUntil=Time.unscaledTime+7;}
            if(message)message.gameObject.SetActive(!game.SaveHealthy||Time.unscaledTime<messageUntil);
        }
        public void SetWorldVisible(bool visible){header.gameObject.SetActive(visible);bottom.gameObject.SetActive(visible);vital.gameObject.SetActive(visible);}
        public void Refresh()
        {
            var state=game.State;int selected=game.SelectedPlot;var crop=selected<0?null:state.plots[selected];bool managed=game.Management&&game.Management.IsOpen;
            bool wet=state.storm.DepthAt(game.World.Player.position)>.025f;
            string terrain=game.InCave?"地下矿洞 · 开采与战利品":(wet?"积水 · ":"")+TerrainTraits.Hint(game.World.HexGrid.TerrainAt(game.World.Player.position),game.Expedition.MovementSpeed(game.World.HexGrid.TerrainAt(game.World.Player.position))*(wet?.65f:1));
            string signature=game.Readiness?.Status+"/"+game.Inspection?.Hint+"/"+game.Mining?.Hint+"/"+game.Forestry?.Hint+"/"+game.IsSprinting+"/"+game.Night?.Active+"/"+game.Night?.TrapPlacing+"/"+managed+"/"+game.World.Adventure.Region+"/"+terrain+"/"+game.Land.NearNewPlot+"/"+game.Land.Hint+"/"+game.Construction.Hint+"/"+game.Fences.Hint+"/"+game.Adventure.Revision+"/"+game.Adventure.TargetHint+"/"+game.Animals.HasNearby+"/"+state.clock.MinuteOfDay+"/"+state.survival.health+"/"+state.day+"/"+state.coins+"/"+state.energy+"/"+selected+"/"+state.selectedCrop+"/"+crop?.planted+"/"+crop?.watered+"/"+crop?.growth+"/"+game.NearHouse+"/"+game.Message+"/"+game.SaveHealthy;
            if(signature==previous)return;previous=signature;
            SetWorldVisible(!managed);
            location.text=game.InCave?"旧矿洞":AdventureContent.Regions[game.World.Adventure.Region];date.text="第 "+state.day+" 天 · "+state.Season;
            var today=WeatherCatalog.Get(state.Weather);weatherName.text=today.displayName;forecast.text="明日 · "+WeatherCatalog.Get(state.TomorrowWeather).displayName;weatherIcon.sprite=UITheme.WeatherIcon(state.Weather);
            readinessValue.text=game.Readiness?game.Readiness.Status:"准备出征";
            healthValue.text="生命 "+state.survival.health+" / "+SurvivalState.MaxHealth;healthFill.fillAmount=state.survival.health/(float)SurvivalState.MaxHealth;clockValue.text=state.clock.Display+" · "+state.Season+" · "+(state.clock.IsNight?"夜间":"日间")+" · 20分钟/日";
            energyValue.text="体力 "+state.energy+" / "+FarmState.MaxEnergy;energyFill.fillAmount=state.energy/(float)FarmState.MaxEnergy;coins.text=state.coins.ToString();
            message.text=game.Message+(game.SaveHealthy?"":" · 保存失败，请检查磁盘权限。");
            string context="探索世界";
            bool canInteract=false;actionLabel.text="交互";
            if(crop!=null)
            {
                context="农田 "+(selected+1)+" · "+(!crop.planted?"种植 "+FarmState.CropNames[state.selectedCrop]:crop.Ready?"收获 "+FarmState.CropNames[crop.kind]:crop.watered?"今天已浇水":"需要浇水");
                canInteract=crop.Ready||(!crop.planted&&state.SelectedSeeds>0);
                if(selected>=12&&!state.adventure.expandedFarm){context="南侧农田 · 需要扩建农场";canInteract=false;}
            }
            else if(game.NearHouse)context="房屋 · 可以休息 · 打开背包购买种子、制作料理"+(game.Night&&game.Night.State.Target(game.State)>=0?" · "+game.Night.Forecast:"");
            if(game.Forestry.HasNearby&&selected<0&&game.Adventure.Target==null){context=game.Forestry.Hint;canInteract=true;actionLabel.text="砍树";}
            if(game.Mining.HasNearby){context=game.Mining.Hint;canInteract=true;actionLabel.text="开采";}
            if(game.Adventure.Target!=null){context=game.Adventure.TargetHint;canInteract=true;}
            if(game.Animals.HasNearby){context="附近动物 · 查看与照料";canInteract=true;}
            if(game.Land.NearNewPlot){context="新农田 · E 播种或收获 · R 浇水";canInteract=true;}
            if(game.Expedition.Fishing){context="钓鱼 · 收线 · 光标进入绿色区域";actionLabel.text="收线";canInteract=true;}
            if(game.Adventure.InBattle){context="遭遇战 · 在战斗面板选择行动";canInteract=false;}
            if(game.Land.Active){context=game.Land.Hint;canInteract=false;}
            if(game.Construction.Active){context=game.Construction.Hint;canInteract=false;}if(game.Fences.Active){context=game.Fences.Hint;canInteract=false;}
            if(game.Night&&game.Night.Active){context="夜间守卫 · E驱赶身旁野兽 · R修补设施";canInteract=true;actionLabel.text="驱赶";}
            if(game.Garden&&game.Garden.Placing){context=game.Garden.Hint;canInteract=false;}
            if(game.Night&&game.Night.TrapPlacing){context="陷阱布置 · 点击六步内空地 · 木2石1 · 右键取消";canInteract=false;}
            if(game.InCave){context="矿洞探索 · E 开采、开箱或攻击 · 入口返回营地";canInteract=true;actionLabel.text="交互";}
            if(game.Inspection&&game.Inspection.HasSelection&&!game.Construction.Active&&!game.Fences.Active&&!game.Land.Active&&!game.Expedition.Fishing&&!game.Adventure.InBattle){context=game.Inspection.Hint;canInteract=game.Inspection.CanInteract;actionLabel.text="选中目标";}
            contextIcon.sprite=UITheme.GetIcon(game.Night&&game.Night.Active?22:game.Construction.Active||game.Fences.Active?17:game.Land.Active?8:game.Expedition.Fishing?21:crop!=null?20:game.Forestry.HasNearby?18:game.Animals.HasNearby?11:15);
            target.text=context+" · "+terrain;actionButton.interactable=canInteract&&!managed;
            game.Adventure.Hud.Refresh();
        }
        RectTransform StatusCard(Transform parent,string name,float x,float y,float width,float height)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image));var rect=go.GetComponent<RectTransform>();rect.SetParent(parent,false);rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(width,height);var image=go.GetComponent<UnityEngine.UI.Image>();image.color=new Color(.05f,.15f,.125f,.54f);image.raycastTarget=false;UITheme.Round(image);return rect;
        }
        void Right(RectTransform rect){rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(1,1);}
        void DecorateButton(UnityEngine.UI.Button button,int icon)
        {var rect=button.GetComponent<RectTransform>();UITheme.Icon(rect,icon,10,5,48,48);var label=button.GetComponentInChildren<TextMeshProUGUI>();label.rectTransform.anchoredPosition=new Vector2(62,-10);label.rectTransform.sizeDelta=new Vector2(rect.sizeDelta.x-70,40);label.fontSize=20;}
        RectTransform Panel(string name,Vector2 min,Vector2 max,Vector2 pivot,Vector2 position,Vector2 size,Color color)
        {var go=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image));var rect=go.GetComponent<RectTransform>();rect.SetParent(transform,false);rect.anchorMin=min;rect.anchorMax=max;rect.pivot=pivot;rect.anchoredPosition=position;rect.sizeDelta=size;go.GetComponent<UnityEngine.UI.Image>().color=color;go.GetComponent<UnityEngine.UI.Image>().raycastTarget=true;return rect;}
        TextMeshProUGUI Label(Transform parent,string value,Vector2 position,Vector2 size,float point,Color color)
        {var go=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI));var rect=go.GetComponent<RectTransform>();rect.SetParent(parent,false);rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=position;rect.sizeDelta=new Vector2(size.x,Mathf.Max(size.y,point*1.55f));var label=go.GetComponent<TextMeshProUGUI>();label.font=font;label.fontSize=point;label.text=value;label.color=color;label.raycastTarget=false;label.enableAutoSizing=false;label.overflowMode=TextOverflowModes.Ellipsis;return label;}
        UnityEngine.UI.Button Button(Transform parent,string value,Vector2 position,Vector2 size,UnityEngine.Events.UnityAction callback)
        {var go=new GameObject(value,typeof(RectTransform),typeof(UnityEngine.UI.Image),typeof(UnityEngine.UI.Button));var rect=go.GetComponent<RectTransform>();rect.SetParent(parent,false);rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=position;rect.sizeDelta=size;var image=go.GetComponent<UnityEngine.UI.Image>();image.color=new Color(.14f,.27f,.23f);var button=go.GetComponent<UnityEngine.UI.Button>();button.targetGraphic=image;button.onClick.AddListener(callback);var label=Label(rect,value,new Vector2(8,-6),size-new Vector2(16,12),18,cream);label.alignment=TextAlignmentOptions.Center;UITheme.Frame(rect);UITheme.Button(button);return button;}
    }
}
