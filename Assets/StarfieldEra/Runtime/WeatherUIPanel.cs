using TMPro;
using UnityEngine;
namespace StarfieldEra
{
    public sealed class WeatherUIPanel:MonoBehaviour
    {
        FarmGame game;TMP_FontAsset font;static Sprite fillSprite;
        readonly Color paper=new Color(.94f,.89f,.77f),ink=new Color(.22f,.25f,.22f),gold=new Color(.78f,.60f,.29f),muted=new Color(.53f,.54f,.47f),card=new Color(.88f,.84f,.73f);
        public void Build(FarmGame owner,TMP_FontAsset asset)
        {
            game=owner;font=asset;var state=game.State.weather;var today=WeatherCatalog.Get(game.State.Weather);var tomorrow=WeatherCatalog.Get(game.State.TomorrowWeather);
            var left=Box(transform,"Today weather details",0,0,350,586,paper);HudOrnament.Border(left);
            Text(left,"今日天气",24,16,302,30,21,ink);Text(left,"第 "+game.State.day+" 天 · 100%实况",24,50,302,26,15,muted);
            Icon(left,today.fullColorIcon,87,72,176);
            Text(left,today.displayName,24,244,302,43,32,ink,TextAlignmentOptions.Center);
            Text(left,today.temperatureRange.x+"～"+today.temperatureRange.y+" ℃    "+today.windDescription,22,294,306,48,16,ink,TextAlignmentOptions.Center);
            var buff=Box(left,"Today's buff card",20,351,310,92,new Color(.79f,.83f,.68f));Text(buff,"今日增益",14,10,280,25,18,ink);Text(buff,today.buffDescription,14,40,280,51,17,ink);
            var next=Box(left,"Tomorrow accurate forecast",20,459,310,106,card);Icon(next,tomorrow.fullColorIcon,9,15,76);
            Text(next,"明日 · 100%准确",92,12,204,25,16,muted);Text(next,tomorrow.displayName,92,41,204,31,24,ink);Text(next,tomorrow.temperatureRange.x+"～"+tomorrow.temperatureRange.y+" ℃",92,78,204,23,15,ink);
            var right=Box(transform,"Experience forecasting",370,0,790,586,new Color(.10f,.22f,.20f));
            Text(right,"预知未来",20,10,336,36,27,paper);Text(right,"凭经验判断天气 · 等级 "+state.weatherSkillLevel+" / 3",20,52,750,27,17,paper);
            Text(right,"生存经验 "+state.survivalExperience+"    星石 "+state.starStones,20,88,750,25,17,paper);
            Text(right,"收获与采集积累经验；晶石矿与守卫产出星石",20,115,750,20,12,paper);
            bool max=state.weatherSkillLevel==3;string cost=max?"已达最高等级":"升级 · 经验 "+WeatherState.ExperienceCosts[state.weatherSkillLevel]+" / 星石 "+WeatherState.StarStoneCosts[state.weatherSkillLevel];
            Button(right,cost,355,18,415,46,game.WeatherManager.UpgradeSkill,!max&&state.survivalExperience>=WeatherState.ExperienceCosts[state.weatherSkillLevel]&&state.starStones>=WeatherState.StarStoneCosts[state.weatherSkillLevel]);
            Progress(right,"Unlocked forecast days",30,144,720,5,state.weatherSkillLevel/3f,gold,new Color(.26f,.36f,.30f));
            for(int i=0;i<4;i++)
            {float x=24+i*237;var node=Box(right,"Forecast unlock node "+i,x,134,21,21,i<=state.weatherSkillLevel?gold:new Color(.28f,.36f,.31f));Text(right,i==0?"未解锁":"预判 "+i+" 天",Mathf.Min(x-12,654),165,126,25,14,i<=state.weatherSkillLevel?paper:muted);}
            for(int i=0;i<3;i++)ForecastSlot(right,i+3,i*258+8,207,250,258);
            var note=Box(right,"Farmer experience note",12,482,766,91,new Color(.91f,.85f,.67f));
            Text(note,"经验分析 · 老农说",15,9,736,25,17,ink);Text(note,WeatherCatalog.Get(state.trueWeatherQueue[2]).experienceProverb,15,39,736,44,18,ink);
        }
        void ForecastSlot(Transform parent,int nthDay,float x,float y,float width,float height)
        {
            var state=game.State.weather;bool unlocked=state.CanPredict(nthDay);
            var slot=Box(parent,"Forecast day "+nthDay,x,y,width,height,unlocked?paper:new Color(.22f,.31f,.27f));HudOrnament.Border(slot);
            Text(slot,"第"+nthDay+"天 · "+(nthDay==3?"后天":(nthDay-1)+"天后"),12,10,width-24,29,19,unlocked?ink:paper,TextAlignmentOptions.Center);
            if(!unlocked)
            {Text(slot,"?",12,55,width-24,90,69,muted,TextAlignmentOptions.Center);Text(slot,"等级 "+(nthDay-2)+" 解锁\n尚不能凭经验判断",12,166,width-24,65,18,paper,TextAlignmentOptions.Center);return;}
            var prediction=state.Predict(nthDay);Icon(slot,WeatherCatalog.Get(prediction.main).sketchIcon,(width-92)/2,44,92);
            Probability(slot,"Main probability",prediction.main,prediction.mainPercent,147,width);
            Probability(slot,"Secondary probability",prediction.secondary,prediction.SecondaryPercent,196,width);
        }
        void Probability(Transform parent,string name,WeatherType type,int percent,float y,float width)
        {Text(parent,WeatherCatalog.Get(type).displayName+"  概率 "+percent+"%",15,y,width-30,27,17,ink);Progress(parent,name,15,y+29,width-30,7,percent/100f,new Color(.31f,.48f,.40f),new Color(.72f,.73f,.62f));}
        void Icon(Transform parent,Sprite sprite,float x,float y,float size)
        {var box=Box(parent,"Weather icon",x,y,size,size,Color.white);var image=box.GetComponent<UnityEngine.UI.Image>();image.sprite=sprite;image.preserveAspect=true;if(!sprite)image.color=new Color(0,0,0,0);}
        void Progress(Transform parent,string name,float x,float y,float width,float height,float value,Color fill,Color track)
        {var root=Box(parent,name+" track",x,y,width,height,track);var image=Box(root,name+" fill",0,0,width,height,fill).GetComponent<UnityEngine.UI.Image>();if(!fillSprite)fillSprite=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,1,1),Vector2.one*.5f);image.sprite=fillSprite;image.type=UnityEngine.UI.Image.Type.Filled;image.fillMethod=UnityEngine.UI.Image.FillMethod.Horizontal;image.fillOrigin=0;image.fillAmount=value;}
        RectTransform Box(Transform parent,string name,float x,float y,float width,float height,Color color)
        {var rect=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image)).GetComponent<RectTransform>();rect.SetParent(parent,false);rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(width,height);var image=rect.GetComponent<UnityEngine.UI.Image>();image.color=color;image.raycastTarget=false;return rect;}
        TextMeshProUGUI Text(Transform parent,string value,float x,float y,float width,float height,float size,Color color,TextAlignmentOptions alignment=TextAlignmentOptions.TopLeft)
        {var text=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();text.transform.SetParent(parent,false);text.rectTransform.anchorMin=text.rectTransform.anchorMax=text.rectTransform.pivot=new Vector2(0,1);text.rectTransform.anchoredPosition=new Vector2(x,-y);text.rectTransform.sizeDelta=new Vector2(width,Mathf.Max(height,size*1.52f));text.font=font;text.text=value;text.fontSize=size;text.color=color;text.alignment=alignment;text.raycastTarget=false;text.overflowMode=TextOverflowModes.Ellipsis;return text;}
        void Button(Transform parent,string caption,float x,float y,float width,float height,UnityEngine.Events.UnityAction action,bool enabled)
        {var rect=Box(parent,caption,x,y,width,height,new Color(.30f,.40f,.29f));rect.GetComponent<UnityEngine.UI.Image>().raycastTarget=true;var button=rect.gameObject.AddComponent<UnityEngine.UI.Button>();button.targetGraphic=rect.GetComponent<UnityEngine.UI.Image>();button.interactable=enabled;button.onClick.AddListener(action);Text(rect,caption,9,7,width-18,height-14,18,paper,TextAlignmentOptions.Center);HudOrnament.Border(rect);}
    }
    public sealed partial class ManagementHud
    {
        void WeatherPage()
        {var root=new GameObject("Weather panel",typeof(RectTransform),typeof(WeatherUIPanel));var rect=root.GetComponent<RectTransform>();rect.SetParent(content,false);rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;root.GetComponent<WeatherUIPanel>().Build(game,font);}
    }
}
