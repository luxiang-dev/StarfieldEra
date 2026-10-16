using UnityEngine;
namespace StarfieldEra
{
    public sealed class BuildingGeneratedMeshes:MonoBehaviour
    {
        public readonly System.Collections.Generic.List<Mesh> owned=new System.Collections.Generic.List<Mesh>();
        void OnDestroy(){foreach(var mesh in owned)if(mesh)Destroy(mesh);}
    }
    public sealed partial class HexConstruction
    {
        static PlacedBuilding Candidate(Vector2Int cell,int kind,int facing,int footprintVersion)
            =>new PlacedBuilding{column=cell.x,row=cell.y,kind=kind,rotation=facing,footprintVersion=footprintVersion};
        public static string RefundDescription(PlacedBuilding b)
            =>"返还木材 "+WoodCost[b.kind]/2+" · 石料 "+StoneCost[b.kind]/2+" · 金币 "+GoldCost[b.kind]/2+"；库存全部取回";
        public void BeginMove(PlacedBuilding record)
        {
            if(record==null||!Game.State.adventure.buildings.Contains(record)||Game.Adventure.InBattle||Game.Night.Active)return;
            Choose(record.kind);if(!Active)return;
            moving=record;rotation=record.rotation;
            Game.ShowMessage("选择建筑的新位置，R旋转；右键取消后保留原位置。移动不消耗材料。");
        }
        public bool CanRelocate(PlacedBuilding record,Vector2Int cell,int facing,out string why)
        {
            why="";
            if(record==null||!Game.State.adventure.buildings.Contains(record)){why="只能移动自己建造的建筑";return false;}
            if(Game.Adventure.InBattle||Game.Night&&Game.Night.Active){why="守卫或战斗期间无法施工";return false;}
            if(!EquipmentCatalog.Has(Game,"hammer")){why="需要持有建造锤";return false;}
            if(facing<0||facing>5){why="无效朝向";return false;}
            foreach(var tile in BuildingFootprints.Cells(Candidate(cell,record.kind,facing,record.footprintVersion)))
                if(!SiteAllowed(tile,record,false,out why))return false;
            return true;
        }
        public bool Relocate(PlacedBuilding record,Vector2Int cell,int facing)
        {
            if(!CanRelocate(record,cell,facing,out var why)){Game.ShowMessage(why);return false;}
            var oldCell=new Vector2Int(record.column,record.row);
            if(!models.TryGetValue(oldCell,out var old)){Game.ShowMessage("建筑模型尚未就绪。");return false;}
            Release(record);models.Remove(oldCell);Game.World.Adventure.Encounters.RemoveAll(e=>e.root==old.transform);
            old.SetActive(false);Destroy(old);
            record.column=cell.x;record.row=cell.y;record.rotation=facing;Claim(record);Spawn(record);
            Game.Inspection?.RebindBuilding(record,models[cell].GetComponent<BuildingInspectable>());LastPlacementFrame=Time.frameCount;ClearInspection();Game.Adventure.Commit("建筑位置与朝向已保存，村民、等级和库存保持不变。");return true;
        }
    }
}
