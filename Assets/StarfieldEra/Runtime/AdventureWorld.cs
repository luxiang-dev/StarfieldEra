using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace StarfieldEra
{
    public enum EncounterKind { Wood, Stone, Berry, Fish, Elder, Town, Companion, Guardian }
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
        public readonly Vector3[] RegionCenters = {new Vector3(-5,0,6),new Vector3(13,0,0),new Vector3(-12,0,-8),new Vector3(10,0,13)};
        public Transform Follower { get; private set; }
        public int Region { get; private set; }
        FarmWorld world;
        Material timber, masonry, green, cream, blue, gold, purple;
        Transform workshop, library, windmill;
        readonly List<TextMeshPro> signs = new List<TextMeshPro>();
        readonly List<Transform> creatures = new List<Transform>();
        float zoom=10;
        int followerSpecies=-1;

        public void Build(FarmWorld farm)
        {
            world=farm;
            timber=world.Mat("Cedar timber","72554A"); masonry=world.Mat("Slate","7C8885");
            green=world.Mat("Moss","476F54"); cream=world.Mat("Limestone","E4D6AF");
            blue=world.Mat("Lake","548EAA"); gold=world.Mat("Harvest gold","E6BF69"); purple=world.Mat("Star crystal","9D80C1");
            Patch("Forest floor",new Vector3(13,.025f,1),new Vector3(12,.04f,19),green);
            Patch("Village plaza",new Vector3(-5,.035f,9),new Vector3(12,.05f,7),cream);
            Patch("Ruins plateau",new Vector3(10,.04f,13),new Vector3(12,.06f,9),masonry);
            Patch("East trail",new Vector3(9,.07f,3),new Vector3(11,.06f,1.3f),cream);
            Patch("North trail",new Vector3(6,.06f,9),new Vector3(1.3f,.05f,14),cream);
            Patch("Lake trail",new Vector3(-10,.04f,-1),new Vector3(1.2f,.04f,10),cream);
            Patch("Lake shore",new Vector3(-13,.03f,-10),new Vector3(10,.1f,8),cream);
            Patch("Mirror lake",new Vector3(-14,.1f,-11),new Vector3(8.5f,.1f,6.5f),blue);
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
                if(StoreVisuals.Current)StoreVisuals.Current.Replace(StoreVisuals.Current.ore,root,1.1f);
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
            Follower=new GameObject("Travelling companion").transform;Follower.SetParent(transform);
            Follower.gameObject.SetActive(false);
        }

        void Patch(string name,Vector3 pos,Vector3 size,Material material) => world.Shape(name,PrimitiveType.Cube,pos,size,material);
        void Tree(Vector3 pos,float height)
        {
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
            if(art) art.Replace(name=="Workshop"?art.workshop:name=="Library"?art.library:art.market,root,3.1f);
            return root;
        }
        Transform Marker(string label,Vector3 pos,EncounterKind kind,int id)
        {
            var root=new GameObject(label).transform;root.SetParent(transform);root.position=pos;
            Encounters.Add(new WorldEncounter{id=id,kind=kind,name=label,root=root});
            var sign=new GameObject("World label",typeof(TextMeshPro)).GetComponent<TextMeshPro>();
            sign.transform.SetParent(root);sign.transform.localPosition=Vector3.up*2.1f;
            sign.rectTransform.sizeDelta=new Vector2(8,1);
            sign.font=world.labelFont;sign.fontSize=2.8f;sign.alignment=TextAlignmentOptions.Center;
            sign.color=new Color(.98f,.93f,.75f);sign.text=label;sign.enableAutoSizing=false;
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
            workshop.gameObject.SetActive(state.workshop);windmill.gameObject.SetActive(state.workshop);
            library.gameObject.SetActive(state.library);
            for(int i=0;i<3;i++)creatures[i].gameObject.SetActive(!state.companions[i]);
            if(state.CompanionCount>0)
            {
                if(!state.companions[state.activeCompanion])for(int i=0;i<3;i++)if(state.companions[i]){state.activeCompanion=i;break;}
                if(followerSpecies!=state.activeCompanion)
                {
                    for(int i=Follower.childCount-1;i>=0;i--)Destroy(Follower.GetChild(i).gameObject);
                    Follower.position=world.Player.position+Vector3.right;
                    Creature(Follower,state.activeCompanion);followerSpecies=state.activeCompanion;
                }
                Follower.gameObject.SetActive(true);
            }
        }
        void LateUpdate()
        {
            if(world==null || world.View==null)return;
            Vector3 player=world.Player.position;
            Region=player.z>9&&player.x>3?3:player.x>8?1:player.x<-7&&player.z<-3?2:0;
            Vector3 target=player+new Vector3(12,17,-17);
            world.View.transform.position=Vector3.Lerp(world.View.transform.position,target,Time.deltaTime*4);
            world.View.transform.rotation=Quaternion.Euler(39, -35,0);
            world.View.transform.LookAt(player+Vector3.up*.2f);
            var mouse=UnityEngine.InputSystem.Mouse.current;
            if(mouse!=null)zoom=Mathf.Clamp(zoom-mouse.scroll.ReadValue().y*.008f,7,18);
            world.View.orthographicSize=Mathf.Lerp(world.View.orthographicSize,zoom,Time.deltaTime*5);
            foreach(var sign in signs)
            {
                if(sign==null)continue;
                sign.transform.rotation=world.View.transform.rotation;
                sign.gameObject.SetActive(Vector3.Distance(sign.transform.parent.position,player)<9);
            }
            if(Follower.gameObject.activeSelf)
            {
                Vector3 destination=player-world.Player.forward*1.2f+world.Player.right*.7f;
                destination.y=.08f+Mathf.Sin(Time.time*4)*.035f;
                Follower.position=Vector3.Lerp(Follower.position,destination,Time.deltaTime*3);
                Follower.rotation=Quaternion.Slerp(Follower.rotation,world.Player.rotation,Time.deltaTime*4);
            }
            if(windmill.gameObject.activeSelf)windmill.Rotate(0,0,Time.deltaTime*30);
        }
    }
}
