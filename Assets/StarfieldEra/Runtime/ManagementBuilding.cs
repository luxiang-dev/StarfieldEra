using System;
using System.Linq;
using UnityEngine;
namespace StarfieldEra
{
    public sealed partial class ManagementHud
    {
        PlacedBuilding managedBuilding;int managedKind=-1,managedSite=-1;string managedName="",managedDescription="";Vector3 managedPosition;bool buildingDemolition;
        public void OpenBuilding(BuildingInspectable info)
        {
            if(!info)return;
            managedBuilding=info.record;managedKind=info.record?.kind??info.kind;managedName=info.displayName;managedDescription=info.description;managedPosition=info.transform.position;managedSite=-1;buildingDemolition=false;Open("building");
        }
        public void OpenBuilding(PlacedBuilding record)
        {
            if(record==null||!A.buildings.Contains(record))return;
            managedBuilding=record;managedKind=record.kind;managedName=HexConstruction.Names[record.kind];managedDescription="";managedPosition=HexWorldGrid.Center(record.column,record.row);managedSite=-1;buildingDemolition=false;Open("building");
        }
        public void OpenResourceBuilding(int site)
        {
            var sites=game.Homestead.Sites();if(site<0||site>=sites.Count)return;
            if(site>=2){OpenBuilding(A.buildings.Where(b=>b.kind<2).ElementAt(site-2));return;}
            managedBuilding=null;managedKind=site;managedSite=site;managedName=game.Homestead.SiteName(site);managedPosition=game.Homestead.SitePosition(site);managedDescription="";buildingDemolition=false;Open("building");
        }
        ResourceSiteState ManagedOperation=>managedKind<2&&managedKind>=0?(managedBuilding?.operation??(managedSite>=0?game.Homestead.Sites()[managedSite]:null)):managedKind==6?(managedBuilding?.operation??A.homestead.workshop):null;
        void BuildingManagement()
        {
            if(managedBuilding!=null&&!A.buildings.Contains(managedBuilding)){Text(content,"建筑已拆除。",0,0,700,60,28,cream);Button(content,"返回城镇",0,90,300,48,()=>Open("town"));return;}
            var s=ManagedOperation;var h=A.homestead;bool workshop=managedKind==6;bool near=game.Homestead.Near(managedPosition,6);int capacity=h.StorageCapacity(A);
            var hero=Box(content,"Building identity",0,0,700,114,cream);UITheme.Frame(hero);if(managedKind>=0)UITheme.Icon(hero,managedKind,14,10,88,88,false,true);
            Text(hero,managedName,120,12,558,42,29,ink);Text(hero,(s!=null?"等级 "+s.level+" / 3 · ":"")+(managedBuilding!=null?"自建建筑 · 可移动和旋转":"固定建筑")+"\n位置 ["+managedPosition.x.ToString("F0")+","+managedPosition.z.ToString("F0")+"]",122,54,554,58,17,ink);
            string status=ProductionStatus(s,workshop,capacity);
            var stateCard=Box(content,"Building alerts",0,128,700,92,new Color(.10f,.24f,.19f));UITheme.Frame(stateCard);UITheme.Icon(stateCard,s!=null&&s.workers==0?15:17,14,12,52,52);
            Text(stateCard,status,80,12,598,65,20,cream);
            var output=Box(content,"Production and storage",0,234,700,182,cream);UITheme.Frame(output);
            Text(output,"生产进度 · "+(s==null?"每日自动结算":"等待今日休息结算"),20,8,654,42,22,ink);
            if(s!=null)
            {
                string raw=workshop?"木板":managedKind==0?"木材":"石料",goods=workshop?"石砖":managedKind==0?"木板":"石砖";
                StorageBar(output,raw,s.stock,capacity,54);StorageBar(output,goods,s.goods,capacity,94);
                string plan;
                if(workshop)plan="每日木材 +"+s.level*6+" 自动入背包；村民加工最多 "+s.workers*s.level*2+" 份"+(s.processing?"石砖":"木板")+"（每份消耗2原料）。";
                else{var forecast=BuildingOperations.Forecast(s,capacity,A.village.Productivity(A,s));plan="下次结算预计："+raw+" "+Signed(forecast.stock-s.stock)+" · "+goods+" "+Signed(forecast.goods-s.goods)+"。";}
                Text(output,plan,20,136,656,40,16,ink);
            }
            else Text(output,(managedDescription!=""?managedDescription:BuildingEconomy.Description(managedKind))+"\n自动产出直接进入背包；无需分配工人。",20,56,656,100,20,ink);
            var upgrade=Box(content,"Upgrade requirements",0,430,700,152,cream);UITheme.Frame(upgrade);
            Text(upgrade,s==null?"建筑用途":"升级条件",20,8,654,42,23,ink);
            Text(upgrade,s==null?BuildingEconomy.Description(managedKind):BuildingOperations.UpgradeCost(s)+"\n"+(BuildingOperations.UpgradeBlock(game.State,s)==""?"材料已备齐":BuildingOperations.UpgradeBlock(game.State,s))+"\n"+(near?"已在施工与领取范围内":"请靠近建筑六步内升级、修补或领取"),20,50,656,94,17,ink);
            var actions=Box(content,"Building management actions",722,0,438,582,new Color(.075f,.19f,.155f));UITheme.Frame(actions);
            Text(actions,"管理这座建筑",22,10,394,44,24,cream);
            Button(actions,s!=null?"村民 "+s.workers+" / 2 · 空闲 "+A.village.residents.Count(r=>r.workplace==0)+" 人 · 查看分工":"村民职业与生活",22,58,394,42,()=>Open("villagers"));
            Button(actions,"派驻 +1",22,104,190,42,()=>ManageAssign(1),s!=null&&s.level>0&&s.workers<2&&h.UsedWorkers(A)<h.WorkerCapacity(A)&&A.village.residents.Any(r=>r.workplace==0));
            Button(actions,"召回 -1",226,104,190,42,()=>ManageAssign(-1),s!=null&&s.workers>0);
            Button(actions,s==null?"无需设置加工":workshop?(s.level<2?"二级解锁石砖加工":s.processing?"切换为木板加工":"切换为石砖加工"):s.level<2?"二级解锁加工":s.processing?"关闭原料加工":"开启原料加工",22,160,394,42,ManageProcessing,s!=null&&s.level>=2);
            Button(actions,"领取全部库存",22,214,394,42,ManageCollect,s!=null&&near&&(s.stock>0||s.goods>0));
            Button(actions,s==null?"无需升级":s.level==0?"修复建筑":s.level>=3?"已达到三级上限":"升级至 "+(s.level+1)+" 级",22,268,394,42,ManageImprove,s!=null&&near&&BuildingOperations.UpgradeBlock(game.State,s)=="");
            Button(actions,managedKind<2&&s!=null&&s.damage>0?"修补 · 木2石1":workshop?"查看装备制作":"耐久完好",22,322,394,42,()=>{if(workshop)Open("craft");else game.Night.Repair(ManagedSiteIndex());},workshop||s!=null&&s.damage>0&&near&&A.wood>=2&&A.stone>=1);
            Button(actions,"移动建筑",22,390,190,42,()=>{var b=managedBuilding;Close();game.Construction.BeginMove(b);},managedBuilding!=null);
            Button(actions,"原地旋转60°",226,390,190,42,()=>{var b=managedBuilding;if(game.Construction.Relocate(b,new Vector2Int(b.column,b.row),(b.rotation+1)%6)){managedPosition=HexWorldGrid.Center(b.column,b.row);}Rebuild();},managedBuilding!=null);
            Button(actions,buildingDemolition?"确认拆除并取回库存":"拆除建筑 · 查看返还",22,444,394,42,()=>{if(!buildingDemolition){buildingDemolition=true;Rebuild();return;}var b=managedBuilding;if(game.Construction.Demolish(new Vector2Int(b.column,b.row))){Close();}else Rebuild();},managedBuilding!=null);
            Button(actions,buildingDemolition?"取消拆除":"返回世界",22,500,394,42,()=>{if(buildingDemolition){buildingDemolition=false;Rebuild();}else Close();});
            if(buildingDemolition&&managedBuilding!=null)Text(content,HexConstruction.RefundDescription(managedBuilding)+"（基础建造费用的一半）",0,586,1160,26,15,gold);
        }
        static string Signed(int value)=>value>=0?"+"+value:value.ToString();
        string ProductionStatus(ResourceSiteState s,bool workshop,int capacity)
        {
            if(s==null)return "自动运行 · 每次休息完成一天的产出";
            if(s.level==0)return "尚未修复 · 修复后安排村民开始生产";
            if(s.damage>=100)return "设施损毁 · 请先修补，再恢复生产";
            if(s.downtime>0)return "暂停生产 · 还需休整 "+s.downtime+" 天";
            if(s.workers==0)return workshop?"无人加工 · 自动木材仍会产出，派驻村民可加工成品":"无人工作 · 请派驻村民，否则没有产出";
            float productivity=A.village.Productivity(A,s);if(productivity<.999f)return "生活补给不足 · 效率 "+Mathf.RoundToInt(productivity*100)+"%，请查看村民住房与口粮";
            if(workshop)
            {
                int available=s.processing?A.stone:A.wood+s.level*6;
                if((s.processing?s.goods:s.stock)>=capacity)return "成品库存已满 · 领取库存后继续加工";
                if(available<s.workers*s.level*4)return "原料不足 · 补充"+(s.processing?"石料":"木材")+"，将按现有材料加工";
                return "生产已安排 · 休息后结算木材与加工成品";
            }
            if(s.stock>=capacity&&(!s.processing||s.goods>=capacity))return "库存已满 · 请领取库存，腾出生产空间";
            if(s.processing&&s.goods>=capacity)return "成品库存已满 · 原料仍可入库，请及时领取";
            return "生产已安排 · 村民 "+s.workers+" 人，休息后完成今日产出";
        }
        void StorageBar(Transform parent,string name,int value,int capacity,float y)
        {
            Text(parent,name+" "+value+" / "+capacity,20,y,200,34,18,ink);
            var bar=Box(parent,name+" storage",224,y+6,450,14,new Color(.73f,.73f,.59f));bar.GetComponent<UnityEngine.UI.Image>().raycastTarget=false;
            var fill=Box(bar,"Fill",0,0,450,14,new Color(.27f,.44f,.29f)).GetComponent<UnityEngine.UI.Image>();fill.type=UnityEngine.UI.Image.Type.Filled;fill.fillMethod=UnityEngine.UI.Image.FillMethod.Horizontal;fill.fillAmount=Mathf.Clamp01(value/(float)capacity);fill.raycastTarget=false;
        }
        int ManagedSiteIndex()=>managedBuilding!=null?game.Homestead.SiteIndex(managedBuilding):managedSite;
        void ManageAssign(int delta)
        {if(managedKind==6){if(BuildingOperations.AssignWorkshop(A,ManagedOperation,delta))game.Adventure.Commit(delta>0?"工坊村民已派驻。":"工坊村民已召回。");}else game.Homestead.Assign(ManagedSiteIndex(),delta);Rebuild();}
        void ManageCollect()
        {if(!game.Homestead.Near(managedPosition,6))return;if(managedKind==6){BuildingOperations.CollectWorkshop(A,ManagedOperation);game.Adventure.Commit("工坊成品已全部领取。");}else game.Homestead.Collect(ManagedSiteIndex());Rebuild();}
        void ManageImprove()
        {if(!game.Homestead.Near(managedPosition,6))return;if(managedKind==6){if(BuildingOperations.ImproveWorkshop(game.State,ManagedOperation))game.Adventure.Commit("工坊已升级，自动木材与加工效率提高。");}else game.Homestead.Improve(ManagedSiteIndex());Rebuild();}
        void ManageProcessing()
        {var s=ManagedOperation;if(s==null||s.level<2)return;if(managedKind==6){s.processing=!s.processing;game.Adventure.Commit(s.processing?"工坊改为加工石砖。":"工坊改为加工木板。");}else game.Homestead.ToggleProcessing(ManagedSiteIndex());Rebuild();}
    }
}
