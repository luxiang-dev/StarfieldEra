using System;
using System.Linq;
using UnityEngine;
namespace StarfieldEra
{
    public sealed partial class ManagementHud
    {
        int residentSelection,residentPage,workplaceSelection,orderSelection,caravanSelection,frontierSelection;
        public void OpenVillager(int id){residentSelection=id;residentPage=id/6;Open("villagers");}
        public void OpenFrontier(int id){frontierSelection=Mathf.Clamp(id,0,1);Open("frontier");}
        void SettlementWorkspace()
        {
            OperationsNavigation();
            if(page=="villagers")VillagersPage();else if(page=="orders")OrdersPage();else if(page=="caravan")CaravanPage();else FrontierPage();
        }
        void OperationsNavigation()
        {
            string[] labels={"资源生产","自建建筑","营地守卫","村民生活","村庄订单"};string[] keys={"production","buildings","camp","villagers","orders"};
            for(int i=0;i<keys.Length;i++){string key=keys[i];Chip(labels[i],i*124,()=>Open(key),page==key);}
        }
        void VillagersPage()
        {
            var village=A.village;village.Ensure(A);int beds=A.homestead.WorkerCapacity(A);var places=VillageState.Workplaces(A);
            Text(content,"村民 "+village.residents.Count+" / 床位 "+beds+" · 口粮 "+village.pantry+"（每日每人1份）",0,52,620,34,18,gold);
            residentPage=Mathf.Clamp(residentPage,0,(village.residents.Count-1)/6);
            for(int i=residentPage*6;i<Math.Min(residentPage*6+6,village.residents.Count);i++)
            {
                var r=village.residents[i];int id=r.id;string job=places.Find(p=>p.site.workplaceId==r.workplace)?.name??"自由活动";
                var button=Button(content,"",0,94+(i%6)*54,620,48,()=>{residentSelection=id;Rebuild();});UITheme.Icon(button.transform,r.specialty==0?0:r.specialty==1?1:17,8,4,38,38);
                Text(button.transform,VillageState.Names[id]+" · "+VillageState.Specialties[r.specialty]+"专长",58,3,280,30,18,cream);Text(button.transform,job+(r.hunger>0?" · 饥饿":" · 已用餐")+(r.id>=beds?" · 缺床位":""),58,27,540,20,13,gold);
            }
            Button(content,"上一页",0,427,145,40,()=>{residentPage--;Rebuild();},residentPage>0);Button(content,"下一页",158,427,145,40,()=>{residentPage++;Rebuild();},residentPage*6+6<village.residents.Count);
            Text(content,"补充村民口粮（不会自动扣取背包食物）",0,467,620,28,16,gold);
            string[] foods={"胡萝卜3 → 口粮3","鲜鱼2 → 口粮4","浆果3 → 口粮2","料理1 → 口粮6"};for(int i=0;i<4;i++){int food=i;var foodButton=Button(content,foods[i],i*157,503,149,40,()=>game.Settlement.Donate(food));foodButton.GetComponentInChildren<TMPro.TextMeshProUGUI>().fontSize=15;}
            Button(content,"招募村民 · 金币20、口粮6，需空床位",0,552,620,42,game.Settlement.Recruit,village.residents.Count<beds&&village.pantry>=6&&game.State.coins>=20);
            residentSelection=Mathf.Clamp(residentSelection,0,village.residents.Count-1);var person=village.residents[residentSelection];var assigned=places.Find(p=>p.site.workplaceId==person.workplace);
            var detail=Details(VillageState.Names[person.id],VillageState.Specialties[person.specialty]+"专长 · 熟练工作 "+person.workedDays+" 天","当前岗位："+(assigned?.name??"自由活动")+"\n用餐："+(person.hunger==0?"已经吃饱":"饥饿 "+person.hunger+" 天")+" · 心情 "+person.morale+" / 100\n住房："+(person.id<beds?"已有床位":"缺少床位，请建居民房屋")+"\n\n匹配专长每工作一天提高5%效率，最高30%。\n饥饿效率降低30%，缺床位再降低20%。\n休息后消耗口粮，并完成一天工作。");
            var available=places.Where(p=>p.site.level>0).ToList();workplaceSelection=available.Count==0?0:Mathf.Clamp(workplaceSelection,0,available.Count-1);var target=available.Count==0?null:available[workplaceSelection];
            Text(detail,target==null?"请先修复资源点或建造工坊":"准备派往："+target.name+" · "+target.site.workers+" / 2",22,342,462,32,16,ink);
            Button(detail,"上一岗位",22,376,224,38,()=>{workplaceSelection=(workplaceSelection-1+available.Count)%available.Count;Rebuild();},available.Count>1);Button(detail,"下一岗位",258,376,226,38,()=>{workplaceSelection=(workplaceSelection+1)%available.Count;Rebuild();},available.Count>1);
            DetailAction(detail,"确认派驻这个岗位",0,()=>game.Settlement.Assign(person.id,target.site.workplaceId),target!=null&&target.site.workers<2&&person.workplace!=target.site.workplaceId);
            DetailAction(detail,"召回村民 · 自由活动",1,()=>game.Settlement.Assign(person.id,0),person.workplace>0);
        }
        void OrdersPage()
        {
            var trade=A.trade;trade.Ensure(game.State.day);var offers=trade.orders.Where(o=>o.cycle==trade.cycle).ToList();orderSelection=Mathf.Clamp(orderSelection,0,offers.Count-1);
            Text(content,"本期截止：第"+(trade.cycle*4+4)+"天 · 声望 "+A.village.reputation+" · 累计完成 "+trade.completed,0,54,620,34,18,gold);
            for(int i=0;i<offers.Count;i++){int n=i;var o=offers[i];var recipe=VillageTradeState.Recipes[o.template];var b=Button(content,"",0,104+i*102,620,88,()=>{orderSelection=n;Rebuild();});UITheme.Icon(b.transform,17+i,12,12,56,56);Text(b.transform,recipe.name,84,9,510,34,22,cream);Text(b.transform,(o.status==0?"待接受":o.status==1?"已接受，待交付":"已经完成")+" · 奖励金币"+recipe.gold+" / 声望"+recipe.reputation,84,48,510,28,16,gold);}
            Text(content,"每4天更新订单。到期未交付将失效，不扣除材料。\n接受与交付都需要走近城镇议事牌。",0,426,620,68,18,cream);
            Button(content,"村庄建设与研究",0,521,300,46,()=>Open("town"));Button(content,"查看商队货品",320,521,300,46,()=>Open("caravan"));
            var selected=offers[orderSelection];var r=VillageTradeState.Recipes[selected.template];var detail=Details(r.name,"第"+(trade.cycle*4+4)+"天结束前交付",r.Needs+"\n\n奖励：金币"+r.gold+" · 声望"+r.reputation+"\n"+(selected.template==0?"交付后公共修桥备料就绪，修桥不再消耗木板。":selected.template==1?"交付后矿道哨塔备料就绪，清理矿道不再消耗石砖。":"交付支持村庄发展，提高声望。")+"\n"+(game.Settlement.NearBoard?"已在议事牌旁。":"需走近议事牌，当前只能查看。"));
            DetailAction(detail,selected.status==0?"接受订单":selected.status==1?"交付全部物资":"本订单已经完成",0,()=>game.Settlement.OrderAction(selected),game.Settlement.NearBoard&&selected.status<2&&(selected.status==0||r.CanPay(game.State)));
            DetailAction(detail,"查看路线建设",1,()=>Open("frontier"));
        }
        void CaravanPage()
        {
            var trade=A.trade;trade.Ensure(game.State.day);bool present=VillageTradeState.CaravanPresent(game.State.day);Text(content,present?"商队正在村口停留 · 第"+(trade.cycle*4+2)+"天后离开":"商队在路上 · 第"+VillageTradeState.NextCaravan(game.State.day)+"天抵达",0,54,620,34,18,gold);
            for(int i=0;i<6;i++){int n=i;var b=Button(content,"",0,98+i*64,620,56,()=>{caravanSelection=n;Rebuild();});UITheme.Icon(b.transform,i==0?14:i==4?17:11,8,5,44,44);Text(b.transform,VillageTradeState.Goods[i],64,4,400,30,19,cream);Text(b.transform,"金币 "+VillageTradeState.Prices[i]+" · 本期剩余 "+(VillageTradeState.Limits[i]-trade.purchases[i]),64,33,530,22,14,gold);}
            Button(content,"在身前空地放置旗帜",0,505,300,42,game.Settlement.PlaceBanner,A.village.banners>0);Button(content,"收回四步内最近的旗帜",320,505,300,42,game.Settlement.RecoverBanner,A.frontier.decorations.Count>0);Text(content,"旗帜背包 "+A.village.banners+" · 已布置 "+A.frontier.decorations.Count+"\n交易需走近村口商队；限购数量在下期重置。",0,550,620,48,15,cream);
            int offer=Mathf.Clamp(caravanSelection,0,5);string[] notes={"珍稀作物，五天成熟。\n每次收获金币60，并获得村民口粮3。\n在背包选择种子，再到农田种植。","学习后可制作工程铁铲，开挖河道、改造地形和修排水沟。","学习后可制作育林手套，与铁铲配合栽种树木。","学习后可制作湿地长靴，改善沼泽和雪地行走速度。","在人物身前的空地放置绿色金纹旗帜。\n可以在四步内收回，移动后再次布置。","六份村民日常口粮。\n每位村民每天需要一份口粮。"};
            string block=trade.BuyBlock(game.State,offer);var detail=Details(VillageTradeState.Goods[offer],"金币 "+VillageTradeState.Prices[offer],notes[offer]+"\n\n"+(block==""?game.Settlement.Near(SettlementGame.CaravanPosition)?"商队就在身旁，可以交易。":"请走到村口商队旁购买。":block));
            DetailAction(detail,"购买这件商品",0,()=>game.Settlement.Buy(offer),present&&game.Settlement.Near(SettlementGame.CaravanPosition)&&block=="");DetailAction(detail,offer>=1&&offer<=3?"查看装备图纸":"查看背包与种子",1,()=>{if(offer>=1&&offer<=3){equipmentView=true;blueprintView=true;Open("craft");}else Open("inventory");});
        }
        void FrontierPage()
        {
            for(int i=0;i<2;i++){int n=i;var f=A.frontier;var b=Button(content,"",0,86+i*120,620,104,()=>{frontierSelection=n;Rebuild();});UITheme.Icon(b.transform,i==0?6:5,12,14,66,66);Text(b.transform,FrontierState.Names[i],94,12,500,36,24,cream);Text(b.transform,(f.unlocked[i]?f.visited[i]?"已发现 · 可以旅行":"路线开放 · 等待步行探索":"路线受阻 · 需修复")+"\n入口坐标 "+FrontierState.Entrance(i).x+"，"+FrontierState.Entrance(i).z,94,49,500,54,16,gold);}
            Text(content,"今日积水 "+game.State.storm.puddles.Count+" 格 · 排水沟 "+game.State.land.cells.Count(c=>c.drainage)+" 格\n大雨和雷暴使低洼土地积水，晴天逐渐退水。\n水深超过15厘米会暂停农作物成长；\n深水不能通行，浅水会减慢移动。",0,358,620,136,18,cream);
            Button(content,"准备排水沟 · J 土地工具",0,514,620,44,()=>{selectedLandTool=8;Open("land");});
            int id=Mathf.Clamp(frontierSelection,0,1);var state=A.frontier;var detail=Details(FrontierState.Names[id],state.unlocked[id]?"路线已经开放":id==0?"修复断桥，进入林谷":"清理矿道，建设哨塔",(id==0?"古林：每日木材6、纤维4。":"矿脉：每日石料8、晶石2、星石1。")+"\n银耳兔：观察获得纤维与生存经验。\n废弃营地：一次性宝箱，含珍稀种子。\n\n施工需要：\n"+state.Cost(id)+"\n\n"+(id==0?state.bridgeCredit?"议事牌订单已备好修桥木板。":"交付修桥木板订单可抵扣木板费用。":state.towerCredit?"议事牌订单已备好矿道石砖。":"交付哨塔订单可抵扣石砖费用。"));
            DetailAction(detail,state.unlocked[id]?"道路已经修复":"在入口施工并开放区域",0,()=>game.Settlement.Repair(id),!state.unlocked[id]&&game.Settlement.Near(FrontierState.Entrance(id),6));
            DetailAction(detail,state.visited[id]?"前往已发现的区域":"先步行进入，解锁旅行",1,()=>game.Settlement.Travel(id),state.unlocked[id]&&state.visited[id]);
        }
        void FrontierMapPins(RectTransform map)
        {
            for(int i=0;i<2;i++){int id=i;var p=FrontierState.Entrance(i);var b=Button(map,FrontierState.Names[i]+(A.frontier.unlocked[i]?"":" · 未通"),0,0,114,30,()=>OpenFrontier(id));var r=b.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;r.anchoredPosition=new Vector2(p.x/(HexWorldGrid.HalfWidth*2)*620,p.z/(HexWorldGrid.HalfDepth*2)*510);b.GetComponentInChildren<TMPro.TextMeshProUGUI>().fontSize=12;}
        }
    }
}
