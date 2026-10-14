using System.Collections.Generic;
using TMPro;
using UnityEngine;
namespace StarfieldEra
{
    public sealed class ExpeditionWorld : MonoBehaviour
    {
        ExpeditionGame game;Material wood,metal,stone,crystal,leaves,decoded;
        readonly List<WorldEncounter> landmarks=new List<WorldEncounter>();
        readonly Dictionary<int,Transform> lids=new Dictionary<int,Transform>();
        readonly Dictionary<int,Renderer> runeGems=new Dictionary<int,Renderer>();
        readonly Dictionary<int,Transform> fibers=new Dictionary<int,Transform>();
        readonly List<TextMeshPro> signs=new List<TextMeshPro>();
        public void Build(ExpeditionGame owner)
        {
            game=owner;var w=game.Game.World;
            foreach(var e in w.Adventure.Encounters)if(e.kind==EncounterKind.Fish&&e.id!=11)e.root.position=w.HexGrid.SafeLand(new Vector3(-29,0,29),.4f);
            wood=w.Mat("Expedition chest wood","875E3E");metal=w.Mat("Expedition brass","C5A75A");stone=w.Mat("Runestone slate","697A75");crystal=w.Mat("Runestone glow","9D80C1");decoded=w.Mat("Decoded rune","83CEC3");leaves=w.Mat("Forage fibers","A3B663");
            for(int i=0;i<8;i++)
            {
                var center=w.Adventure.RegionCenters[i];
                var chest=Point("旅人补给箱",EncounterKind.Treasure,i,center+new Vector3(4,0,3));
                Piece(chest.root,"Chest body",PrimitiveType.Cube,new Vector3(0,.32f,0),new Vector3(1,.6f,.75f),wood);
                var hinge=new GameObject("Chest lid hinge").transform;hinge.SetParent(chest.root,false);hinge.localPosition=new Vector3(0,.65f,.35f);lids[i]=hinge;
                Piece(hinge,"Chest lid",PrimitiveType.Cube,new Vector3(0,0,-.35f),new Vector3(1.08f,.12f,.82f),wood);
                for(int x=-1;x<=1;x+=2)Piece(chest.root,"Chest band",PrimitiveType.Cube,new Vector3(x*.33f,.33f,-.4f),new Vector3(.08f,.62f,.04f),metal);
                Piece(chest.root,"Chest latch",PrimitiveType.Cube,new Vector3(0,.48f,-.4f),new Vector3(.14f,.2f,.06f),metal);
                var obstacle=chest.root.gameObject.AddComponent<BoxCollider>();obstacle.center=Vector3.up*.3f;obstacle.size=new Vector3(1,.6f,.75f);
                var rune=Point(ExpeditionContent.RuneNames[i],EncounterKind.Rune,i,center+new Vector3(-4,0,3));
                Piece(rune.root,"Rune pedestal",PrimitiveType.Cylinder,Vector3.up*.12f,new Vector3(1.1f,.12f,1.1f),stone);
                Piece(rune.root,"Standing stone",PrimitiveType.Cube,Vector3.up*.8f,new Vector3(.65f,1.35f,.45f),stone);
                var gem=Piece(rune.root,"Star inscription",PrimitiveType.Sphere,new Vector3(0,1.12f,-.25f),Vector3.one*.26f,crystal);runeGems[i]=gem.GetComponent<Renderer>();
                for(int j=0;j<3;j++)Piece(rune.root,"Rune marking",PrimitiveType.Cube,new Vector3(0,.52f+j*.15f,-.24f),new Vector3(.32f,.025f,.025f),metal);
                var plant=Point(i==6?"湿地芦苇":i==5?"山地草束":"野生纤维",EncounterKind.Fiber,i,center+new Vector3(3,0,-5));
                var growth=new GameObject("Fiber growth").transform;growth.SetParent(plant.root,false);fibers[i]=growth;
                for(int j=0;j<5;j++)
                {Piece(growth,"Fiber stem",PrimitiveType.Cube,new Vector3((j-2)*.12f,.3f,.1f*(j%2)),new Vector3(.04f,.55f+.1f*(j%2),.04f),leaves);Piece(growth,"Fiber seedhead",PrimitiveType.Sphere,new Vector3((j-2)*.12f,.65f,.1f*(j%2)),new Vector3(.08f,.16f,.08f),metal);}
            }
            Refresh();
        }
        Vector3 Position(Vector3 desired)
        {
            var grid=game.Game.World.HexGrid;Vector3 best=grid.SafeLand(desired,.4f);float distance=float.MaxValue;
            for(int r=-20;r<=20;r++)for(int c=-23;c<=22;c++)
            {
                var cell=new Vector2Int(c,r);if(!grid.Contains(cell))continue;var p=HexWorldGrid.Center(c,r);float d=(p-desired).sqrMagnitude;if(d>=distance||!grid.CanStand(p,.45f))continue;
                bool blocked=false;foreach(var col in Physics.OverlapSphere(p+Vector3.up*.75f,.65f,~0,QueryTriggerInteraction.Ignore))if(col!=grid.GetComponent<Collider>()&&!(col is CharacterController)){blocked=true;break;}if(blocked)continue;
                foreach(var e in game.Game.World.Adventure.Encounters)if((p-e.Position).sqrMagnitude<8){blocked=true;break;}if(blocked)continue;
                foreach(var plot in game.Game.World.PlotPositions)if((p-plot).sqrMagnitude<4){blocked=true;break;}if(blocked||game.Game.Fences.Enclosure(cell)!=null)continue;
                distance=d;best=p;
            }
            return best;
        }
        WorldEncounter Point(string name,EncounterKind kind,int id,Vector3 desired)
        {
            var root=new GameObject(name).transform;root.SetParent(transform,false);root.position=Position(desired);
            var encounter=new WorldEncounter{name=name,kind=kind,id=id,root=root};landmarks.Add(encounter);game.Game.World.Adventure.Encounters.Add(encounter);
            var target=root.gameObject.AddComponent<ExpeditionTarget>();target.encounter=encounter;
            var hitbox=root.gameObject.AddComponent<BoxCollider>();hitbox.isTrigger=true;hitbox.center=Vector3.up*.75f;hitbox.size=new Vector3(1.1f,1.5f,1.1f);
            var sign=new GameObject("Landmark label",typeof(TextMeshPro)).GetComponent<TextMeshPro>();sign.transform.SetParent(root,false);sign.transform.localPosition=Vector3.up*1.8f;sign.rectTransform.sizeDelta=new Vector2(7,1);sign.font=game.Game.World.labelFont;sign.fontSize=2.5f;sign.alignment=TextAlignmentOptions.Center;sign.text=name;sign.color=new Color(.98f,.93f,.75f);sign.raycastTarget=false;signs.Add(sign);
            return encounter;
        }
        Transform Piece(Transform root,string name,PrimitiveType type,Vector3 p,Vector3 scale,Material material)
        {
            var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(root,false);go.transform.localPosition=p;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=material;go.GetComponent<Collider>().enabled=false;return go.transform;
        }
        public void Refresh()
        {
            for(int i=0;i<8;i++)
            {if(lids.TryGetValue(i,out var lid))lid.localRotation=Quaternion.Euler(game.State.caches[i]?65:0,0,0);if(runeGems.TryGetValue(i,out var gem))gem.sharedMaterial=game.State.runes[i]?decoded:crystal;if(fibers.TryGetValue(i,out var plant))plant.gameObject.SetActive(game.State.forageDay[i]!=game.Game.State.day);}
        }
        public string NextLandmark()
        {
            WorldEncounter next=null;float distance=float.MaxValue;
            foreach(var e in landmarks)
            {if(e.kind==EncounterKind.Fiber||(e.kind==EncounterKind.Treasure?game.State.caches[e.id]:game.State.runes[e.id]))continue;float d=Vector3.Distance(e.Position,game.Game.World.Player.position);if(d<distance){next=e;distance=d;}}
            return next==null?"全部补给箱与石碑已探索。":"附近未探索："+(next.kind==EncounterKind.Treasure?AdventureContent.Regions[next.id]+"补给箱":next.name)+"，距离 "+Mathf.CeilToInt(distance)+" 步。";
        }
        void LateUpdate()
        {
            foreach(var sign in signs){sign.transform.rotation=game.Game.World.View.transform.rotation;sign.gameObject.SetActive(Vector3.Distance(sign.transform.parent.position,game.Game.World.Player.position)<8);}
        }
    }
}
