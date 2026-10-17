using System.Collections.Generic;
using UnityEngine;
namespace StarfieldEra
{
    /// <summary>Deterministic outer wilderness: illustrated props, fresh gather sites and the supplied wolf den.</summary>
    public sealed class ExpandedWilderness:MonoBehaviour
    {
        public static readonly Vector3 DenPosition=new Vector3(58,0,39);
        readonly List<Transform> cards=new List<Transform>();
        readonly List<Transform> grounded=new List<Transform>();
        readonly List<Object> owned=new List<Object>();
        Sprite[] sprites;Material material;FarmGame game;
        public void Build(FarmGame owner)
        {
            game=owner;var texture=Resources.Load<Texture2D>("WildernessArt/WildProps");
            if(texture)
            {
                int w=texture.width/2,h=texture.height/2;sprites=new Sprite[4];
                for(int i=0;i<4;i++){sprites[i]=Sprite.Create(texture,new Rect((i%2)*w,i<2?h:0,w,h),new Vector2(.5f,.05f),100,0,SpriteMeshType.FullRect);owned.Add(sprites[i]);}
                material=new Material(Shader.Find("StarfieldEra/Storybook Sprite"));owned.Add(material);
            }
            var random=new System.Random(57491);
            for(int i=0;i<230;i++)
            {
                var p=new Vector3(-78+(float)random.NextDouble()*156,0,-66+(float)random.NextDouble()*132);
                if(Mathf.Abs(p.x)<43&&Mathf.Abs(p.z)<37||WorldRelief.PlanarDistance(p,DenPosition)<9||!game.World.HexGrid.CanStand(p,.6f))continue;
                if(i%3==0){var tree=game.World.GetComponent<FantasyPresentation>().PlantTree(game.World.HexGrid.SurfacePoint(p),3+i%4*.45f);if(tree)grounded.Add(tree);}
                else Prop(p,i%9==0?3:1,i%9==0?3: .65f+i%3*.2f,"Wilderness illustration");
            }
            var centers=new[]{new Vector3(57,0,16),new Vector3(-58,0,43),new Vector3(-57,0,-43),new Vector3(55,0,-43)};
            for(int region=0;region<centers.Length;region++)
            {
                var center=centers[region];var sign=Prop(center+Vector3.left*5,2,1.8f,"Outer trail sign");
                if(sign)WorldNameplate.Create(sign,"荒野营地",game.interfaceFont,game.World.View,2.2f);
                var camp=Anchor("荒野营地 · 料理与补给",center,EncounterKind.Camp,0);
                if(!LocalEnvironment.Camp(camp,camp.position))
                {
                    var tent=game.World.Cone("Outer camp tent",camp.position+Vector3.up*.05f,new Vector3(2,1.8f,2),game.World.Mat("Outer camp canvas","B9A274"));tent.SetParent(camp,true);
                }
                for(int i=0;i<4;i++)
                {
                    var p=center+new Vector3((i-1.5f)*4,0,6+i%2*3);
                    var bush=Anchor("荒野浆果丛",p,EncounterKind.Berry,32+region*4+i);
                    var card=Prop(p,0,1.3f,"Gatherable illustrated berry bush");if(card)card.SetParent(bush,true);
                }
            }
            var den=new GameObject("Wolf den landmark").transform;den.SetParent(transform,false);den.position=game.World.HexGrid.SurfacePoint(DenPosition);grounded.Add(den);
            var prefab=Resources.Load<GameObject>("Environment/WolfDen");
            if(prefab){var visual=Instantiate(prefab,den);visual.transform.localPosition=Vector3.zero;visual.transform.localRotation=Quaternion.Euler(0,180,0);}
            var obstacle=den.gameObject.AddComponent<BoxCollider>();obstacle.center=new Vector3(0,2.4f,1);obstacle.size=new Vector3(7,4.8f,4);
            var baseCollider=den.gameObject.AddComponent<BoxCollider>();baseCollider.center=Vector3.up*.45f;baseCollider.size=new Vector3(9,.9f,9);
            var entry=Anchor("狼窟 · 北岭",DenPosition+new Vector3(0,0,-5.5f),EncounterKind.Landmark,0);
            WorldNameplate.Create(entry,"北岭狼窟",game.interfaceFont,game.World.View,2.2f);
            for(int i=0;i<2;i++)
            {
                var p=game.World.HexGrid.SurfacePoint(DenPosition+new Vector3(i==0?-5.5f:5.5f,0,-5));
                var wolf=LocalEnvironment.Wolf(transform,p);if(wolf){wolf.rotation=Quaternion.Euler(0,i==0?205:155,0);grounded.Add(wolf);}
            }
        }
        Transform Anchor(string name,Vector3 p,EncounterKind kind,int id)
        {
            var root=new GameObject(name).transform;root.SetParent(transform,false);root.position=game.World.HexGrid.SurfacePoint(p);grounded.Add(root);
            game.World.Adventure.Encounters.Add(new WorldEncounter{name=name,root=root,kind=kind,id=id});return root;
        }
        Transform Prop(Vector3 p,int index,float height,string name)
        {
            if(sprites==null)return null;
            var root=new GameObject(name,typeof(SpriteRenderer)).transform;root.SetParent(transform,false);root.position=game.World.HexGrid.SurfacePoint(p);
            var renderer=root.GetComponent<SpriteRenderer>();renderer.sprite=sprites[index];renderer.sharedMaterial=material;root.localScale=Vector3.one*(height/renderer.sprite.bounds.size.y);cards.Add(root);grounded.Add(root);return root;
        }
        public void RefreshGround()
        {foreach(var t in grounded)if(t){var p=t.position;p.y=game.World.HexGrid.GroundHeight(p);t.position=p;}}
        void LateUpdate()
        {if(!game||!game.World.View)return;foreach(var t in cards)if(t)t.rotation=game.World.View.transform.rotation;}
        void OnDestroy(){foreach(var obj in owned)if(obj)Destroy(obj);}
    }
}
