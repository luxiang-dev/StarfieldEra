using UnityEngine;

namespace StarfieldEra
{
    // Crisp vector ornamentation keeps the HUD readable at different window sizes.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class HudOrnament : UnityEngine.UI.MaskableGraphic
    {
        public bool frame;
        public int symbol;
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper helper)
        {
            helper.Clear(); var rect=rectTransform.rect;
            if(frame)
            {
                const float bevel=7, border=1.4f;
                var outer=new[]{new Vector2(rect.xMin+bevel,rect.yMin),new Vector2(rect.xMax-bevel,rect.yMin),new Vector2(rect.xMax,rect.yMin+bevel),new Vector2(rect.xMax,rect.yMax-bevel),new Vector2(rect.xMax-bevel,rect.yMax),new Vector2(rect.xMin+bevel,rect.yMax),new Vector2(rect.xMin,rect.yMax-bevel),new Vector2(rect.xMin,rect.yMin+bevel)};
                for(int i=0;i<8;i++)
                {
                    var a=outer[i];var b=outer[(i+1)%8];
                    var ai=Vector2.MoveTowards(a,rect.center,border);var bi=Vector2.MoveTowards(b,rect.center,border);
                    int start=helper.currentVertCount;helper.AddVert(a,color,Vector2.zero);helper.AddVert(b,color,Vector2.zero);helper.AddVert(bi,color,Vector2.zero);helper.AddVert(ai,color,Vector2.zero);
                    helper.AddTriangle(start,start+1,start+2);helper.AddTriangle(start,start+2,start+3);
                }
                return;
            }
            float size=Mathf.Min(rect.width,rect.height)*.44f;
            var center=rect.center;
            if(symbol==0) // compass rose / seed emblem
            {
                for(int i=0;i<8;i++)
                {
                    float angle=i*Mathf.PI/4;
                    var tip=center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*size;
                    var side=new Vector2(-Mathf.Sin(angle),Mathf.Cos(angle))*size*.17f;
                    Triangle(helper,center-side,tip,center+side,color);
                }
            }
            else
            {
                int sides=symbol==5?6:4;
                for(int i=0;i<sides;i++)
                {
                    float a=i*Mathf.PI*2/sides,b=(i+1)*Mathf.PI*2/sides;
                    var scale=symbol==4?new Vector2(1,.48f):symbol==1?new Vector2(.9f,.55f):Vector2.one;
                    Triangle(helper,center,center+Vector2.Scale(new Vector2(Mathf.Cos(a),Mathf.Sin(a))*size,scale),center+Vector2.Scale(new Vector2(Mathf.Cos(b),Mathf.Sin(b))*size,scale),i%2==0?color:color*.78f);
                }
            }
        }
        static void Triangle(UnityEngine.UI.VertexHelper h,Vector2 a,Vector2 b,Vector2 c,Color tint)
        {int n=h.currentVertCount;h.AddVert(a,tint,Vector2.zero);h.AddVert(b,tint,Vector2.zero);h.AddVert(c,tint,Vector2.zero);h.AddTriangle(n,n+1,n+2);}
        public static void Border(RectTransform parent)
        {
            var go=new GameObject("Gold ornamental frame",typeof(RectTransform),typeof(CanvasRenderer),typeof(HudOrnament));var r=go.GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;
            var g=go.GetComponent<HudOrnament>();g.frame=true;g.color=new Color(.72f,.56f,.32f,.85f);g.raycastTarget=false;
        }
    }
}
