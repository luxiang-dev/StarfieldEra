using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace StarfieldEra
{
    public sealed partial class SettlementGame:MonoBehaviour
    {
        public FarmGame Game {get;private set;}
        public VillageState Village=>Game.State.adventure.village;
        public VillageTradeState Trade=>Game.State.adventure.trade;
        public FrontierState Frontier=>Game.State.adventure.frontier;
        public static readonly Vector3 CaravanPosition=new Vector3(2,0,10);
        string sceneSignature="";
        bool SafeAction=>!Game.Adventure.InBattle&&!Game.Expedition.Fishing&&!Game.Night.Active;
        public void Initialize(FarmGame game)
        {Game=game;Village.Ensure(game.State.adventure);Trade.Ensure(game.State.day);BuildWorld();AdvanceWeather();Refresh();RefreshEnvironment();}
        public bool CanEnter(Vector3 p){if(!Game)return true;int i=FrontierState.Region(p);return i<0||Frontier.unlocked[i];}
        public bool Near(Vector3 p,float range=4)=>WorldRelief.PlanarDistance(Game.World.Player.position,p)<=range;
        public bool NearBoard=>Near(new Vector3(-2,0,7.5f));
        public void Refresh()
        {
            Village.Ensure(Game.State.adventure);Trade.Ensure(Game.State.day);
            string signature=Village.residents.Count+"/"+VillageTradeState.CaravanPresent(Game.State.day)+"/"+Trade.caravanCycle+"/"+string.Join(",",Frontier.unlocked)+"/"+Frontier.decorations.Count;
            if(signature==sceneSignature)return;sceneSignature=signature;RefreshSettlementWorld();
        }
        public void NewDay(){Refresh();RefreshEnvironment();}
        public string DaySummary=>"村民口粮余 "+Village.pantry+"，饥饿 "+Village.residents.Count(r=>r.hunger>0)+" 人。积水 "+Game.State.storm.puddles.Count+" 格。"+(VillageTradeState.CaravanPresent(Game.State.day)?"商队正在村口停留。":"下次商队：第"+VillageTradeState.NextCaravan(Game.State.day)+"天。");
        public void Assign(int resident,int workplace)
        {
            if(!SafeAction)return;var r=Village.residents.Find(v=>v.id==resident);var places=VillageState.Workplaces(Game.State.adventure);var site=workplace==0?null:places.Find(p=>p.site.workplaceId==workplace)?.site;
            if(workplace!=0&&site==null||!Village.Assign(Game.State.adventure,r,site)){Game.ShowMessage("岗位未修复、已经满员，或没有可用村民名额。");return;}Game.Adventure.Commit("已调整"+VillageState.Names[resident]+"的工作安排。");
        }
        public void Recruit(){if(!SafeAction)return;if(!Village.Recruit(Game.State)){Game.ShowMessage("招募需要空床位、口粮6和金币20；最多9位村民。");return;}Game.Adventure.Commit("新村民已经入住，打开村民生活面板安排工作。");}
        public void Donate(int food){if(!SafeAction)return;if(!Village.Donate(Game.State,food)){Game.ShowMessage("食物不足，无法捐入口粮仓。");return;}Game.Adventure.Commit("食物已存入口粮仓，村民每天每人消耗一份。");}
        public void OrderAction(VillageOrder order)
        {
            if(!SafeAction)return;if(!NearBoard){Game.ShowMessage("请走近城镇议事牌提交或接受订单。");return;}
            bool accepted=order!=null&&order.status==0;bool done=accepted?Trade.Accept(Game.State.day,order):Trade.Deliver(Game.State,order);
            if(!done){Game.ShowMessage("订单已经到期、已经完成，或背包材料不足。");return;}Game.Adventure.Commit(accepted?"订单已接受，请在本期结束前带齐物资交付。":"订单已交付，获得金币、村庄声望及公共建设备料。");
        }
        public void Buy(int offer)
        {
            if(!SafeAction)return;if(!Near(CaravanPosition)){Game.ShowMessage("请走近村口商队交易。");return;}if(!Trade.Buy(Game.State,offer)){Game.ShowMessage(Trade.BuyBlock(Game.State,offer));return;}Game.Adventure.Commit("商队交易完成："+VillageTradeState.Goods[offer]+"。");
        }
        public void Repair(int route)
        {
            if(!SafeAction||route<0||route>1)return;if(!Near(FrontierState.Entrance(route),6)){Game.ShowMessage("请走到路线入口六步以内施工。");return;}
            if(!EquipmentCatalog.Require(Game,"hammer"))return;
            if(!Frontier.Repair(Game.State,route)){Game.ShowMessage("施工物资或声望不足，需要："+Frontier.Cost(route));return;}Game.Adventure.Commit("道路已修复，"+FrontierState.Names[route]+"现已开放。步行进入后可解锁旅行。");
        }
        public void Travel(int route)
        {
            if(!SafeAction||route<0||route>1||!Frontier.unlocked[route]||!Frontier.visited[route])return;
            var controller=Game.World.Controller;controller.enabled=false;Game.World.Player.position=Game.World.HexGrid.SafeLand(FrontierState.Centers[route]+Vector3.back*3,controller.radius);controller.enabled=true;Game.Management.Close();Game.Inspection.Clear();Game.Adventure.Commit("抵达："+FrontierState.Names[route]);
        }
        public void PlaceBanner()
        {
            if(!SafeAction)return;var p=Game.World.Player.position+Game.World.Player.forward*2.5f;var grid=Game.World.HexGrid;
            if(Village.banners<1||Frontier.decorations.Count>=16||!grid.TryCell(p,out var c)||!grid.CanBuildOn(c)||!CanEnter(p)||Game.State.land.Find(c)?.channel==true||Game.State.land.Find(c)?.tilled==true||Game.State.land.Find(c)?.tree==true||Frontier.decorations.Any(d=>d.column==c.x&&d.row==c.y)||Game.State.adventure.buildings.Any(b=>BuildingFootprints.Cells(b).Contains(c))||Game.World.Adventure.Encounters.Any(e=>e.root&&WorldRelief.PlanarDistance(e.Position,p)<2)||!grid.CanStand(p,.3f))
            {Game.ShowMessage("在身前的空陆地放置旗帜，需要背包有旗帜；最多16面，避开建筑、农田与水面。");return;}
            Village.banners--;Frontier.decorations.Add(new VillageDecoration{column=c.x,row=c.y,rotation=Mathf.RoundToInt(Game.World.Player.eulerAngles.y/60)%6});Game.Adventure.Commit("村庄旗帜已放置，可以在附近收回。");
        }
        public void RecoverBanner()
        {
            if(!SafeAction||Village.banners>=99)return;var d=Frontier.decorations.OrderBy(x=>WorldRelief.PlanarDistance(HexWorldGrid.Center(x.column,x.row),Game.World.Player.position)).FirstOrDefault();
            if(d==null||!Near(HexWorldGrid.Center(d.column,d.row))){Game.ShowMessage("四步内没有可收回的旗帜。");return;}Frontier.decorations.Remove(d);Village.banners++;Game.Adventure.Commit("已收回村庄旗帜。");
        }
        public void Interact(WorldEncounter item)
        {
            if(!SafeAction)return;
            if(item.kind==EncounterKind.Resident){Game.Management.OpenVillager(item.id);return;}
            if(item.kind==EncounterKind.Caravan){Game.Management.Open("caravan");return;}
            if(item.kind==EncounterKind.Route){Game.Management.OpenFrontier(item.id);return;}
            if(item.kind==EncounterKind.Decoration){Game.Management.Open("caravan");return;}
            int route=item.id/3,kind=item.id%3;if(route<0||route>1||!Frontier.unlocked[route])return;
            if(kind==0&&!EquipmentCatalog.Require(Game,route==0?"axe":"pick"))return;
            if(!Frontier.Gather(Game.State,route,kind)){Game.ShowMessage("这里今天已经互动过，宝箱已经开启，或体力不足。");return;}
            Game.Adventure.Commit(kind==0?(route==0?"林谷采集：木材6、纤维4。":"矿脉开采：石料8、晶石2、星石1。"):kind==1?"观察到银耳兔，获得纤维2与生存经验3。":"废弃营地宝箱：金币25、星辉南瓜种子2、声望1。");
        }
        float nextVisit;
        void Update()
        {
            if(!Game)return;UpdateResidents();UpdateDrainage();if(Time.unscaledTime<nextVisit)return;nextVisit=Time.unscaledTime+1;
            int region=FrontierState.Region(Game.World.Player.position);if(region>=0&&Frontier.unlocked[region]&&!Frontier.visited[region]){Frontier.visited[region]=true;Game.Adventure.Commit("发现新区域："+FrontierState.Names[region]+"，已解锁快速旅行。");}
        }
    }
}
