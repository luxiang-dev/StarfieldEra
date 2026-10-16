using System.Linq;
using UnityEngine;
namespace StarfieldEra
{
    public sealed partial class ManagementHud
    {
        bool rabbitView=true,campView;int campChoice;int selectedSite,sitePage;
        public void OpenHomestead(bool rabbits,int site=0)
        {campView=false;rabbitView=rabbits;selectedSite=site;sitePage=site/5;Open("homestead");}
        void HomesteadPage()
        {
            Chip("兔子与委托",0,()=>{campView=false;rabbitView=true;Rebuild();},rabbitView&&!campView);
            Chip("资源点经营",158,()=>{campView=false;rabbitView=false;Rebuild();},!rabbitView&&!campView);
            Chip("营地与守卫",316,OpenCamp,campView);
            Chip("返回城镇",474,()=>Open("town"),false);
            if(campView)CampPage();else if(rabbitView)RabbitPage();else ResourcePage();
        }
        void RabbitPage()
        {
            var h=game.Homestead;var state=A.homestead;
            var rabbits=h.Rabbits.Where(r=>r&&r.Record?.status!=3).ToList();
            if(!h.Inspected&&rabbits.Count>0)h.InspectForHandbook(rabbits[0]);
            for(int i=0;i<rabbits.Count;i++)
            {
                var rabbit=rabbits[i];var record=rabbit.Record;
                string status=record==null?"野外 · "+(rabbit.lureSeconds>0?"正在吃诱饵":"靠近会逃跑"):record.status==1?"携带中 · 回小屋安置":"兔舍 · "+(record.fedDay==game.State.day?"今天已喂":"今天未喂")+" · 已照料"+record.care+"天";
                Row("兔子 "+(rabbit.id+1),status,i,()=>{h.InspectForHandbook(rabbit);Rebuild();},h.Inspected==rabbit,62);
            }
            Text(content,"胡萝卜 "+state.carrots+" · 兔舍 "+state.rabbits.Count(r=>r.status==2)+" / 3",0,382,620,34,20,cream);
            Button(content,"购买胡萝卜 · 6金币 / 3份",0,430,300,44,h.BuyCarrots);
            Button(content,"安置携带的兔子 · 小屋门前",316,430,304,44,h.House,state.rabbits.Any(r=>r.status==1));
            Button(content,"喂选中兔子 · 胡萝卜1",0,490,300,44,h.Feed,h.Inspected&&h.Inspected.Record?.status==2);
            Button(content,state.rabbitQuest==0?"向村长接寻兔委托":state.rabbitQuest==1?"向村长交付寻兔委托":"寻兔委托已完成",316,490,304,44,h.Quest,state.rabbitQuest!=2);
            Text(content,"提示：回世界等待兔子靠近，E或点击兔子打开操作。",0,548,620,32,16,gold);
            var selected=h.Inspected;var r=selected?selected.Record:null;
            string where=selected?"位置 ["+selected.transform.position.x.ToString("F0")+","+selected.transform.position.z.ToString("F0")+"]":"";
            string quest=state.rabbitQuest==0?"村长正在寻找走失的兔子，可去接取委托。":state.rabbitQuest==1?"交还携带的兔子，或已照料一天的兔子。奖励30金币、胡萝卜2。":"兔子已交还村民，委托完成。";
            var detail=Details(selected?"兔子 "+(selected.id+1):"兔舍","捕捉 · 携带 · 照料",where+"\n野兔六步内可投放胡萝卜，诱饵持续25秒。\n返回世界，等兔子靠近两步内再捕捉。\n一次携带1只，兔舍最多3只。\n回小屋门前安置，靠近兔舍喂养。\n每日喂1份胡萝卜，休息后收益4金币。\n\n寻兔委托："+quest);
            if(r==null)
            {
                DetailAction(detail,"投放胡萝卜并返回世界",0,h.Lure,selected);
                DetailAction(detail,"捕捉已被引诱的兔子",1,h.Capture,selected&&selected.lureSeconds>0&&h.Near(selected.transform.position,1.8f));
            }
            else
            {
                DetailAction(detail,r.status==1?"在小屋安置这只兔子":"喂养这只兔子",0,()=>{if(r.status==1)h.House();else h.Feed();});
                DetailAction(detail,"放回这只兔子",1,h.Release,r.status!=3);
            }
        }
        public void OpenCamp(){campView=true;Open("homestead");}
        void CampPage()
        {
            string[] names={"居民房屋","仓库","木围栏","牧场门","捕兽陷阱"};
            string[] costs={"木材10 · 石料6 · 金币15 · 占4格","木材8 · 石料8 · 金币12 · 占4格","每段木材2 · 连续摆放","每扇木材4 · 可开关","每处木材2 · 石料1 · 触发一次"};
            string[] descriptions={"建造房屋，增加2名可派驻村民，总容量最多9人。\n靠近居民房屋也可结束一天。\n拆除前需召回超出容量的村民。","建造仓库存放物资，使每个资源点的原料、成品容量各增加30，总容量最多120。\n拆除不会吞掉已有库存，超额库存领取后恢复容量。","复用现有自由围栏。关好门可阻挡野兽，野兽会啃咬围栏，三次后这段围栏破损。\n白天点击受损围栏可花木材1修补。","与围栏相连的门。夜袭前关好门；打开的门无法阻挡野兽。","选择后点击空地放置：角色六步内、已修复资源点八步内。\n陷阱会击退接触的一只野兽，触发后消耗；再次布置可补充。"};
            for(int i=0;i<names.Length;i++){int choice=i;Row(names[i],costs[i],i,()=>{campChoice=choice;Rebuild();},campChoice==i,62);}
            var state=A.homestead;var sites=game.Homestead.Sites();selectedSite=Mathf.Clamp(selectedSite,0,sites.Count-1);
            Text(content,"房屋 "+A.buildings.Count(b=>b.kind==4)+" · 仓库 "+A.buildings.Count(b=>b.kind==5)+"\n村民容量 "+state.WorkerCapacity(A)+" · 库存容量 "+state.StorageCapacity(A)+" · 陷阱 "+A.defense.traps.Count(t=>!t.spent),0,382,620,62,19,cream);
            Text(content,game.Night.Forecast,0,455,620,54,18,gold);
            Button(content,"修补 "+game.Homestead.SiteName(selectedSite)+" · 耐久"+(100-sites[selectedSite].damage),0,520,444,46,()=>game.Night.Repair(selectedSite));
            Button(content,"下一资源点",454,520,166,46,()=>{selectedSite=(selectedSite+1)%sites.Count;Rebuild();});
            var detail=Details(names[campChoice],costs[campChoice],descriptions[campChoice]+"\n\n每逢第3、6、9…天，有派驻村民的资源点会遭袭。\n回房屋休息将前往资源点，守卫60秒。\n三波野兽；E驱赶，R修补设施。\n守住奖励15金币；失败损失半数库存并停产一天。");
            DetailAction(detail,"开始布置"+names[campChoice],0,()=>{if(campChoice<2)game.Construction.Choose(campChoice+4);else if(campChoice<4){Close();game.Fences.Choose(campChoice-2);}else game.Night.ChooseTrap();});
            DetailAction(detail,"回房屋休息 / 开始今夜守卫",1,game.Sleep);
        }
        void ResourcePage()
        {
            var h=game.Homestead;var sites=h.Sites();var state=A.homestead;
            selectedSite=Mathf.Clamp(selectedSite,0,sites.Count-1);int pages=Mathf.CeilToInt(sites.Count/5f);sitePage=Mathf.Clamp(sitePage,0,pages-1);
            for(int i=sitePage*5;i<Mathf.Min(sites.Count,sitePage*5+5);i++)
            {
                int id=i;var site=sites[i];Row(h.SiteName(i),site.level==0?"废弃 · 等待修复":"等级"+site.level+" · 村民"+site.workers+" / 2 · 库存"+site.stock+" · 成品"+site.goods,i-sitePage*5,()=>{selectedSite=id;Rebuild();},selectedSite==id,62);
            }
            Button(content,"上一页",0,388,120,38,()=>{sitePage--;Rebuild();},sitePage>0);Text(content,(sitePage+1)+" / "+pages,142,394,160,30,17,cream);Button(content,"下一页",310,388,120,38,()=>{sitePage++;Rebuild();},sitePage<pages-1);
            Text(content,"村民 "+state.UsedWorkers(A)+" / "+state.WorkerCapacity(A)+" 已派驻 · 每处最多2人\n木材 "+A.wood+" · 石料 "+A.stone+" · 木板 "+state.planks+" · 石砖 "+state.bricks,0,446,620,68,20,cream);
            Text(content,"休息结算生产，库存随仓库增加。靠近六步内领取和施工。\n自建林场与采石场也在此分配村民、升级和领取。",0,530,620,58,17,gold);
            int chosen=selectedSite;var s=sites[chosen];int kind=h.SiteKind(chosen);bool near=h.Near(h.SitePosition(chosen),6);
            var panel=Box(content,"Production details",654,8,506,574,cream);UITheme.Frame(panel);
            UITheme.Icon(panel,kind,18,12,58,58,false,true);Text(panel,h.SiteName(chosen),92,23,392,40,25,ink);
            string cost=s.level==0?"修复：木4 石4 金10":s.level==1?"升级：木8 石6 金20":s.level==2?"升级：木12 石10 金35 木板2 石砖2":"已达到三级上限";
            Text(panel,(s.level==0?"废弃":s.level+"级 · 每人每日原料+"+((s.level+1)*2))+"\n"+cost+"\n库存："+(kind==0?"木材":"石料")+s.stock+" / "+state.StorageCapacity(A)+" · "+(kind==0?"木板":"石砖")+s.goods+" / "+state.StorageCapacity(A)+"\n耐久："+(100-s.damage)+" / 100 · 停产"+s.downtime+"天\n加工："+(s.processing?"开启":"关闭")+" · 二级解锁\n每人每日消耗4原料，产出2成品。\n位置 ["+h.SitePosition(chosen).x.ToString("F0")+","+h.SitePosition(chosen).z.ToString("F0")+"] · "+(near?"可施工与领取":"需靠近六步内施工与领取"),22,82,462,210,18,ink);
            Button(panel,s.level==0?"修复资源点":s.level<3?"升级资源点":"已经满级",22,302,462,42,()=>h.Improve(chosen),s.level<3&&near);
            Button(panel,"派驻村民 +1",22,354,224,42,()=>h.Assign(chosen,1),s.level>0&&s.workers<2&&state.UsedWorkers(A)<state.WorkerCapacity(A));
            Button(panel,"召回村民 -1",260,354,224,42,()=>h.Assign(chosen,-1),s.workers>0);
            Button(panel,s.level<2?"二级解锁加工":s.processing?"关闭加工":"开启加工",22,406,462,42,()=>h.ToggleProcessing(chosen),s.level>=2);
            Button(panel,"领取全部原料与成品",22,458,462,42,()=>h.Collect(chosen),near&&(s.stock>0||s.goods>0));
            Button(panel,s.damage>0?"修补设施 · 木2石1":"返回世界前往资源点",22,510,462,42,()=>{if(s.damage>0)game.Night.Repair(chosen);else Close();});
        }
    }
}
