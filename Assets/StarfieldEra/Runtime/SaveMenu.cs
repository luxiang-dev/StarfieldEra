using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
namespace StarfieldEra
{
    /// <summary>Startup and in-world save selection, independent of the handbook.</summary>
    public sealed class SaveMenu:MonoBehaviour
    {
        static bool resumeAfterReload;
        FarmGame game;GameObject panel;RectTransform body;TMP_FontAsset font;bool loading;float previousScale=1;
        public bool IsOpen=>panel&&panel.activeSelf;
        public void Initialize(FarmGame owner)
        {
            game=owner;font=game.interfaceFont;
            var go=new GameObject("Save selection",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler),typeof(UnityEngine.UI.GraphicRaycaster));go.transform.SetParent(transform,false);panel=go;
            var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=500;
            var scaler=go.GetComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1440,820);scaler.matchWidthOrHeight=.5f;
            var shade=go.AddComponent<UnityEngine.UI.Image>();shade.color=new Color(.025f,.07f,.065f,.94f);
            var sheet=new GameObject("Save cards",typeof(RectTransform),typeof(UnityEngine.UI.Image));sheet.transform.SetParent(go.transform,false);body=sheet.GetComponent<RectTransform>();body.sizeDelta=new Vector2(820,650);body.anchorMin=body.anchorMax=body.pivot=new Vector2(.5f,.5f);sheet.GetComponent<UnityEngine.UI.Image>().color=new Color(.09f,.17f,.15f,.98f);
            if(!FindFirstObjectByType<EventSystem>()){var events=new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));events.transform.SetParent(transform);events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();}
            Show();
            if(resumeAfterReload){resumeAfterReload=false;Load(SaveSlots.Active,false);}
        }
        void Clear(){foreach(Transform child in body){child.gameObject.SetActive(false);Destroy(child.gameObject);}}
        RectTransform Rect(string name,float y,float h)
        {
            var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(body,false);var r=go.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,1);r.anchoredPosition=new Vector2(0,-y);r.sizeDelta=new Vector2(740,h);return r;
        }
        void Label(string value,float y,float h=42,int size=24)
        {var t=Rect("Label",y,h).gameObject.AddComponent<TextMeshProUGUI>();t.font=font;t.text=value;t.fontSize=size;t.color=new Color(.96f,.87f,.67f);t.alignment=TextAlignmentOptions.Center;t.raycastTarget=false;}
        void Button(string value,float y,Action action)
        {
            var r=Rect(value,y,66);var image=r.gameObject.AddComponent<UnityEngine.UI.Image>();image.color=new Color(.17f,.29f,.24f);var b=r.gameObject.AddComponent<UnityEngine.UI.Button>();b.targetGraphic=image;b.onClick.AddListener(()=>action());
            var child=new GameObject("Title",typeof(RectTransform),typeof(TextMeshProUGUI));child.transform.SetParent(r,false);var cr=child.GetComponent<RectTransform>();cr.anchorMin=Vector2.zero;cr.anchorMax=Vector2.one;cr.offsetMin=Vector2.zero;cr.offsetMax=Vector2.zero;var t=child.GetComponent<TextMeshProUGUI>();t.font=font;t.text=value;t.fontSize=23;t.color=new Color(.98f,.9f,.73f);t.alignment=TextAlignmentOptions.Center;t.raycastTarget=false;
        }
        public void Show()
        {
            if(loading)return;
            if(!IsOpen){previousScale=Time.timeScale;panel.SetActive(true);}Time.timeScale=0;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;Clear();
            Label("旅程存档",28,55,34);Label("选择旅程 · 每个存档独立保存",90);
            for(int i=0;i<SaveSlots.Count;i++){int slot=i;Button((slot==SaveSlots.Active?"当前 · ":"")+"存档 "+(slot+1)+"    "+SaveSlots.Description(slot),155+i*82,()=>Choose(slot));}
            if(game.Ready){Button("返回游戏",412,Close);Button("重新开始当前存档",491,()=>Confirm(SaveSlots.Active));}
            Label("F5 存档菜单 · 自动保存 · 重新开始会备份旧进度",582,45,20);
        }
        void Choose(int slot)
        {
            if(game.Ready&&slot==SaveSlots.Active){Close();return;}
            if(SaveSlots.Exists(slot)&&!FarmSave.TryLoad(SaveSlots.PathFor(slot),out _)){Confirm(slot);return;}
            Load(slot,false);
        }
        void Confirm(int slot)
        {Clear();Label("重新开始旅程？",70,60,32);Label("旧进度将保留在 Archives 备份目录。",165);Label("新旅程从第1天开始，金币、木材、石料各1000。",220,60,22);Button("确认重新开始",335,()=>Load(slot,true));Button("取消",425,Show);}
        void Load(int slot,bool restart)
        {
            try
            {
                if(game.Ready){game.FlushSave();if(!game.SaveHealthy){Error("保存失败，请检查磁盘空间后重试。");return;}}
                if(restart)SaveSlots.Restart(slot);
                SaveSlots.Active=slot;loading=true;Clear();Label("正在准备旅程…",215,70,34);Label("生成地形与营地，首次加载模型可能需要时间。",310,60,22);
                Time.timeScale=1;
                if(game.Ready){game.PrepareReload();resumeAfterReload=true;SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex);}else game.BeginWorld();
            }
            catch(Exception e) when(e is System.IO.IOException||e is UnauthorizedAccessException){Error("存档操作失败："+e.Message);}
        }
        void Error(string message){loading=false;Clear();Label(message,160,180,22);Button("返回",420,Show);}
        public void Loaded(){loading=false;panel.SetActive(false);Time.timeScale=previousScale;}
        void Close(){if(!game.Ready)return;panel.SetActive(false);Time.timeScale=previousScale;}
        void Update(){if(Keyboard.current==null||loading)return;if(Keyboard.current.f5Key.wasPressedThisFrame){if(IsOpen&&game.Ready)Close();else Show();}else if(IsOpen&&game.Ready&&Keyboard.current.escapeKey.wasPressedThisFrame)Close();}
        void OnDestroy(){Time.timeScale=1;}
    }
}
