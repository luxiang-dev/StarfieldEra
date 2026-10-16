using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace StarfieldEra
{
    /// <summary>Painted silhouettes with physical 3D terrain and unchanged gameplay colliders.</summary>
    public sealed class FantasyPresentation : MonoBehaviour
    {
        readonly List<Object> owned=new List<Object>();
        readonly List<Transform> billboards=new List<Transform>();
        readonly Dictionary<Material,Material> painted=new Dictionary<Material,Material>();
        FarmWorld world;Sprite[] sprites,buildingSprites;Material spriteMaterial;
        public void Build(FarmWorld source)
        {
            world=source;var texture=Resources.Load<Texture2D>("FantasyArt/StorybookProps");if(!texture)return;
            int w=texture.width/2,h=texture.height/2;
            sprites=new Sprite[4];
            var rects=new[]{new Rect(0,h,w,h),new Rect(w,h,w,h),new Rect(0,0,w,h),new Rect(w,0,w,h)};
            for(int i=0;i<4;i++){sprites[i]=Sprite.Create(texture,rects[i],new Vector2(.5f,.04f),100,0,SpriteMeshType.FullRect);owned.Add(sprites[i]);}
            spriteMaterial=new Material(Shader.Find("StarfieldEra/Storybook Sprite"));owned.Add(spriteMaterial);
            var anchors=world.GetComponentsInChildren<Transform>(true);int treeIndex=0;
            foreach(var anchor in anchors)
            {
                if(anchor.name.StartsWith("Store art - ")&&anchor.name.Contains("Pine_Tree"))
                {
                    float height=BoundsHeight(anchor,3);Hide(anchor);Add(anchor.position,treeIndex++%5==0?1:0,Mathf.Clamp(height,2.8f,4.8f),"Painted woodland tree");
                }
                else if(anchor.name.StartsWith("Store art - ")&&(anchor.name.Contains("Rock")||anchor.name.Contains("Stone_01")))
                {float height=BoundsHeight(anchor,.7f);Hide(anchor);var visual=Add(anchor.position,2,Mathf.Clamp(height*1.3f,.65f,1.5f),"Painted moss rocks");var rock=visual.GetComponent<HarvestableRock>();if(rock)rock.source=anchor;}
                else if(anchor.name=="Distant hills")Hide(anchor);
            }
            var houseNames=new HashSet<string>{"Farmhouse","Roof west","Roof east","Door","Window","Window frame","Chimney","Clay roof tile","Farmhouse stonework","Door planks","Door latch","Porch crate","Crate strap"};
            foreach(var renderer in world.GetComponentsInChildren<Renderer>())if(houseNames.Contains(renderer.name))renderer.enabled=false;
            Add(world.HexGrid.SurfacePoint(new Vector3(-5,0,5.3f)),3,4.8f,"Painted fantasy farmhouse");
            // Town buildings use the same illustrated facade and retain their own colliders,
            // inspectable roots and unlock visibility.
            var houses=new HashSet<string>{"Seed market","Village bakery","Workshop","Library","Southern mill","Observatory"};
            foreach(var anchor in anchors)if(houses.Contains(anchor.name))
            {
                if(anchor.name=="Workshop"&&LocalEnvironment.HasBuildingVisual(anchor))continue;
                Hide(anchor);
                if(anchor.name=="Workshop"&&LocalEnvironment.ProductionBuilding(0,anchor,anchor.position,3.1f,4.5f))continue;
                var visual=Add(anchor.position,3,anchor.name=="Library"?4.5f:3.8f,"Painted "+anchor.name);
                visual.SetParent(anchor,true);
                if(anchor.name=="Library"||anchor.name=="Observatory"){var sprite=visual.GetComponent<SpriteRenderer>();if(sprite)sprite.color=new Color(.88f,.94f,1);}
            }
            for(int i=0;i<10;i++)
            {
                var p=new Vector3(-9+i*.8f,0,2.9f+Mathf.Sin(i*.6f)*.28f);
                Add(world.HexGrid.SurfacePoint(p,.015f),2,.28f,"Painted path pebble");
            }
            for(int i=0;i<7;i++)
            {
                var p=new Vector3(3.2f,0,8.5f+i*.7f);Add(world.HexGrid.SurfacePoint(p),2,.65f+i%2*.2f,"Painted terrace outcrop");
            }
            // Smaller readable flora accents replace dense material noise.
            var art=StoreVisuals.Current;
            foreach(var anchor in anchors)
            {
                if(!anchor.name.StartsWith("Store art - "))continue;
                if(art&&((art.grass&&anchor.name=="Store art - "+art.grass.name)||(art.flower&&anchor.name=="Store art - "+art.flower.name)))
                {if(Mathf.Abs(anchor.position.x*37+anchor.position.z*11)%5>1.25f)Hide(anchor);}
            }
            Stylize(world.transform);
            foreach(var light in world.GetComponentsInChildren<Light>())if(light.type==LightType.Directional)
            {
                light.transform.rotation=Quaternion.Euler(48,-105,0);light.color=new Color(1,.97f,.90f);light.intensity=1.1f;light.shadows=LightShadows.Soft;light.shadowStrength=.8f;light.shadowBias=.02f;light.shadowNormalBias=.08f;
                var data=light.GetComponent<UniversalAdditionalLightData>()??light.gameObject.AddComponent<UniversalAdditionalLightData>();data.usePipelineSettings=false;data.softShadowQuality=SoftShadowQuality.High;
            }
            RenderSettings.ambientLight=new Color(.53f,.59f,.54f);
            world.gameObject.AddComponent<WatersideDetails>().Build(world);
            FaceCamera();
        }
        float BoundsHeight(Transform root,float fallback)
        {var renderers=root.GetComponentsInChildren<Renderer>();if(renderers.Length==0)return fallback;var b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);return b.size.y;}
        static void Hide(Transform root){foreach(var r in root.GetComponentsInChildren<Renderer>())r.enabled=false;}
        Transform Add(Vector3 p,int spriteIndex,float height,string name,bool forceSprite=false)
        {
            Transform local=null;
            if(!forceSprite&&(spriteIndex==0||spriteIndex==1))local=LocalEnvironment.Tree(transform,p,height,p.x*17+p.z*13);
            else if(!forceSprite&&spriteIndex==2)local=LocalEnvironment.Rock(transform,p,height,p.x*37+p.z*13);
            else if(!forceSprite&&spriteIndex==3)local=LocalEnvironment.House(transform,p,height,name.Contains("farmhouse")?5.2f:4.5f);
            if(local){local.name=name.Replace("Painted","Model");return local;}
            var go=new GameObject(name,typeof(SpriteRenderer));go.transform.SetParent(transform,false);go.transform.position=p;
            var renderer=go.GetComponent<SpriteRenderer>();renderer.sprite=sprites[spriteIndex];renderer.sharedMaterial=spriteMaterial;renderer.shadowCastingMode=ShadowCastingMode.TwoSided;renderer.receiveShadows=true;
            float scale=height/renderer.sprite.bounds.size.y;go.transform.localScale=Vector3.one*scale;billboards.Add(go.transform);return go.transform;
        }
        public Transform PlantTree(Vector3 position,float height)
        {return sprites==null?null:Add(position,1,height,"Restored woodland tree");}
        public void IllustrateBuilding(Transform root,int kind,Vector3 center,float height)
        {
            if(sprites==null||LocalEnvironment.HasBuildingVisual(root))return;
            foreach(var renderer in root.GetComponentsInChildren<Renderer>())if(renderer.name!="Terrace foundation")renderer.enabled=false;
            bool illustrated=kind==2||kind==5||kind>=7&&kind<=10;
            var visual=Add(center,kind==1?2:3,kind==1?4.7f:kind==0?height*1.3f:height,"Painted constructed building",illustrated);visual.SetParent(root,true);
            if(illustrated)
            {
                if(buildingSprites==null)
                {
                    var atlas=Resources.Load<Texture2D>("UI/BuildingIcons");
                    if(atlas){buildingSprites=new Sprite[12];int w=atlas.width/4,h=atlas.height/4;for(int i=0;i<12;i++){buildingSprites[i]=Sprite.Create(atlas,new Rect(i%4*w,(3-i/4)*h,w,h),new Vector2(.5f,.04f),100);owned.Add(buildingSprites[i]);}}
                }
                if(buildingSprites!=null){var renderer=visual.GetComponent<SpriteRenderer>();renderer.sprite=buildingSprites[kind];visual.localScale=Vector3.one*(height/renderer.sprite.bounds.size.y);}
            }
        }
        public void Stylize(Transform root)
        {
            var shader=Shader.Find("StarfieldEra/Storybook Models");if(!shader)return;
            foreach(var renderer in root.GetComponentsInChildren<Renderer>())
            {
                if(!renderer.enabled||renderer is SpriteRenderer||renderer.GetComponent<TMPro.TMP_Text>()||renderer is TMPro.TMP_SubMesh)continue;
                var materials=renderer.sharedMaterials;bool changed=false;
                for(int i=0;i<materials.Length;i++)
                {
                    var original=materials[i];if(!original||!original.shader.name.StartsWith("Universal Render Pipeline/"))continue;
                    if(!painted.TryGetValue(original,out var clone))
                    {
                        clone=new Material(original){shader=shader,name=original.name+" storybook"};
                        if(!original.HasProperty("_BaseMap"))clone.SetTexture("_BaseMap",Texture2D.whiteTexture);
                        if(!original.HasProperty("_BaseColor"))clone.SetColor("_BaseColor",Color.white);
                        if(!original.IsKeywordEnabled("_EMISSION"))clone.SetColor("_EmissionColor",Color.black);
                        painted[original]=clone;owned.Add(clone);
                    }
                    materials[i]=clone;changed=true;
                }
                if(changed)renderer.sharedMaterials=materials;
            }
        }
        void FaceCamera(){if(!world||!world.View)return;billboards.RemoveAll(t=>!t);foreach(var t in billboards)if(t)t.rotation=world.View.transform.rotation;}
        void LateUpdate(){FaceCamera();}
        void OnDestroy(){foreach(var o in owned)if(o)Destroy(o);}
    }
}
