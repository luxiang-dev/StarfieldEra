using TMPro;
using UnityEngine;
namespace StarfieldEra
{
    /// <summary>One screen canvas for nearby actors; bars never intercept picking.</summary>
    public sealed class HealthBars:MonoBehaviour
    {
        FarmGame game;GardenActor actor;RaidThreat threat;Transform subject;string displayName;RectTransform rect;UnityEngine.UI.Image fill;TextMeshProUGUI label;
        public void Initialize(FarmGame owner,GardenActor target,Transform canvas)
        {
            actor=target;subject=target.transform;displayName=target.DisplayName;Create(owner,canvas,target.zombie!=null);
        }
        public void InitializeWolf(FarmGame owner,RaidThreat record,Transform target,Transform canvas)
        {threat=record;subject=target;displayName="野狼";Create(owner,canvas,true);}
        void Create(FarmGame owner,Transform canvas,bool hostile)
        {
            game=owner;rect=new GameObject("Health - "+displayName,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(canvas,false);rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.one*.5f;rect.sizeDelta=new Vector2(116,36);
            var track=Image(rect,"Health border",new Vector2(0,-12),new Vector2(116,10),new Color(.04f,.09f,.07f,.9f));
            fill=Image(track.transform,"Health fill",Vector2.zero,new Vector2(112,6),hostile?new Color(.85f,.25f,.20f):new Color(.44f,.75f,.30f));
            UITheme.Round(track);fill.sprite=track.sprite;
            fill.type=UnityEngine.UI.Image.Type.Filled;fill.fillMethod=UnityEngine.UI.Image.FillMethod.Horizontal;
            label=new GameObject("Health value",typeof(RectTransform),typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();label.transform.SetParent(rect,false);label.rectTransform.sizeDelta=new Vector2(160,24);label.rectTransform.anchoredPosition=new Vector2(0,8);label.font=owner.interfaceFont;label.fontSize=14;label.alignment=TextAlignmentOptions.Center;label.color=new Color(1,.95f,.80f);label.outlineWidth=.18f;label.raycastTarget=false;
        }
        static UnityEngine.UI.Image Image(Transform parent,string name,Vector2 position,Vector2 size,Color color)
        {var i=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image)).GetComponent<UnityEngine.UI.Image>();i.transform.SetParent(parent,false);i.rectTransform.sizeDelta=size;i.rectTransform.anchoredPosition=position;i.color=color;i.raycastTarget=false;return i;}
        void LateUpdate()
        {
            if(!subject||!rect)return;var target=subject.position+Vector3.up*(threat!=null?1.3f:actor.unit!=null&&actor.unit.kind==1?2.5f:2.1f);
            var point=game.World.View.WorldToScreenPoint(target);bool visible=point.z>0&&Vector3.Distance(game.World.Player.position,subject.position)<30&&!game.Management.IsOpen;
            rect.gameObject.SetActive(visible);if(!visible)return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)rect.parent,point,null,out var position);rect.anchoredPosition=position;
            int health=threat!=null?threat.hp:actor.Health,max=threat!=null?3:actor.MaxHealth;fill.fillAmount=health/(float)max;label.text=displayName+" "+health+"/"+max;
        }
        void OnDestroy(){if(rect)Destroy(rect.gameObject);}
    }
}
