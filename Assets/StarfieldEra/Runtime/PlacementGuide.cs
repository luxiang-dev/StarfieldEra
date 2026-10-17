using TMPro;
using UnityEngine;
namespace StarfieldEra
{
    public sealed class PlacementGuide:MonoBehaviour
    {
        FarmGame game;RectTransform root;TextMeshProUGUI title,details;UnityEngine.UI.Button rotate;
        public void Initialize(FarmGame owner,TMP_FontAsset font)
        {
            game=owner;
            root=new GameObject("Placement guide",typeof(RectTransform),typeof(UnityEngine.UI.Image)).GetComponent<RectTransform>();root.SetParent(transform,false);root.anchorMin=root.anchorMax=root.pivot=Vector2.zero;root.anchoredPosition=new Vector2(24,142);root.sizeDelta=new Vector2(572,218);
            var bg=root.GetComponent<UnityEngine.UI.Image>();bg.color=new Color(.035f,.11f,.095f,.9f);UITheme.Round(bg);UITheme.Frame(root);UITheme.Icon(root,17,14,12,42,42);
            title=Label(root,"",70,12,480,42,21,font);details=Label(root,"",20,62,530,100,17,font);
            rotate=Action("旋转 · R",20,()=>game.Construction.Rotate(),font);Action("取消 · 右键",294,()=>{game.Construction.Cancel();game.Fences.Cancel();},font);
            root.gameObject.SetActive(false);
        }
        void Update()
        {
            if(!game)return;bool active=game.Construction.Active||game.Fences.Active;
            root.gameObject.SetActive(active);if(!active)return;
            title.text=game.Construction.Active?(game.Construction.Relocating?"移动与旋转":"建筑选址"):"围栏连续建造";
            details.text=game.Construction.Active?game.Construction.PreviewDescription+"\n"+game.Construction.Hint:game.Fences.Hint+"\n按住左键拖动，松开建造；也可点选起点与终点。";
            rotate.interactable=game.Construction.CanRotate;
        }
        TextMeshProUGUI Label(Transform parent,string text,float x,float y,float w,float h,float size,TMP_FontAsset font)
        {var t=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();t.rectTransform.SetParent(parent,false);t.rectTransform.anchorMin=t.rectTransform.anchorMax=t.rectTransform.pivot=new Vector2(0,1);t.rectTransform.anchoredPosition=new Vector2(x,-y);t.rectTransform.sizeDelta=new Vector2(w,h);t.font=font;t.fontSize=size;t.color=UITheme.Paper;t.text=text;t.raycastTarget=false;return t;}
        UnityEngine.UI.Button Action(string label,float x,UnityEngine.Events.UnityAction action,TMP_FontAsset font)
        {var r=new GameObject(label,typeof(RectTransform),typeof(UnityEngine.UI.Image),typeof(UnityEngine.UI.Button)).GetComponent<RectTransform>();r.SetParent(root,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-174);r.sizeDelta=new Vector2(258,32);var image=r.GetComponent<UnityEngine.UI.Image>();image.color=UITheme.Card;UITheme.Round(image);var b=r.GetComponent<UnityEngine.UI.Button>();b.targetGraphic=image;b.onClick.AddListener(action);UITheme.Button(b);Label(r,label,4,2,250,28,17,font).alignment=TextAlignmentOptions.Center;return b;}
    }
}
