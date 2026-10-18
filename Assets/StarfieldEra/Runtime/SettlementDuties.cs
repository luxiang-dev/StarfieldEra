using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace StarfieldEra
{
    public sealed partial class SettlementGame
    {
        sealed class Duty
        {
            public Vector3 target;public VillageWorkplace site;public GardenUnit wall;public FenceRecord fence;public GardenActor enemy;public float actionAt;public int patrol;
            public string status="等待工作";public GameObject crate;
        }
        readonly Dictionary<int,Duty> duties=new Dictionary<int,Duty>();float dutyTick;
        public string DutyStatus(int id)=>duties.TryGetValue(id,out var d)?d.status:"等待安排";
        public void AssignDuty(int id,int role)
        {
            if(!SafeAction||role<0||role>3)return;var r=Village.residents.Find(p=>p.id==id);if(r==null)return;
            if(!Village.Assign(Game.State.adventure,r,null))return;r.role=role;if(duties.TryGetValue(id,out var previous)&&previous.crate)Destroy(previous.crate);duties.Remove(id);destinations.Remove(id);destinationInputs.Remove(id);Game.Adventure.Commit("已安排"+VillageState.Names[id]+"："+VillageState.Roles[role]+"。");
        }
        Vector3 StoreFront()
        {
            var warehouse=Game.State.adventure.buildings.Where(b=>b.kind==5).OrderBy(b=>WorldRelief.PlanarDistance(HexWorldGrid.Center(b.column,b.row),Game.World.Door)).FirstOrDefault();
            return warehouse==null?Game.World.Door+Vector3.back*3:HexWorldGrid.Center(warehouse.column,warehouse.row)+Vector3.back*3;
        }
        bool DutyDestination(ResidentState r,out Vector3 target)
        {
            target=default;if(r.role==0&&!r.HasCargo)return false;
            if(!duties.TryGetValue(r.id,out var d))duties[r.id]=d=new Duty{target=StoreFront()};target=d.target;return true;
        }
        bool DutyArrived(int id,Vector3 p,Vector3 desired)
        {
            if(destinationInputs.TryGetValue(id,out var input)&&(input-desired).sqrMagnitude<.01f&&destinations.TryGetValue(id,out var accessible))desired=accessible;
            return WorldRelief.PlanarDistance(p,desired)<2.3f;
        }
        void UpdateDuties()
        {
            if(Game.SimulationPaused||Time.time<dutyTick)return;dutyTick=Time.time+1;
            var places=VillageState.Workplaces(Game.State.adventure);
            foreach(var r in Village.residents)
            {
                if(!people.TryGetValue(r.id,out var root)||!root)continue;
                if(!duties.TryGetValue(r.id,out var d))duties[r.id]=d=new Duty{target=StoreFront()};
                if(r.role==0&&!r.HasCargo){d.status="自由活动";if(d.crate)d.crate.SetActive(false);continue;}
                if(r.hunger>=2){d.status="饥饿，暂停工作；请补充口粮";continue;}
                bool arrived=DutyArrived(r.id,root.position,d.target);
                if(r.HasCargo)
                {
                    d.target=StoreFront();d.status="运送物资到仓库";
                    if(!d.crate){var box=Game.World.Shape("Carried supply crate",PrimitiveType.Cube,root.position+Vector3.up,new Vector3(.45f,.4f,.35f),Game.World.Mat("Porter crate","A38258"));box.SetParent(root,true);box.localPosition=new Vector3(0,.9f,.38f);foreach(var c in box.GetComponents<Collider>())c.enabled=false;d.crate=box.gameObject;}
                    d.crate.SetActive(true);
                    if(DutyArrived(r.id,root.position,d.target)){var a=Game.State.adventure;a.wood+=r.cargoWood;a.stone+=r.cargoStone;a.homestead.planks+=r.cargoPlanks;a.homestead.bricks+=r.cargoBricks;r.cargoWood=r.cargoStone=r.cargoPlanks=r.cargoBricks=0;d.site=null;d.crate.SetActive(false);d.status="物资已入库";Game.Persist();}continue;
                }
                if(d.crate)d.crate.SetActive(false);
                if(r.role==1)
                {
                    if(d.site==null||d.site.site.stock+d.site.site.goods==0)d.site=places.Where(p=>p.site.stock+p.site.goods>0).OrderByDescending(p=>p.site.stock+p.site.goods).FirstOrDefault();
                    if(d.site==null){d.target=StoreFront();d.status="库存为空，等待产出";continue;}
                    d.target=d.site.position+new Vector3(2.8f,0,-2.5f);d.status="前往收取："+d.site.name;
                    if(DutyArrived(r.id,root.position,d.target))
                    {
                        var s=d.site.site;int raw=Mathf.Min(100,s.stock),goods=Mathf.Min(40,s.goods);s.stock-=raw;s.goods-=goods;
                        if(d.site.kind==0){r.cargoWood=raw;r.cargoPlanks=goods;}else if(d.site.kind==1){r.cargoStone=raw;r.cargoBricks=goods;}else{r.cargoPlanks=raw;r.cargoBricks=goods;}
                        d.target=StoreFront();d.status="已装载，返回仓库";Game.Persist();
                    }
                }
                else if(r.role==2)
                {
                    d.wall=Game.State.garden.units.Where(u=>u.kind==1&&u.health<u.MaxHealth).OrderBy(u=>WorldRelief.PlanarDistance(root.position,u.Position)).FirstOrDefault();
                    d.fence=Game.State.adventure.fences.FirstOrDefault(f=>f.damage>0);
                    var site=places.Where(p=>p.site.damage>0&&p.site.level>0).OrderBy(p=>WorldRelief.PlanarDistance(root.position,p.position)).FirstOrDefault();
                    if(d.wall==null&&d.fence==null&&site==null){d.target=StoreFront();d.status="防线完好，等待维修";continue;}
                    if(d.wall!=null)d.target=d.wall.Position+Vector3.back*2;else if(d.fence!=null){PastureFences.Endpoints(d.fence,out var a,out var b);d.target=(a+b)*.5f+Vector3.back*1.5f;}else d.target=site.position+new Vector3(2.8f,0,-2.5f);
                    if(Game.State.adventure.wood<2||Game.State.adventure.stone<1){d.status="维修缺木材2、石料1";continue;}d.status="前往维修防线或设施";
                    if(DutyArrived(r.id,root.position,d.target)&&Time.time>=d.actionAt)
                    {
                        d.actionAt=Time.time+4;Game.State.adventure.wood-=2;Game.State.adventure.stone--;
                        if(d.wall!=null)d.wall.health=Mathf.Min(d.wall.MaxHealth,d.wall.health+100);else if(d.fence!=null)d.fence.damage=0;else site.site.damage=Mathf.Max(0,site.site.damage-35);
                        d.status="正在维修";Game.Persist();
                    }
                }
                else if(r.role==3)
                {
                    if(d.enemy==null||d.enemy.Health<=0||WorldRelief.PlanarDistance(root.position,d.enemy.transform.position)>14)d.enemy=Game.Garden.NearestEnemy(root.position,10);
                    if(d.enemy)
                    {
                        d.target=d.enemy.transform.position;d.status="发现僵尸，守卫接敌";
                        if(WorldRelief.PlanarDistance(root.position,d.target)<3&&Time.time>=d.actionAt&&Game.Garden.CanGuardAttack(root.position,d.enemy))
                        {d.actionAt=Time.time+1.8f;Game.Garden.Damage(d.enemy,20,root.position);}
                    }
                    else
                    {
                        var wolf=Game.Night.GuardTarget(root.position);if(wolf){d.target=wolf.position;d.status="守卫驱赶野狼";if(Time.time>=d.actionAt&&Game.Night.GuardAttack(root.position))d.actionAt=Time.time+2;continue;}
                        var production=places.Where(p=>p.site.level>0).ToList();int count=production.Count+1;d.patrol%=count;var point=d.patrol==0?Game.World.Door:production[d.patrol-1].position;
                        d.target=point+new Vector3(3,0,-3);d.status="巡逻营地与生产设施";if(arrived&&Time.time>=d.actionAt){d.actionAt=Time.time+4;d.patrol=(d.patrol+1)%count;}
                    }
                }
            }
        }
    }
}
