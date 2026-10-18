using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
namespace StarfieldEra
{
    /// <summary>Draws the selected mesh without changing its materials, colliders or pick targets.</summary>
    [DefaultExecutionOrder(1000)]
    public sealed class WorldSelectionVisual:MonoBehaviour
    {
        sealed class Surface
        {
            public Renderer renderer;public Mesh mesh;public SkinnedMeshRenderer skin;public Mesh baked;
        }
        readonly List<Surface> surfaces=new List<Surface>();
        readonly Vector3[] corners=new Vector3[8];
        FarmGame game;WorldInspection inspection;Transform boundRoot;RectTransform overlay,frame,badge;TextMeshProUGUI caption;
        Material outline,groundMaterial;LineRenderer groundBorder;float rescanAt;
        static readonly Color Gold=new Color(1,.81f,.36f,1);
        public void Initialize(FarmGame owner,WorldInspection source,TMP_FontAsset font)
        {
            game=owner;inspection=source;
            var shader=Resources.Load<Shader>("SelectionOutline");if(shader)outline=new Material(shader){name="Selected target contour"};
            overlay=Rect("Selection feedback",transform);overlay.anchorMin=Vector2.zero;overlay.anchorMax=Vector2.one;overlay.offsetMin=overlay.offsetMax=Vector2.zero;overlay.SetAsFirstSibling();
            var group=overlay.gameObject.AddComponent<CanvasGroup>();group.blocksRaycasts=false;group.interactable=false;
            frame=Rect("Target corner brackets",overlay);
            foreach(var corner in new[]{Vector2.zero,Vector2.right,Vector2.up,Vector2.one})
            {
                // A dark stroke keeps the gold corners readable over bright terrain and rain.
                Corner(corner,5,new Color(.02f,.055f,.04f,.9f));Corner(corner,2.5f,Gold);
            }
            badge=Rect("Target name badge",overlay);badge.sizeDelta=new Vector2(280,42);
            var background=badge.gameObject.AddComponent<UnityEngine.UI.Image>();background.color=new Color(.025f,.085f,.065f,.96f);background.raycastTarget=false;UITheme.Round(background);
            var accent=Rect("Target badge accent",badge);accent.anchorMin=new Vector2(0,0);accent.anchorMax=new Vector2(0,1);accent.pivot=new Vector2(0,.5f);accent.sizeDelta=new Vector2(3,0);accent.anchoredPosition=Vector2.zero;
            var stripe=accent.gameObject.AddComponent<UnityEngine.UI.Image>();stripe.color=Gold;stripe.raycastTarget=false;
            caption=new GameObject("Selected name and distance",typeof(RectTransform),typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();caption.transform.SetParent(badge,false);
            caption.rectTransform.anchorMin=Vector2.zero;caption.rectTransform.anchorMax=Vector2.one;caption.rectTransform.offsetMin=new Vector2(12,5);caption.rectTransform.offsetMax=new Vector2(-12,-5);
            caption.font=font;caption.fontSize=18;caption.alignment=TextAlignmentOptions.Center;caption.color=Gold;caption.raycastTarget=false;caption.overflowMode=TextOverflowModes.Ellipsis;
            var pin=Rect("Target pointer",badge);pin.anchoredPosition=new Vector2(0,-24);pin.sizeDelta=new Vector2(8,8);pin.localRotation=Quaternion.Euler(0,0,45);
            var diamond=pin.gameObject.AddComponent<UnityEngine.UI.Image>();diamond.color=Gold;diamond.raycastTarget=false;
            groundMaterial=game.World.Mat("Selected land boundary","F4CF77");
            var border=new GameObject("Selected cell boundary",typeof(LineRenderer));border.transform.SetParent(transform,false);groundBorder=border.GetComponent<LineRenderer>();groundBorder.sharedMaterial=groundMaterial;groundBorder.loop=true;groundBorder.useWorldSpace=true;groundBorder.widthMultiplier=.045f;groundBorder.shadowCastingMode=ShadowCastingMode.Off;groundBorder.receiveShadows=false;
            Hide();
        }
        static RectTransform Rect(string name,Transform parent)
        {var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;return r;}
        void Corner(Vector2 corner,float thickness,Color color)
        {
            for(int axis=0;axis<2;axis++)
            {
                var r=Rect("Target corner stroke",frame);r.anchorMin=r.anchorMax=r.pivot=corner;r.anchoredPosition=Vector2.zero;
                r.sizeDelta=axis==0?new Vector2(24,thickness):new Vector2(thickness,24);
                var image=r.gameObject.AddComponent<UnityEngine.UI.Image>();image.color=color;image.raycastTarget=false;
            }
        }
        static bool VisibleSurface(Renderer r)=>r&&r.enabled&&r.gameObject.activeInHierarchy&&!(r is LineRenderer)&&!(r is ParticleSystemRenderer)&&!r.GetComponent<TMP_Text>()&&!r.GetComponent<TMP_SubMesh>()&&!r.GetComponentInParent<WorldNameplate>();
        void Bind(WorldChoice choice)
        {
            if(boundRoot!=choice.root)ReleaseMeshes();boundRoot=choice.root;rescanAt=Time.unscaledTime+.5f;
            if(choice.land||choice.plot>=0||!boundRoot){ReleaseMeshes();return;}
            var remaining=new List<Surface>(surfaces);surfaces.Clear();
            foreach(var r in boundRoot.GetComponentsInChildren<Renderer>())
            {
                if(!VisibleSurface(r))continue;
                var surface=remaining.Find(item=>item.renderer==r);
                if(surface!=null)remaining.Remove(surface);
                else
                {
                    surface=new Surface{renderer=r,skin=r as SkinnedMeshRenderer};
                    if(surface.skin)surface.baked=new Mesh{name="Temporary selected skinned mesh"};
                    else{var filter=r.GetComponent<MeshFilter>();if(filter)surface.mesh=filter.sharedMesh;}
                }
                surfaces.Add(surface);
            }
            foreach(var surface in remaining)if(surface.baked)Destroy(surface.baked);
        }

        void LateUpdate()
        {
            if(!game||!inspection){Hide();return;}
            var choice=inspection.Current;
            bool busy=game.Management.IsOpen||game.Construction.Menu.IsOpen||game.Construction.Active||game.Fences.Active||game.Land.Active||game.Night.TrapPlacing||game.Adventure.InBattle||game.Expedition.Fishing||game.Adventure.Hud.IsContextOpen;
            if(choice==null||!choice.Alive||busy){Hide();if(choice==null||!choice.Alive){ReleaseMeshes();boundRoot=null;}return;}
            if(boundRoot!=choice.root||Time.unscaledTime>=rescanAt)Bind(choice);
            var camera=game.World.View;if(!camera){Hide();return;}
            bool hasBounds=false;var bounds=new Bounds();
            foreach(var surface in surfaces)
            {
                if(!VisibleSurface(surface.renderer))continue;
                if(!hasBounds){bounds=surface.renderer.bounds;hasBounds=true;}else bounds.Encapsulate(surface.renderer.bounds);
                if(!outline)continue;
                var mesh=surface.mesh;
                if(surface.skin){// Bake into renderer-local units before applying the draw matrix (imported rigs use 100x scale).
                    surface.skin.BakeMesh(surface.baked,true);mesh=surface.baked;}
                if(!mesh)continue;
                for(int submesh=0;submesh<mesh.subMeshCount;submesh++)Graphics.DrawMesh(mesh,surface.renderer.localToWorldMatrix,outline,surface.renderer.gameObject.layer,camera,submesh,null,ShadowCastingMode.Off,false,null,LightProbeUsage.Off);
            }
            bool cell=choice.land||choice.plot>=0;groundBorder.gameObject.SetActive(cell);
            if(cell)
            {
                var center=choice.plot>=0?game.World.PlotPositions[choice.plot]:HexWorldGrid.Center(choice.cell.x,choice.cell.y);center=game.World.HexGrid.SurfacePoint(center,.07f);
                int count=6;groundBorder.positionCount=count;bounds=new Bounds(center,new Vector3(HexWorldGrid.Radius*2,.22f,HexWorldGrid.Radius*2));hasBounds=true;
                for(int i=0;i<count;i++)
                {
                    var offset=HexWorldGrid.Corner(i,HexWorldGrid.Radius-.035f);
                    groundBorder.SetPosition(i,game.World.HexGrid.SurfacePoint(center+offset,.07f));
                }
            }
            if(!hasBounds)bounds=new Bounds(choice.root.position+Vector3.up*.35f,Vector3.one*.7f);
            PositionFrame(camera,bounds,choice);
        }
        void PositionFrame(Camera camera,Bounds bounds,WorldChoice choice)
        {
            Vector2 min=new Vector2(float.MaxValue,float.MaxValue),max=new Vector2(float.MinValue,float.MinValue);bool any=false;
            for(int i=0;i<8;i++)
            {
                corners[i]=bounds.center+Vector3.Scale(bounds.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                var screen=camera.WorldToScreenPoint(corners[i]);if(screen.z<=camera.nearClipPlane)continue;
                min=Vector2.Min(min,screen);max=Vector2.Max(max,screen);any=true;
            }
            var viewport=camera.pixelRect;
            if(!any||max.x<viewport.xMin||min.x>viewport.xMax||max.y<viewport.yMin||min.y>viewport.yMax){overlay.gameObject.SetActive(false);return;}
            min=Vector2.Max(min-Vector2.one*9,viewport.min+Vector2.one*10);max=Vector2.Min(max+Vector2.one*9,viewport.max-Vector2.one*10);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(overlay,min,null,out var a);RectTransformUtility.ScreenPointToLocalPointInRectangle(overlay,max,null,out var b);
            frame.anchoredPosition=(a+b)*.5f;frame.sizeDelta=Vector2.Max(b-a,new Vector2(38,38));
            float width=Mathf.Clamp((choice.Name.Length+inspection.SelectedDistance.ToString("0.0").Length+5)*18,180,380);badge.sizeDelta=new Vector2(width,42);
            float half=overlay.rect.width*.5f;badge.anchoredPosition=new Vector2(Mathf.Clamp((a.x+b.x)*.5f,-half+width*.5f+12,half-width*.5f-12),Mathf.Min(b.y+40,overlay.rect.yMax-112));
            caption.text=choice.Name+" · "+inspection.SelectedDistance.ToString("0.0")+"米";overlay.gameObject.SetActive(true);
        }
        void Hide(){if(overlay)overlay.gameObject.SetActive(false);if(groundBorder)groundBorder.gameObject.SetActive(false);}
        void ReleaseMeshes(){foreach(var surface in surfaces)if(surface.baked)Destroy(surface.baked);surfaces.Clear();}
        void OnDisable(){Hide();}
        void OnDestroy(){ReleaseMeshes();if(outline)Destroy(outline);if(groundMaterial)Destroy(groundMaterial);if(overlay)Destroy(overlay.gameObject);if(groundBorder)Destroy(groundBorder.gameObject);}
    }
}
