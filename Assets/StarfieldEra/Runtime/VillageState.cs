using System;
using System.Collections.Generic;
using System.Linq;
namespace StarfieldEra
{
    [Serializable] public sealed class ResidentState
    {public int id,specialty,workplace,workedDays,hunger,morale=75;
        public int role,cargoWood,cargoStone,cargoPlanks,cargoBricks;
        public bool HasCargo=>cargoWood+cargoStone+cargoPlanks+cargoBricks>0;}
    public sealed class VillageWorkplace
    {public ResourceSiteState site;public int kind;public string name;public UnityEngine.Vector3 position;}
    [Serializable] public sealed class VillageState
    {
        public int version,pantry=9,lastDay,lastTrainingDay,reputation,banners;
        public List<ResidentState> residents=new List<ResidentState>();
        public static readonly string[] Names={"阿松","石禾","小栎","青葵","阿砾","木棉","云杉","小满","南星"};
        public static readonly string[] Roles={"生产 / 自由活动","搬运工","维修工","巡逻守卫"};
        public static readonly string[] Specialties={"伐木","采矿","加工"};
        public static List<VillageWorkplace> Workplaces(AdventureState a)
        {
            var result=new List<VillageWorkplace>{new VillageWorkplace{site=a.homestead.lumber,kind=0,name="青苔林场",position=new UnityEngine.Vector3(19,0,-10)},new VillageWorkplace{site=a.homestead.quarry,kind=1,name="白岩采石场",position=new UnityEngine.Vector3(20,0,18)}};
            if(a.FixedTown(6))result.Add(new VillageWorkplace{site=a.homestead.workshop,kind=2,name="村庄工坊",position=new UnityEngine.Vector3(-10,0,13)});
            foreach(var b in a.buildings)if(b!=null&&(b.kind<2||b.kind==6)&&b.operation!=null)result.Add(new VillageWorkplace{site=b.operation,kind=b.kind==6?2:b.kind,name=HexConstruction.Names[b.kind]+" ["+b.column+","+b.row+"]",position=HexWorldGrid.Center(b.column,b.row)});
            return result;
        }
        public void Ensure(AdventureState a)
        {
            if(residents==null)residents=new List<ResidentState>();var places=Workplaces(a);if(residents.Any(v=>v==null)||places.Any(p=>!p.site.Valid()))return;var used=new HashSet<int>();int next=places.Max(p=>p.site.workplaceId)+1;
            foreach(var p in places){if(p.site.workplaceId<=0||!used.Add(p.site.workplaceId)){p.site.workplaceId=next++;used.Add(p.site.workplaceId);}}
            if(version==0){version=1;while(residents.Count<Math.Min(9,Math.Max(3,places.Sum(p=>p.site.workers))))AddResident();}
            // Import legacy worker counts, while retaining named assignments wherever possible.
            foreach(var r in residents)if(!places.Any(p=>p.site.workplaceId==r.workplace))r.workplace=0;
            foreach(var p in places)
            {
                var assigned=residents.Where(r=>r.workplace==p.site.workplaceId).ToList();
                while(assigned.Count>p.site.workers){assigned[assigned.Count-1].workplace=0;assigned.RemoveAt(assigned.Count-1);}
            }
            foreach(var p in places)while(residents.Count(r=>r.workplace==p.site.workplaceId)<p.site.workers)
            {
                var r=residents.Where(v=>v.workplace==0&&v.role==0).OrderByDescending(v=>v.specialty==p.kind).FirstOrDefault();
                if(r==null&&residents.Count<9)r=AddResident();if(r==null)break;r.workplace=p.site.workplaceId;
            }
        }
        ResidentState AddResident(){var r=new ResidentState{id=residents.Count,specialty=residents.Count%3};residents.Add(r);return r;}
        public bool Recruit(FarmState farm)
        {Ensure(farm.adventure);if(residents.Count>=farm.adventure.homestead.WorkerCapacity(farm.adventure)||pantry<6||farm.coins<20)return false;pantry-=6;farm.coins-=20;AddResident();return true;}
        public bool Assign(AdventureState a,ResidentState r,ResourceSiteState target)
        {
            Ensure(a);if(!residents.Contains(r))return false;var places=Workplaces(a);var p=target==null?null:places.Find(x=>x.site==target);
            if(target!=null&&(p==null||target.level==0||target.workers>=2||r.workplace==target.workplaceId))return false;
            if(target!=null&&r.workplace==0&&a.homestead.UsedWorkers(a)>=a.homestead.WorkerCapacity(a))return false;
            var old=places.Find(x=>x.site.workplaceId==r.workplace);if(old!=null)old.site.workers--;r.workplace=target?.workplaceId??0;if(target!=null)target.workers++;r.role=0;return true;
        }
        public bool Adjust(AdventureState a,ResourceSiteState site,int delta)
        {
            Ensure(a);var place=Workplaces(a).Find(p=>p.site==site);if(place==null||Math.Abs(delta)!=1)return false;
            var r=delta>0?residents.Where(v=>v.workplace==0&&v.role==0).OrderByDescending(v=>v.specialty==place.kind).FirstOrDefault():residents.LastOrDefault(v=>v.workplace==site.workplaceId);
            return r!=null&&Assign(a,r,delta>0?site:null);
        }
        public float Productivity(AdventureState a,ResourceSiteState s)
        {
            if(version==0)return 1;var p=Workplaces(a).Find(x=>x.site==s);if(p==null)return 1;var workers=residents.Where(r=>r.workplace==s.workplaceId).ToList();if(workers.Count==0)return 1;
            return workers.Average(r=>(r.hunger==0?1f:.7f)*(r.id<a.homestead.WorkerCapacity(a)?1f:.8f)*(r.specialty==p.kind?1+Math.Min(.3f,r.workedDays*.05f):1));
        }
        public void BeginDay(FarmState farm)
        {
            Ensure(farm.adventure);if(lastDay>=farm.day)return;lastDay=farm.day;int beds=farm.adventure.homestead.WorkerCapacity(farm.adventure);
            foreach(var r in residents){bool fed=pantry>0;if(fed)pantry--;r.hunger=fed?0:Math.Min(3,r.hunger+1);r.morale=Math.Max(0,Math.Min(100,r.morale+(fed?3:-10)+(r.id<beds?1:-8)));}
        }
        public void EndDay(int day){if(lastTrainingDay>=day)return;lastTrainingDay=day;foreach(var r in residents)if(r.workplace>0&&r.hunger==0)r.workedDays++;}
        public bool Donate(FarmState f,int food)
        {
            var a=f.adventure;if(pantry>99980)return false;
            if(food==0){if(a.homestead.carrots<3)return false;a.homestead.carrots-=3;pantry+=3;}
            else if(food==1){if(a.fish<2)return false;a.fish-=2;pantry+=4;}
            else if(food==2){if(a.berries<3)return false;a.berries-=3;pantry+=2;}
            else if(food==3){if(a.meals<1)return false;a.meals--;pantry+=6;}else return false;return true;
        }
        public bool Valid()=>version>=0&&version<=1&&pantry>=0&&pantry<=99999&&lastDay>=0&&lastTrainingDay>=0&&reputation>=0&&banners>=0&&banners<=99&&residents!=null&&residents.Count<=9&&residents.Select(r=>r?.id??-1).Distinct().Count()==residents.Count&&residents.All(r=>r!=null&&r.id>=0&&r.id<9&&r.specialty>=0&&r.specialty<3&&r.workplace>=0&&r.role>=0&&r.role<=3&&(r.role==0||r.workplace==0)&&r.cargoWood>=0&&r.cargoWood<=100&&r.cargoStone>=0&&r.cargoStone<=100&&r.cargoPlanks>=0&&r.cargoPlanks<=100&&r.cargoBricks>=0&&r.cargoBricks<=100&&r.workedDays>=0&&r.hunger>=0&&r.hunger<=3&&r.morale>=0&&r.morale<=100);
    }
}
