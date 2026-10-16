using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace StarfieldEra
{
    public enum EncounterKind { Wood, Stone, Berry, Fish, Elder, Town, Companion, Guardian, Market, Camp, Crystal, Fiber, Treasure, Rune, Landmark }
    public sealed class WorldEncounter
    {
        public int id;
        public EncounterKind kind;
        public string name;
        public Transform root;
        public Vector3 Position => root.position;
    }

    public sealed class AdventureWorld : MonoBehaviour
    {
        public readonly List<WorldEncounter> Encounters = new List<WorldEncounter>();
        public readonly Vector3[] RegionCenters = {new Vector3(-5,0,6),new Vector3(13,0,0),new Vector3(-12,0,-8),new Vector3(10,0,13),new Vector3(24,0,-24),new Vector3(27,0,27),new Vector3(-29,0,22),new Vector3(-28,0,-20)};
        public Transform Follower { get; private set; }
        public int Region { get; private set; }
        FarmWorld world;FarmGame owner;
        Material timber, masonry, green, cream, blue, gold, purple;
        Transform workshop, library, windmill, mill, observatory;
        readonly List<WorldNameplate> signs = new List<WorldNameplate>();
        readonly List<Transform> creatures = new List<Transform>();
        public PlayerCameraRig CameraRig {get;private set;}
        int followerSpecies=-1;

        public void Build(FarmWorld farm)
        {
            world=farm;owner=farm.GetComponentInParent<FarmGame>();
            CameraRig=world.View.gameObject.AddComponent<PlayerCameraRig>();CameraRig.Initialize(owner);
            timber=world.Mat("Cedar timber","72554A"); masonry=world.Mat("Slate","87927E");
            green=world.Mat("Moss","557B48"); cream=world.Mat("Limestone","D1C493");
            blue=world.Mat("Lake","4597A8"); gold=world.Mat("Harvest gold","E6BF69"); purple=world.Mat("Star crystal","9D80C1");
            Patch("Forest floor",new Vector3(13,.025f,1),new Vector3(12,.04f,19),green,TerrainKind.Forest);
            Patch("Village plaza",new Vector3(-5,.035f,9),new Vector3(12,.05f,7),cream,TerrainKind.Road);
            Patch("Ruins plateau",new Vector3(10,.04f,13),new Vector3(12,.06f,9),masonry,TerrainKind.Rock);
            Patch("East trail",new Vector3(9,.07f,3),new Vector3(11,.06f,1.3f),cream,TerrainKind.Road);
            Patch("North trail",new Vector3(6,.06f,9),new Vector3(1.3f,.05f,14),cream,TerrainKind.Road);
            Patch("Lake trail",new Vector3(-10,.04f,-1),new Vector3(1.2f,.04f,10),cream,TerrainKind.Road);
            Patch("Lake shore",new Vector3(-13,.03f,-10),new Vector3(10,.1f,8),cream,TerrainKind.Sand);
            Patch("Mirror lake",new Vector3(-14,.1f,-11),new Vector3(8.5f,.1f,6.5f),blue,TerrainKind.Water);
            var blocker=world.Shape("Lake collision",PrimitiveType.Cube,new Vector3(-14,-.15f,-11),new Vector3(8.5f,.8f,6.5f),blue,true);
            blocker.GetComponent<Renderer>().enabled=false;
            for(int i=0;i<5;i++) Patch("Jetty plank",new Vector3(-11.5f,.24f,-8.3f-i*.5f),new Vector3(2,.12f,.42f),timber);
            for(int i=0;i<18;i++)
            {
                var pos=new Vector3(10+(i%5)*1.8f,.1f,-5+(i/5)*4);
                if(i%5==2) continue;
                Tree(pos,2.4f+(i%3)*.45f);
            }
            for(int i=0;i<8;i++)
                world.Cone("Distant hills",new Vector3(-16+i*4.7f,0,17.8f),new Vector3(5,3+(i%3),3),i%2==0?green:masonry);
            House("Seed market",new Vector3(-10,0,8.5f),2.6f,blue);
            House("Village bakery",new Vector3(-1,0,11.5f),2.4f,gold);
            workshop=House("Workshop",new Vector3(-10,0,13),2.6f,timber);
            library=House("Library",new Vector3(-5,0,13.6f),3f,purple);
            workshop.gameObject.SetActive(false);library.gameObject.SetActive(false);
            windmill=new GameObject("Windmill rotor").transform;windmill.SetParent(transform);
            windmill.position=new Vector3(-10,3.5f,11.5f);
            var blade=world.Shape("Mill blade",PrimitiveType.Cube,windmill.position,new Vector3(.16f,3.3f,.12f),cream);
            blade.SetParent(windmill,true);
            blade=world.Shape("Mill cross",PrimitiveType.Cube,windmill.position,new Vector3(3.3f,.16f,.12f),cream);blade.SetParent(windmill,true);
            for(int i=0;i<6;i++)
            {
                Vector3 pos=new Vector3(7.5f+(i%3)*2.6f,0,12+(i/3)*3.5f);
                var pillar=new GameObject("Ancient column").transform;pillar.SetParent(transform,false);pillar.position=pos;
                if(LocalEnvironment.Pillar(pillar,pos,2.45f+i%2*.8f,i*12))
                {
                    var collision=pillar.gameObject.AddComponent<BoxCollider>();collision.center=Vector3.up*1.1f;collision.size=new Vector3(.85f,2.2f,.85f);continue;
                }
                Destroy(pillar.gameObject);
                world.Shape("Broken column",PrimitiveType.Cylinder,pos+Vector3.up*(1.1f+i%2*.4f),new Vector3(.8f,1.1f+i%2*.4f,.8f),cream,true);
                world.Shape("Column capital",PrimitiveType.Cube,pos+Vector3.up*(2.2f+i%2*.8f),new Vector3(1.2f,.25f,1.2f),cream);
            }
            for(int i=0;i<4;i++)world.Cone("Crystal spire",new Vector3(11+i*.7f,.1f,16),new Vector3(.6f,1.4f+(i%2)*.8f,.6f),purple);
            world.Shape("Ancient portal",PrimitiveType.Cube,new Vector3(10,3,16),new Vector3(5,.6f,1),masonry);
            for(int i=0;i<6;i++)
            {
                Vector3 pos=new Vector3(12+(i%3)*2,0,-3+(i/3)*5);
                var root=Marker("倒木",pos,EncounterKind.Wood,i);
                var log=world.Shape("Fallen log",PrimitiveType.Cylinder,pos+Vector3.up*.3f,new Vector3(.65f,.85f,.65f),timber);
                log.rotation=Quaternion.Euler(0,0,90);log.SetParent(root,true);
                if(StoreVisuals.Current)StoreVisuals.Current.Replace(StoreVisuals.Current.fallenLog,root,.7f,false,2.4f);
            }
            for(int i=0;i<3;i++)
            {
                Vector3 pos=new Vector3(4+i*1.9f,0,11);
                var root=Marker("矿石",pos,EncounterKind.Stone,6+i);
                world.Shape("Ore",PrimitiveType.Sphere,pos+Vector3.up*.35f,new Vector3(1.1f,.85f,1),masonry).SetParent(root,true);
                world.Cone("Ore fleck",pos+Vector3.up*.6f,new Vector3(.4f,.6f,.4f),purple).SetParent(root,true);
                if(!LocalEnvironment.ReplaceRock(root,1.1f)&&StoreVisuals.Current)StoreVisuals.Current.Replace(StoreVisuals.Current.ore,root,1.1f);
            }
            for(int i=0;i<2;i++)
            {
                Vector3 pos=new Vector3(10+i*5,0,5);
                var root=Marker("浆果丛",pos,EncounterKind.Berry,9+i);
                world.Shape("Berry bush",PrimitiveType.Sphere,pos+Vector3.up*.55f,new Vector3(1.3f,1.1f,1.3f),green).SetParent(root,true);
                for(int j=0;j<4;j++) world.Shape("Berry",PrimitiveType.Sphere,pos+new Vector3((j%2)*.55f-.25f,.9f,(j/2)*.55f-.25f),Vector3.one*.22f,purple).SetParent(root,true);
            }
            var art=StoreVisuals.Current;
            if(art)
            {
                var random=new System.Random(218);
                for(int i=0;i<65;i++)
                {
                    var pos=new Vector3(9+(float)random.NextDouble()*10,0,-7+(float)random.NextDouble()*16);
                    art.Place(i%8==0?art.flower:art.grass,transform,pos,i%8==0?.55f:.35f);
                }
                for(int i=0;i<14;i++)art.Place(i%4==0?art.flower:art.shrub,transform,new Vector3(-17+i*2.2f,0,16.2f),i%4==0?.6f:.85f);
            }
            Marker("湖岸钓点",new Vector3(-11.5f,0,-8),EncounterKind.Fish,11);
            var elder=Marker("村长 · 阿岚",new Vector3(-5,0,8.8f),EncounterKind.Elder,0);
            Villager(elder,cream,purple);
            var board=Marker("城镇议事牌",new Vector3(-2,0,7.5f),EncounterKind.Town,0);
            world.Shape("Board post",PrimitiveType.Cube,board.position+Vector3.up*.7f,new Vector3(.18f,1.4f,.18f),timber).SetParent(board,true);
            world.Shape("Board",PrimitiveType.Cube,board.position+Vector3.up*1.3f,new Vector3(1.6f,.8f,.15f),cream).SetParent(board,true);
            for(int i=0;i<3;i++)
            {
                var pos=i==0?new Vector3(14,0,6):i==1?new Vector3(-8,0,-7):new Vector3(14,0,12);
                var root=Marker(AdventureContent.Names[i],pos,EncounterKind.Companion,i);
                Creature(root,i);creatures.Add(root);
            }
            var guardian=Marker("遗迹守卫 · 岩甲兽",new Vector3(9.8f,0,10),EncounterKind.Guardian,0);
            Creature(guardian,3);
            BuildFrontier();
            Follower=new GameObject("Travelling companion").transform;Follower.SetParent(transform);
            Follower.gameObject.SetActive(false);
        }

        void BuildFrontier()
        {
            var plains=world.Mat("Golden plains","ACAF67");
            var mountain=world.Mat("White rock mountains","A2ACA2");
            var marsh=world.Mat("Glowing marsh","496B67");
            Patch("Plains region",RegionCenters[4],new Vector3(28,0,24),plains,TerrainKind.Meadow);
            Patch("Mountain region",RegionCenters[5],new Vector3(28,0,24),mountain,TerrainKind.Rock);
            Patch("Marsh region",RegionCenters[6],new Vector3(23,0,27),marsh,TerrainKind.Marsh);
            Patch("Outpost region",RegionCenters[7],new Vector3(22,0,24),green,TerrainKind.Forest);
            Patch("Southern road",new Vector3(12,0,-15),new Vector3(2.5f,0,27),cream,TerrainKind.Road);
            Patch("Northern road",new Vector3(17,0,23),new Vector3(2.5f,0,26),cream,TerrainKind.Road);
            Patch("Outpost road",new Vector3(-21,0,-6),new Vector3(30,0,2.5f),cream,TerrainKind.Road);
            Patch("Marsh road",new Vector3(-20,0,16),new Vector3(32,0,2.5f),cream,TerrainKind.Road);
            var random=new System.Random(629);
            for(int region=4;region<8;region++)
            {
                var center=RegionCenters[region];
                for(int i=0;i<24;i++)
                {
                    var pos=center+new Vector3(-9+(float)random.NextDouble()*18,0,-8+(float)random.NextDouble()*16);
                    if(Vector3.Distance(pos,center)<3)continue;
                    if(region==5)world.Cone("Mountain crag",pos,new Vector3(2.5f,2+i%4,2.5f),i%2==0?mountain:masonry);
                    else Tree(pos,2.5f+i%4*.55f);
                }
                for(int i=0;i<5;i++)
                {
                    int id=12+(region-4)*5+i;
                    var kind=region==5?(i==4?EncounterKind.Crystal:EncounterKind.Stone):region==6?(i==4?EncounterKind.Fish:EncounterKind.Berry):i%2==0?EncounterKind.Wood:EncounterKind.Berry;
                    var pos=center+new Vector3(-5+i*2.5f,0,i%2==0?-4:4);
                    var root=Marker(kind==EncounterKind.Crystal?"晶石矿":kind==EncounterKind.Stone?"山地矿石":kind==EncounterKind.Wood?"林地倒木":kind==EncounterKind.Fish?"湿地钓点":"野生浆果",pos,kind,id);
                    var art=StoreVisuals.Current;
                    var prefab=art?(kind==EncounterKind.Stone?art.ore:kind==EncounterKind.Wood?art.fallenLog:kind==EncounterKind.Crystal||kind==EncounterKind.Fish?null:art.shrub):null;
                    if(kind==EncounterKind.Stone&&LocalEnvironment.ReplaceRock(root,1.1f)){}
                    else if(prefab)art.Place(prefab,root,pos,kind==EncounterKind.Wood?.5f:1,0,2);
                    else world.Cone("Frontier resource",pos,new Vector3(1,1.2f,1),kind==EncounterKind.Crystal?purple:green).SetParent(root,true);
                }
            }
            House("Outpost market",new Vector3(-28,0,-22),3.5f,gold);
            var trader=Marker("远行商人",new Vector3(-28,0,-18),EncounterKind.Market,0);Villager(trader,cream,gold);
            trader=Marker("平原商人",new Vector3(24,0,-24),EncounterKind.Market,0);Villager(trader,cream,blue);
            foreach(var pos in new[]{new Vector3(-25,0,-19),new Vector3(22,0,-22),new Vector3(-27,0,20)})
            {
                var camp=Marker("营地 · 料理与补给",pos,EncounterKind.Camp,0);
                if(!LocalEnvironment.Camp(camp,pos,2.6f,3.2f))
                {
                    world.Cone("Camp tent",pos+Vector3.forward,new Vector3(2,1.7f,2),gold).SetParent(camp,true);
                    world.Shape("Campfire",PrimitiveType.Cylinder,pos+Vector3.up*.1f,new Vector3(.7f,.1f,.7f),masonry).SetParent(camp,true);
                }
            }
            var summit=Marker("白岩首领 · 野蛮人",new Vector3(27,0,26),EncounterKind.Guardian,1);Creature(summit,4);
            var mire=Marker("幽光守卫",new Vector3(-29,0,23),EncounterKind.Guardian,2);Creature(mire,3);
            mill=House("Southern mill",new Vector3(-1,0,-12.5f),3,gold);
            observatory=House("Observatory",new Vector3(-14,0,13.5f),3.5f,purple);
            mill.gameObject.SetActive(false);observatory.gameObject.SetActive(false);
        }

        void Patch(string name,Vector3 pos,Vector3 size,Material material,TerrainKind? kind=null)
        {
            if(name=="Jetty plank") world.Shape(name,PrimitiveType.Cube,pos,size,material);
            else world.HexGrid.PaintBox(pos,size,material,kind);
        }
        void Tree(Vector3 pos,float height)
        {
            if(LocalEnvironment.Tree(transform,pos,height+1,pos.x*17+pos.z*13))return;
            if (StoreVisuals.Current && StoreVisuals.Current.tree) { StoreVisuals.Current.Place(StoreVisuals.Current.tree, transform, pos, height + 1); return; }
            world.Shape("Forest trunk",PrimitiveType.Cylinder,pos+Vector3.up*.6f,new Vector3(.3f,.6f,.3f),timber,true);
            world.Cone("Forest canopy",pos+Vector3.up*.8f,new Vector3(2.1f,height,2.1f),green);
            world.Cone("Canopy tier",pos+Vector3.up*1.8f,new Vector3(1.6f,height*.7f,1.6f),green);
        }
        Transform House(string name,Vector3 pos,float size,Material roof)
        {
            var root=new GameObject(name).transform;root.SetParent(transform);root.position=pos;
            world.Shape(name+" walls",PrimitiveType.Cube,pos+Vector3.up*1.1f,new Vector3(size,2.2f,size*.8f),cream,true).SetParent(root,true);
            for(int i=0;i<2;i++)
            {
                var panel=world.Shape("Roof",PrimitiveType.Cube,pos+new Vector3((i==0?-.25f:.25f)*size,2.5f,0),new Vector3(size*.7f,.22f,size),roof);
                panel.rotation=Quaternion.Euler(0,0,i==0?28:-28);panel.SetParent(root,true);
            }
            world.Shape("Village door",PrimitiveType.Cube,pos+new Vector3(0,.7f,-size*.41f),new Vector3(.65f,1.4f,.08f),timber).SetParent(root,true);
            world.Shape("Village window",PrimitiveType.Cube,pos+new Vector3(-size*.3f,1.3f,-size*.41f),new Vector3(.5f,.6f,.09f),blue).SetParent(root,true);
            var art=StoreVisuals.Current;
            if(name=="Workshop"&&Resources.Load<GameObject>("Environment/Workshop"))
            {
                foreach(var renderer in root.GetComponentsInChildren<Renderer>())renderer.enabled=false;
                LocalEnvironment.Workshop(root,pos);
            }
            else if(art) art.Replace(name=="Workshop"||name=="Southern mill"?art.workshop:name=="Library"||name=="Observatory"?art.library:art.market,root,3.1f);
            return root;
        }
        Transform Marker(string label,Vector3 pos,EncounterKind kind,int id)
        {
            var root=new GameObject(label).transform;root.SetParent(transform);root.position=pos;
            Encounters.Add(new WorldEncounter{id=id,kind=kind,name=label,root=root});
            var sign=WorldNameplate.Create(root,label,world.labelFont,world.View,2.1f);
            signs.Add(sign);return root;
        }
        void Villager(Transform root,Material skin,Material clothes)
        {
            if(StoreVisuals.Current && StoreVisuals.Current.Replace(StoreVisuals.Current.villager,root,1.85f,true))return;
            world.Shape("Villager coat",PrimitiveType.Capsule,root.position+Vector3.up*.7f,new Vector3(.65f,.6f,.65f),clothes).SetParent(root,true);
            world.Shape("Villager head",PrimitiveType.Sphere,root.position+Vector3.up*1.5f,Vector3.one*.45f,skin).SetParent(root,true);
            world.Shape("Village hat",PrimitiveType.Cylinder,root.position+Vector3.up*1.8f,new Vector3(.8f,.08f,.8f),gold).SetParent(root,true);
        }
        void Creature(Transform root,int species)
        {
            if(species==0&&LocalEnvironment.Rabbit(root,root.position))return;
            if(species==4){if(LocalCharacterMotion.AttachBoss(root))return;species=3;}
            var art=StoreVisuals.Current;
            var prefab=art?(species==3?art.guardian:(art.companions!=null && species<art.companions.Length?art.companions[species]:null)):null;
            if(art && art.Replace(prefab,root,species==3?2.2f:1.1f))return;
            Material fur=species==0?green:species==1?blue:species==2?gold:masonry;
            Vector3 pos=root.position;
            world.Shape("Creature body",PrimitiveType.Sphere,pos+Vector3.up*.45f,new Vector3(.95f,.65f,1.3f),fur).SetParent(root,true);
            world.Shape("Creature head",PrimitiveType.Sphere,pos+new Vector3(0,.85f,-.4f),Vector3.one*.65f,fur).SetParent(root,true);
            for(int i=0;i<2;i++)
            {
                world.Cone("Ears",pos+new Vector3(i==0?-.24f:.24f,1.1f,-.4f),new Vector3(.25f,.55f,.25f),species==3?purple:fur).SetParent(root,true);
                world.Shape("Eyes",PrimitiveType.Sphere,pos+new Vector3(i==0?-.19f:.19f,.95f,-.7f),Vector3.one*.1f,timber).SetParent(root,true);
            }
            for(int i=0;i<4;i++)world.Shape("Feet",PrimitiveType.Capsule,pos+new Vector3(i%2==0?-.25f:.25f,.18f,i<2?-.35f:.35f),new Vector3(.2f,.2f,.2f),fur).SetParent(root,true);
            var tail=world.Cone("Tail",pos+new Vector3(0,.5f,.55f),new Vector3(.45f,species==2?1.2f:.6f,.45f),species==3?purple:cream);
            tail.rotation=Quaternion.Euler(45,0,0);tail.SetParent(root,true);
        }

        public void Refresh(AdventureState state)
        {
            workshop.gameObject.SetActive(state.workshop);windmill.gameObject.SetActive(state.workshop&&!Resources.Load<GameObject>("Environment/Lumberyard"));
            library.gameObject.SetActive(state.library);
            mill.gameObject.SetActive(state.mill);observatory.gameObject.SetActive(state.observatory);
            for(int i=0;i<3;i++)creatures[i].gameObject.SetActive(!state.companions[i]);
            if(state.CompanionCount>0)
            {
                if(!state.companions[state.activeCompanion])for(int i=0;i<3;i++)if(state.companions[i]){state.activeCompanion=i;break;}
                if(followerSpecies!=state.activeCompanion)
                {
                    for(int i=Follower.childCount-1;i>=0;i--)Destroy(Follower.GetChild(i).gameObject);
                    Follower.position=world.HexGrid.SafeLand(world.Player.position+Vector3.right,.15f,false);
                    Creature(Follower,state.activeCompanion);followerSpecies=state.activeCompanion;
                }
                Follower.gameObject.SetActive(true);
            }
        }
        public void AdjustZoom(float amount) { CameraRig.AdjustZoom(amount); }
        void LateUpdate()
        {
            if(world==null || world.View==null)return;
            Vector3 player=world.Player.position;
            Region=player.z<-16&&player.x>10?4:player.z>20&&player.x>0?5:player.x<-22&&player.z>8?6:player.x<-22&&player.z<-6?7:player.z>9&&player.x>3?3:player.x>8?1:player.x<-7&&player.z<-3?2:0;
            foreach(var sign in signs)
            {
                if(sign==null)continue;
                sign.transform.rotation=world.View.transform.rotation;
                sign.gameObject.SetActive(Vector3.Distance(sign.transform.parent.position,player)<9);
            }
            if(Follower.gameObject.activeSelf)
            {
                Vector3 destination=player-world.Player.forward*1.2f+world.Player.right*.7f;
                destination.y=world.HexGrid.GroundHeight(destination)+(followerSpecies==0?.01f:.08f+Mathf.Sin(Time.time*4)*.035f);
                if(!world.HexGrid.CanStand(Follower.position,.15f))Follower.position=world.HexGrid.SafeLand(player,.15f,false);
                var step=Vector3.Lerp(Follower.position,destination,Time.deltaTime*3)-Follower.position;
                var horizontal=world.HexGrid.ConstrainMovement(Follower.position,step,.15f);
                Follower.position+=horizontal;var grounded=Follower.position;grounded.y=world.HexGrid.GroundHeight(grounded)+(followerSpecies==0?.01f:.08f+Mathf.Sin(Time.time*4)*.035f);Follower.position=grounded;
                Follower.rotation=Quaternion.Slerp(Follower.rotation,world.Player.rotation,Time.deltaTime*4);
            }
            if(windmill.gameObject.activeSelf)windmill.Rotate(0,0,Time.deltaTime*30);
        }
    }
}
