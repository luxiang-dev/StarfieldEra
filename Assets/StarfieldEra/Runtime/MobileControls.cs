using TMPro;
using UnityEngine;

namespace StarfieldEra
{
    public sealed class MobileControls : MonoBehaviour
    {
        public static bool IsTouchMode => Application.isMobilePlatform || current;
        public static Vector2 Movement => current && current.stick ? current.stick.Value : Vector2.zero;
        static MobileControls current;
        FarmGame game;
        MobileStick stick;
        RectTransform safe, world;
        TMP_FontAsset font;
        TextMeshProUGUI waterLabel;
        public void Create(FarmGame owner, TMP_FontAsset asset)
        {
            current = this; game = owner; font = asset;
            if(Application.isMobilePlatform)Application.targetFrameRate=30;
            safe = Rect("Mobile safe area", transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            world = Rect("Touch actions", safe, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var pad = Rect("Movement stick", world, Vector2.zero, Vector2.zero, new Vector2(125, 245), Vector2.one * 190);
            var background = pad.gameObject.AddComponent<UnityEngine.UI.Image>(); background.color = new Color(.08f,.20f,.17f,.8f);
            stick = pad.gameObject.AddComponent<MobileStick>();
            stick.knob = Rect("Stick knob", pad, Vector2.one * .5f, Vector2.one * .5f, Vector2.zero, Vector2.one * 76);
            var knobImage = stick.knob.gameObject.AddComponent<UnityEngine.UI.Image>(); knobImage.color = new Color(.91f,.79f,.5f,.9f); knobImage.raycastTarget = false;
            AddButton("交互",0,0,()=>{if(game.Expedition.Fishing)game.Expedition.Reel();else game.Interact();});
            waterLabel = AddButton("浇水",1,0,()=>{if(game.Construction.Active)game.Construction.Rotate();else game.Water();});
            AddButton("建造",0,1,game.Construction.Open);
            AddButton("土地",1,1,()=>game.Management.Open("land"));
            AddButton("种子",0,2,game.CycleCrop);
            AddButton("购买",1,2,game.BuySeed);
            AddButton("休息",0,3,game.Sleep);
            AddButton("取消",1,3,Cancel);
            AddButton("跳跃",0,4,game.RequestJump);
            AddButton("奔跑",1,4,()=>{game.SprintRequested=!game.SprintRequested;});

        }
        void Cancel()
        {
            if(game.Management.IsOpen){game.Management.Close();return;}
            if(game.Expedition.Fishing){game.Expedition.CancelFishing();return;}
            game.Night?.CancelTrap();game.Construction.Cancel(); game.Fences.Cancel(); game.Land.Cancel();
            if(!game.Adventure.InBattle)game.Adventure.Hud.Hide();
        }
        void Update()
        {
            if(!safe)return;
            var area=Screen.safeArea;
            safe.anchorMin=new Vector2(area.xMin/Screen.width,area.yMin/Screen.height);
            safe.anchorMax=new Vector2(area.xMax/Screen.width,area.yMax/Screen.height);
            world.gameObject.SetActive(!game.Management.IsOpen&&!game.Adventure.InBattle&&!game.Adventure.Hud.IsContextOpen);
            stick.enabled=!game.Expedition.Fishing;
            if(game.Expedition.Fishing)stick.gameObject.SetActive(false);else stick.gameObject.SetActive(true);
            waterLabel.text=game.Night&&game.Night.Active?"修补":game.Construction.Active?"旋转":"浇水";
        }
        TextMeshProUGUI AddButton(string text,int column,int row,UnityEngine.Events.UnityAction action)
        {
            var rect=Button(world,text,new Vector2(1,0),new Vector2(-210+column*135,150+row*78),action);
            return rect.GetComponentInChildren<TextMeshProUGUI>();
        }
        RectTransform Button(Transform parent,string text,Vector2 anchor,Vector2 position,UnityEngine.Events.UnityAction action)
        {
            var rect=Rect(text,parent,anchor,anchor,position,new Vector2(122,68));
            var image=rect.gameObject.AddComponent<UnityEngine.UI.Image>();image.color=new Color(.08f,.22f,.18f,.94f);
            var button=rect.gameObject.AddComponent<UnityEngine.UI.Button>();button.targetGraphic=image;button.onClick.AddListener(action);
            var label=Rect("Caption",rect,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero).gameObject.AddComponent<TextMeshProUGUI>();
            label.font=font;label.fontSize=24;label.text=text;label.alignment=TextAlignmentOptions.Center;label.raycastTarget=false;
            return rect;
        }
        static RectTransform Rect(string name,Transform parent,Vector2 min,Vector2 max,Vector2 position,Vector2 size)
        {
            var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);
            r.anchorMin=min;r.anchorMax=max;r.pivot=Vector2.one*.5f;r.anchoredPosition=position;r.sizeDelta=size;return r;
        }
        void OnDestroy(){if(game)game.SprintRequested=false;if(current==this)current=null;}
    }
}
