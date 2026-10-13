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
        TextMeshProUGUI actionLabel;
        string previous;
        readonly Color ink = new Color(.16f, .24f, .25f);

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

            RectTransform header = Panel("Header", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), Vector2.zero, new Vector2(0, 116), new Color(.95f, .94f, .86f, .97f));
            Label("Title", header, "星野纪元", new Vector2(30, -16), new Vector2(340, 42), 31, ink);
            Label("Subtitle", header, "STARFIELD ERA  /  经营 · 探索 · 伙伴", new Vector2(32, -61), new Vector2(420, 25), 15, ink);
            counters = Label("Counters", header, "", new Vector2(490, -20), new Vector2(1070, 88), 18, ink);

            RectTransform card = Panel("Objective", new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -140), new Vector2(345, 165), new Color(.16f, .24f, .25f, .94f));
            Label("Objective title", card, "晨风村 · 复兴之旅", new Vector2(20, -16), new Vector2(290, 32), 24, new Color(1, .88f, .62f));
            objective = Label("Objective body", card, "", new Vector2(20, -58), new Vector2(310, 94), 17, Color.white);

            RectTransform bottom = Panel("Action bar", Vector2.zero, new Vector2(1, 0), Vector2.zero, Vector2.zero, new Vector2(0, 180), new Color(.95f, .94f, .86f, .97f));
            target = Label("Target", bottom, "", new Vector2(28, -14), new Vector2(1540, 32), 23, ink);
            message = Label("Message", bottom, "", new Vector2(28, -50), new Vector2(1535, 35), 18, ink);
            actionButton = Button("Interact", bottom, "E  种植", 28, game.Interact, out actionLabel);
            waterButton = Button("Water", bottom, "R  浇水", 234, game.Water, out _);
            sleepButton = Button("Sleep", bottom, "F  结束一天", 440, game.Sleep, out _);
            buyButton = Button("Buy", bottom, "B  种子 · 5 金币", 646, game.BuySeed, out _);
            Button("Journal", bottom, "Q  冒险日志", 852, ()=>game.Adventure.Hud.Show("journal"), out _);
            Button("Map", bottom, "M  旅途地图", 1058, ()=>game.Adventure.Hud.Show("map"), out _);
            Label("Movement hint", bottom, "WASD 移动 · C 切换伙伴\n滚轮缩放 · 自动保存", new Vector2(1280, -104), new Vector2(300, 52), 16, ink);
            var adventureHud = gameObject.AddComponent<AdventureHud>();
            adventureHud.Create(game.Adventure,font);
        }

        public void Refresh()
        {
            FarmState state = game.State;
            int selected = game.SelectedPlot;
            CropState crop = selected < 0 ? null : state.plots[selected];
            string signature = game.Adventure.Revision+"/"+game.World.Adventure.Region+"/"+game.Adventure.TargetHint+"/"+$"{state.day}/{state.coins}/{state.seeds}/{state.harvests}/{selected}/{crop?.planted}/{crop?.watered}/{crop?.growth}/{game.NearHouse}/{game.SaveHealthy}/{game.Message}";
            if (signature == previous) return;
            previous = signature;
            var a=state.adventure;
            counters.text = $"{AdventureContent.Regions[game.World.Adventure.Region]} · 第 {state.day} 天   金币 {state.coins}   种子 {state.seeds}   伙伴 {a.CompanionCount}/3\n木材 {a.wood}   石料 {a.stone}   浆果 {a.berries}   鲜鱼 {a.fish}   晶石 {a.crystals}   知识 {a.science}";
            objective.text = AdventureContent.Quests[a.quest];
            message.text = game.Message + (game.SaveHealthy ? "" : "  保存失败，请检查磁盘权限。");
            target.text = crop == null ? (game.NearHouse ? "小屋 · 休息与种子商店" : "探索农场 · 走近农田或小屋") :
                $"农田 {selected + 1} · " + (!crop.planted ? "空田" : crop.Ready ? "金萝卜成熟，可以收获" : $"金萝卜 {crop.growth}/2 · " + (crop.watered ? "今天已浇水" : "等待浇水"));
            if(game.Adventure.Target!=null || game.Adventure.InBattle) target.text=game.Adventure.TargetHint;
            actionLabel.text = game.Adventure.Target!=null ? "E  交互" : crop != null && crop.Ready ? "E  收获 · +12" : "E  种植";
            actionButton.interactable = !game.Adventure.InBattle && (game.Adventure.Target!=null || (crop != null && (crop.Ready || (!crop.planted && state.seeds > 0))));
            waterButton.interactable = !game.Adventure.InBattle && crop != null && crop.planted && !crop.watered && !crop.Ready;
            sleepButton.interactable = !game.Adventure.InBattle && game.NearHouse;
            buyButton.interactable = !game.Adventure.InBattle && game.NearHouse && state.coins >= FarmState.SeedPrice;
            game.Adventure.Hud.Refresh();
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
            image.color = new Color(.22f, .37f, .32f); image.raycastTarget = true;
            var button = go.GetComponent<UnityEngine.UI.Button>();
            button.targetGraphic = image; button.onClick.AddListener(action);
            var colors = button.colors;
            colors.disabledColor = new Color(.62f, .65f, .6f, .5f);
            colors.highlightedColor = new Color(1, .93f, .72f);
            button.colors = colors;
            caption = Label("Caption", rect, label, new Vector2(8, -9), new Vector2(172, 34), 19, Color.white);
            caption.alignment = TextAlignmentOptions.Center;
            return button;
        }
    }
}
