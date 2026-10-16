using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace StarfieldEra
{
    public sealed class TreeChopping:MonoBehaviour
    {
        FarmGame game;readonly List<HarvestableTree> trees=new List<HarvestableTree>();
        readonly HashSet<Vector2Int> stumps=new HashSet<Vector2Int>();
        Material stumpMaterial;float nextSwing;
        public HarvestableTree Target {get;private set;}
        public bool HasNearby=>Target&&!Target.Felled;
        public string Hint=>HasNearby?"树木 · E / X 砍树 · "+Target.hits+" / 3 · 每斧体力1":"";
        public void Initialize(FarmGame owner)
        {
            game=owner;stumpMaterial=game.World.Mat("Fresh cut tree stump","9A7653");
            foreach(var tree in game.World.GetComponentsInChildren<HarvestableTree>(true))
            {
                if(game.World.HexGrid.TryCell(tree.transform.position,out var cell))
                {var planted=game.State.land.Find(cell);if(planted?.tree==true&&WorldRelief.PlanarDistance(tree.transform.position,HexWorldGrid.Center(cell.x,cell.y))<.15f){tree.planted=true;tree.cell=cell;}}
                Register(tree);
            }
            UpdateTarget();
        }
        public void Register(HarvestableTree tree)
        {
            if(!game)return;if(!trees.Contains(tree))trees.Add(tree);
            if(!tree.planted&&game.State.forestry.Contains(tree.transform.position))
            {tree.Felled=true;tree.gameObject.SetActive(false);Stump(tree.transform.position);}
        }
        void Stump(Vector3 point)
        {
            if(!stumps.Add(ForestryState.Key(point)))return;
            point=game.World.HexGrid.SurfacePoint(point,.12f);
            var stump=game.World.Shape("Saved felled tree stump",PrimitiveType.Cylinder,point,new Vector3(.45f,.12f,.45f),stumpMaterial);
            stump.SetParent(transform,true);
        }
        public void UpdateTarget()
        {
            Target=null;if(!game)return;float nearest=2.7f;trees.RemoveAll(t=>!t);
            foreach(var tree in trees)
            {
                if(tree.Felled||!tree.gameObject.activeInHierarchy)continue;
                if(tree.planted&&game.State.land.Find(tree.cell)?.treeGrowth<3)continue;
                var delta=tree.transform.position-game.World.Player.position;delta.y=0;
                if(game.World.Adventure.CameraRig.IsFirstPerson&&Vector3.Dot(delta.normalized,game.World.Player.forward)<.15f)continue;
                float d=delta.magnitude;if(d<nearest){nearest=d;Target=tree;}
            }
        }
        public void Chop()
        {
            if(!game||game.Management.IsOpen||game.Adventure.InBattle||game.Expedition.Fishing||game.Night.Active||game.Construction.Active||game.Fences.Active||game.Land.Active)return;
            if(!game.IsGrounded){game.ShowMessage("落地后再挥斧。");return;}
            UpdateTarget();if(!HasNearby){game.ShowMessage("走近树木再砍伐，第一人称需要面向树干。");return;}
            if(Time.time<nextSwing)return;
            if(!EquipmentCatalog.Require(game,"axe"))return;
            if(game.State.forestry.felled.Count>=4096&&!Target.planted){game.ShowMessage("已达到树木记录上限。");return;}
            if(!game.State.SpendEnergy(1)){game.ShowMessage("体力不足，请先休息或使用料理。");return;}
            nextSwing=Time.time+.45f;var tree=Target;tree.hits++;
            StartCoroutine(Sway(tree,tree.hits>=3));
            if(tree.hits<3){game.Adventure.Commit("挥斧砍树 "+tree.hits+" / 3，体力 -1。");return;}
            tree.Felled=true;
            if(!tree.planted&&!game.State.forestry.Fell(tree.transform.position))return;
            if(tree.planted)
            {
                var cell=game.State.land.Find(tree.cell);cell.tree=false;cell.treeGrowth=0;cell.treeWatered=false;cell.kind=(int)TerrainKind.Meadow;
            }
            foreach(var col in tree.GetComponentsInChildren<Collider>())col.enabled=false;
            int wood=5+game.State.adventure.toolLevel*2+WeatherCatalog.Get(game.State.Weather).extraWood;
            game.State.adventure.wood+=wood;game.State.weather.survivalExperience+=3;
            game.Adventure.Commit("树木已砍倒，木材 +"+wood+"，生存经验 +3。");UpdateTarget();
        }
        IEnumerator Sway(HarvestableTree tree,bool falling)
        {
            var root=tree.transform;var start=root.rotation;var direction=root.position-game.World.Player.position;direction.y=0;
            var axis=Vector3.Cross(Vector3.up,direction.sqrMagnitude>.01f?direction.normalized:Vector3.forward);
            float duration=falling?.5f:.22f,elapsed=0;
            while(root&&elapsed<duration)
            {
                if(game.Management.IsOpen){yield return null;continue;}
                elapsed+=Time.deltaTime;float t=Mathf.Clamp01(elapsed/duration);
                root.rotation=Quaternion.AngleAxis(falling?Mathf.SmoothStep(0,78,t):Mathf.Sin(t*Mathf.PI)*3,axis)*start;yield return null;
            }
            if(!root)yield break;root.rotation=start;
            if(!falling)yield break;
            root.gameObject.SetActive(false);Stump(root.position);
            if(tree.planted)game.Land.FinishLogging(tree.cell);
            else if(game.World.HexGrid.TryCell(root.position,out var cell)&&game.World.HexGrid.TerrainAt(cell)==TerrainKind.Forest)
            {
                bool occupied=false;foreach(var other in trees)if(other&&!other.Felled&&other.gameObject.activeInHierarchy&&WorldRelief.PlanarDistance(other.transform.position,HexWorldGrid.Center(cell.x,cell.y))<2){occupied=true;break;}
                if(!occupied){game.Land.FinishLogging(cell);game.Persist();}
            }
        }
    }
}
