using System;
using System.Collections.Generic;
using System.Linq;

namespace StarfieldEra
{
    [Serializable] public sealed class RabbitRecord
    {
        public int id, status, fedDay=-1, care;
        // 0 wild, 1 carried, 2 housed, 3 returned to villager.
    }
    [Serializable] public sealed class ResourceSiteState
    {
        public int workplaceId;
        public int level, workers, stock, goods, damage, downtime;
        public bool processing;
        public bool Valid()=>level>=0&&level<=3&&workers>=0&&workers<=2&&stock>=0&&stock<=1200&&goods>=0&&goods<=1200&&damage>=0&&damage<=100&&downtime>=0&&downtime<=1&&(level>0||workers==0)&&(!processing||level>=2);
        public void NewDay(int capacity=300,float productivity=1)
        {
            if(downtime>0){downtime--;return;}
            if(level==0||workers==0||damage>=100)return;
            int raw=(int)Math.Floor(workers*(level+1)*20*productivity+.0001f);
            stock+=Math.Min(raw,Math.Max(0,capacity-stock));
            if(processing){int amount=Math.Min(workers*20,Math.Min(stock/2,Math.Max(0,capacity-goods)));stock-=amount*2;goods+=amount;}
        }
    }
    [Serializable] public sealed class HomesteadState
    {
        public int carrots, planks, bricks, rabbitQuest;
        public List<RabbitRecord> rabbits=new List<RabbitRecord>();
        public ResourceSiteState workshop=new ResourceSiteState{level=1};
        public ResourceSiteState lumber=new ResourceSiteState(), quarry=new ResourceSiteState();
        public void Upgrade()
        {
            if(workshop==null)workshop=new ResourceSiteState{level=1};
            if(rabbits==null)rabbits=new List<RabbitRecord>();
            if(lumber==null)lumber=new ResourceSiteState();if(quarry==null)quarry=new ResourceSiteState();
        }
        public IEnumerable<ResourceSiteState> Sites(AdventureState adventure)
        {
            yield return lumber;yield return quarry;
            if(adventure.buildings==null)yield break;
            foreach(var b in adventure.buildings)if(b!=null&&b.kind<2&&b.operation!=null)yield return b.operation;
        }
        public IEnumerable<ResourceSiteState> Workshops(AdventureState a)
        {
            if(a.FixedTown(6))yield return workshop;
            if(a.buildings!=null)foreach(var b in a.buildings)if(b!=null&&b.kind==6)yield return b.operation??new ResourceSiteState{level=1};
        }
        public int WorkerCapacity(AdventureState adventure)=>Math.Min(9,3+2*(adventure.buildings?.Count(b=>b!=null&&b.kind==4)??0));
        public int StorageCapacity(AdventureState adventure)=>Math.Min(1200,300+300*(adventure.buildings?.Count(b=>b!=null&&b.kind==5)??0));
        public int UsedWorkers(AdventureState adventure)=>Sites(adventure).Sum(s=>s.workers)+Workshops(adventure).Sum(s=>s.workers);
        public bool Valid(AdventureState adventure)=>carrots>=0&&planks>=0&&bricks>=0&&rabbitQuest>=0&&rabbitQuest<=2&&rabbits!=null&&rabbits.Count<=5&&rabbits.All(r=>r!=null&&r.id>=0&&r.id<5&&r.status>=1&&r.status<=3&&r.fedDay>=-1&&r.care>=0)&&rabbits.Select(r=>r.id).Distinct().Count()==rabbits.Count&&rabbits.Count(r=>r.status==1)<=1&&rabbits.Count(r=>r.status==2)<=3&&lumber!=null&&quarry!=null&&workshop!=null&&workshop.Valid()&&Workshops(adventure).All(s=>s.Valid())&&Sites(adventure).All(s=>s.Valid())&&UsedWorkers(adventure)<=WorkerCapacity(adventure);
        public void NewDay(FarmState farm)
        {
            foreach(var rabbit in rabbits)if(rabbit.status==2&&rabbit.fedDay==farm.day-1){rabbit.care++;farm.coins+=4;}
            foreach(var site in Sites(farm.adventure))site.NewDay(StorageCapacity(farm.adventure),farm.adventure.village.Productivity(farm.adventure,site));
        }
        public bool BuyCarrots(FarmState farm)
        {if(farm.coins<6)return false;farm.coins-=6;carrots+=3;return true;}
        public bool Improve(FarmState farm,ResourceSiteState site)
        {
            if(site==null||!Sites(farm.adventure).Contains(site)||site.level>=3)return false;
            int wood=site.level==0?4:site.level==1?8:12,stone=site.level==0?4:site.level==1?6:10,gold=site.level==0?10:site.level==1?20:35;
            if(farm.adventure.wood<wood||farm.adventure.stone<stone||farm.coins<gold)return false;
            if(site.level==2&&(planks<2||bricks<2))return false;
            farm.adventure.wood-=wood;farm.adventure.stone-=stone;farm.coins-=gold;
            if(site.level==2){planks-=2;bricks-=2;}site.level++;return true;
        }
        public bool Assign(AdventureState adventure,ResourceSiteState site,int delta)
        {
            if(site==null||!Sites(adventure).Contains(site)||site.level==0||Math.Abs(delta)!=1||site.workers+delta<0||site.workers+delta>2||(delta>0&&UsedWorkers(adventure)>=WorkerCapacity(adventure)))return false;
            return adventure.village.Adjust(adventure,site,delta);
        }
        public void Collect(AdventureState adventure,ResourceSiteState site,int kind)
        {
            if(site==null||!Sites(adventure).Contains(site))return;
            if(kind==0){adventure.wood+=site.stock;planks+=site.goods;}else{adventure.stone+=site.stock;bricks+=site.goods;}site.stock=site.goods=0;
        }
    }
}
