using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace StarfieldEra
{
    public sealed class FarmHud : MonoBehaviour
    {
        FarmGame game;
        TMP_FontAsset font;
        TextMeshProUGUI counters, target, message, objective;
        UnityEngine.UI.Button actionButton, waterButton, sleepButton, buyButton;
        TextMeshProUGUI actionLabel, questProgress, buyLabel;
        UnityEngine.UI.Image progressFill;
        readonly TextMeshProUGUI[] resources = new TextMeshProUGUI[6];
        string previous;
        readonly Color ink = new Color(.24f, .19f, .13f);

        public void Create(FarmGame owner, TMP_FontAsset interfaceFont)
        {
            game = owner; font = interfaceFont;
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.matchWidthOrHeight = .5f;
            gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            var events = new GameObject("Farm EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.transform.SetParent(transform);
            events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();

            RectTransform header = Panel("Header", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), Vector2.zero, new Vector2(0, 116), new Color(.93f, .89f, .79f, .96f));
            Label("Title", header, "星野纪元", new Vector2(120, -14), new Vector2(340, 45), 36, ink);
            Label("Subtitle", header, "STARFIELD ERA  /  经营 · 探索 · 伙伴", new Vector2(122, -65), new Vector2(350, 25), 15, ink);
            counters = Label("Counters", header, "", new Vector2(500, -16), new Vector2(1070, 35), 18, ink);

            Icon(header, new Vector2(28,-14), new Vector2(78,78),0,new Color(.65f,.45f,.22f));
            string[] names={"木材","石料","浆果","鲜鱼","晶石","知识"};
            Color[] colors={new Color(.55f,.32f,.16f),new Color(.42f,.46f,.45f),new Color(.51f,.6f,.25f),new Color(.19f,.5f,.64f),new Color(.61f,.32f,.72f),new Color(.58f,.4f,.22f)};
            for(int i=0;i<6;i++)
            {
                Icon(header,new Vector2(500+i*172,-62),new Vector2(26,28),i+1,colors[i]);
                resources[i]=Label(names[i],header,"",new Vector2(536+i*172,-62),new Vector2(133,32),20,ink);
            }
            RectTransform card = Panel("Objective", new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -140), new Vector2(365, 190), new Color(.06f, .16f, .15f, .94f));
            HudOrnament.Border(card);
            Label("Objective title", card, "晨风村 · 复兴之旅", new Vector2(20, -16), new Vector2(290, 32), 24, new Color(1, .88f, .62f));
            objective = Label("Objective body", card, "", new Vector2(20, -58), new Vector2(325, 118), 17, Color.white);

            var track=new GameObject("Quest progress track",typeof(RectTransform),typeof(UnityEngine.UI.Image));
            var trackRect=track.GetComponent<RectTransform>();trackRect.SetParent(card,false);trackRect.anchorMin=trackRect.anchorMax=trackRect.pivot=new Vector2(0,1);
            trackRect.anchoredPosition=new Vector2(20,-150);trackRect.sizeDelta=new Vector2(325,8);track.GetComponent<UnityEngine.UI.Image>().color=new Color(.03f,.1f,.09f);track.GetComponent<UnityEngine.UI.Image>().raycastTarget=false;
            var fill=new GameObject("Quest progress fill",typeof(RectTransform),typeof(UnityEngine.UI.Image));
            var fillRect=fill.GetComponent<RectTransform>();fillRect.SetParent(trackRect,false);fillRect.anchorMin=Vector2.zero;fillRect.anchorMax=Vector2.one;fillRect.offsetMin=fillRect.offsetMax=Vector2.zero;
            progressFill=fill.GetComponent<UnityEngine.UI.Image>();progressFill.color=new Color(.73f,.6f,.32f);progressFill.raycastTarget=false;
            questProgress=Label("Quest progress",card,"",new Vector2(20,-164),new Vector2(325,22),14,new Color(.92f,.84f,.65f));

            RectTransform bottom = Panel("Action bar", Vector2.zero, new Vector2(1, 0), Vector2.zero, Vector2.zero, new Vector2(0, 180), new Color(.06f, .16f, .15f, .97f));
            target = Label("Target", bottom, "", new Vector2(28, -14), new Vector2(1540, 32), 23, new Color(.98f,.87f,.65f));
            message = Label("Message", bottom, "", new Vector2(28, -50), new Vector2(1535, 35), 18, new Color(.87f,.86f,.74f));
            actionButton = Button("Interact", bottom, "E  种植", 28, game.Interact, out actionLabel);
            waterButton = Button("Water", bottom, "R  浇水", 234, game.Water, out _);
            sleepButton = Button("Sleep", bottom, "F  结束一天", 440, game.Sleep, out _);
            buyButton = Button("Buy", bottom, "B  种子 · 5 金币", 646, game.BuySeed, out buyLabel);
            Button("Journal", bottom, "Q  冒险日志", 852, ()=>game.Adventure.Hud.Show("journal"), out _);
            Button("Map", bottom, "M  旅途地图", 1058, ()=>game.Adventure.Hud.Show("map"), out _);
            Label("Movement hint", bottom, "WASD 移动 · C 伙伴 · T 种子\nI 行囊 · 滚轮缩放 · 自动保存", new Vector2(1280, -104), new Vector2(300, 52), 16, new Color(.86f,.84f,.72f));
            var adventureHud = gameObject.AddComponent<AdventureHud>();
            adventureHud.Create(game.Adventure,font);
        }

        public void Refresh()
        {
            FarmState state = game.State;
            int selected = game.SelectedPlot;
            CropState crop = selected < 0 ? null : state.plots[selected];
            string signature = game.Adventure.Revision+"/"+game.World.Adventure.Region+"/"+game.Adventure.TargetHint+"/"+$"{state.day}/{state.coins}/{state.seeds}/{state.harvests}/{state.energy}/{state.selectedCrop}/{state.SelectedSeeds}/{selected}/{crop?.planted}/{crop?.watered}/{crop?.growth}/{game.NearHouse}/{game.SaveHealthy}/{game.Message}";
            if (signature == previous) return;
            previous = signature;
            var a=state.adventure;
            counters.text = $"{AdventureContent.Regions[game.World.Adventure.Region]} · 第 {state.day} 天 {state.Season} · {(state.Rainy?"雨天":"晴天")}   体力 {state.energy}/30 金币 {state.coins}";
            int[] values={a.wood,a.stone,a.berries,a.fish,a.crystals,a.science};
            string[] names={"木材","石料","浆果","鲜鱼","晶石","知识"};
            for(int i=0;i<6;i++)resources[i].text=names[i]+" "+values[i];
            objective.text = AdventureContent.Quests[a.quest];
            float progress=game.Adventure.QuestProgress;
            progressFill.rectTransform.anchorMax=new Vector2(progress,1);
            questProgress.text=a.quest==0?Mathf.Min(state.harvests,3)+" / 3":a.quest==6?"纪元开启":Mathf.RoundToInt(progress*100)+"%";
            message.text = game.Message + (game.SaveHealthy ? "" : "  保存失败，请检查磁盘权限。");
            target.text = crop == null ? (game.NearHouse ? "小屋 · 休息与种子商店" : "探索农场 · 走近农田或小屋") :
                $"农田 {selected + 1} · " + (!crop.planted ? "空田" : crop.Ready ? FarmState.CropNames[crop.kind]+"成熟，可以收获" : $"{FarmState.CropNames[crop.kind]} {crop.growth}/{crop.RequiredDays} · " + (crop.watered ? "今天已浇水" : "等待浇水"));
            if(selected>=12 && !a.expandedFarm)target.text="南侧农田 · 需要扩建农场";
            buyLabel.text="B "+FarmState.CropNames[state.selectedCrop]+" · "+state.SeedCost+"金";
            if(game.Adventure.Target!=null || game.Adventure.InBattle) target.text=game.Adventure.TargetHint;
            actionLabel.text = game.Adventure.Target!=null ? "E  交互" : crop != null && crop.Ready ? "E  收获" : "E  种植";
            actionButton.interactable = !game.Adventure.InBattle && (game.Adventure.Target!=null || (crop != null && (crop.Ready || (!crop.planted && state.SelectedSeeds > 0 && (selected<12||a.expandedFarm)))));
            waterButton.interactable = !game.Adventure.InBattle && crop != null && crop.planted && !crop.watered && !crop.Ready;
            sleepButton.interactable = !game.Adventure.InBattle && game.NearHouse;
            buyButton.interactable = !game.Adventure.InBattle && (game.NearHouse || game.Adventure.Target?.kind==EncounterKind.Market) && state.coins >= state.SeedCost;
            game.Adventure.Hud.Refresh();
        }

        void Icon(Transform parent,Vector2 position,Vector2 size,int symbol,Color color)
        {
            var go=new GameObject("HUD emblem",typeof(RectTransform),typeof(CanvasRenderer),typeof(HudOrnament));var r=go.GetComponent<RectTransform>();
            r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=position;r.sizeDelta=size;
            var g=go.GetComponent<HudOrnament>();g.symbol=symbol;g.color=color;g.raycastTarget=false;
        }

        RectTransform Panel(string name, Vector2 min, Vector2 max, Vector2 pivot, Vector2 position, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(transform, false);
            rect.anchorMin = min; rect.anchorMax = max; rect.pivot = pivot;
            rect.anchoredPosition = position; rect.sizeDelta = size;
            var image = go.GetComponent<UnityEngine.UI.Image>();
            image.color = color; image.raycastTarget = false;
            return rect;
        }

        TextMeshProUGUI Label(string name, Transform parent, string value, Vector2 position, Vector2 size, float fontSize, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = position; rect.sizeDelta = size;
            var text = go.GetComponent<TextMeshProUGUI>();
            text.font = font; text.text = value; text.fontSize = fontSize; text.color = color;
            text.enableAutoSizing = false; text.raycastTarget = false;
            return text;
        }

        UnityEngine.UI.Button Button(string name, Transform parent, string label, float x, UnityEngine.Events.UnityAction action, out TextMeshProUGUI caption)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -102); rect.sizeDelta = new Vector2(188, 52);
            var image = go.GetComponent<UnityEngine.UI.Image>();
            image.color = x<850 ? new Color(.33f,.25f,.17f) : new Color(.12f,.26f,.22f); image.raycastTarget = true;
            var button = go.GetComponent<UnityEngine.UI.Button>();
            button.targetGraphic = image; button.onClick.AddListener(action);
            var colors = button.colors;
            colors.disabledColor = new Color(.58f, .6f, .53f, .85f);
            colors.highlightedColor = new Color(1, .93f, .72f);
            button.colors = colors;
            caption = Label("Caption", rect, label, new Vector2(8, -9), new Vector2(172, 34), 19, new Color(.98f,.86f,.63f));
            HudOrnament.Border(rect);
            caption.alignment = TextAlignmentOptions.Center;
            return button;
        }
    }
}
