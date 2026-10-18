using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace StarfieldEra
{
    public sealed class RockMining:MonoBehaviour
    {
        FarmGame game;readonly List<HarvestableRock> rocks=new List<HarvestableRock>();float nextSwing;
        public HarvestableRock Target {get;private set;}
        public bool HasNearby=>Target&&!Target.Depleted;
        public string Hint=>HasNearby?"石料 · E 开采 · 体力2 · 采完碎裂消失":"";
        public void Initialize(FarmGame owner)
        {
            game=owner;
            foreach(var rock in game.World.GetComponentsInChildren<HarvestableRock>(true))Register(rock);
            UpdateTarget();
        }
        public void Register(HarvestableRock rock)
        {
            if(!game||rocks.Contains(rock))return;
            // Resource markers own labels and old fallback geometry as well as their new model.
            foreach(var encounter in game.World.Adventure.Encounters)
                if(encounter.kind==EncounterKind.Stone&&rock.transform.IsChildOf(encounter.root)){rock.source=encounter.root;break;}
            rocks.Add(rock);
            if(game.State.mining.Contains(rock.transform.position))rock.Hide();
        }
        public bool Reachable(HarvestableRock rock)
        {
            if(!rock||rock.Depleted||!rock.gameObject.activeInHierarchy)return false;
            var delta=rock.transform.position-game.World.Player.position;delta.y=0;
            return delta.magnitude<2.7f&&(!game.World.Adventure.CameraRig.IsFirstPerson||Vector3.Dot(delta.normalized,game.World.Player.forward)>=.15f);
        }
        public void UpdateTarget()
        {
            Target=null;if(!game)return;float nearest=2.7f;rocks.RemoveAll(r=>!r);
            foreach(var rock in rocks)if(Reachable(rock))
            {float d=WorldRelief.PlanarDistance(rock.transform.position,game.World.Player.position);if(d<nearest){nearest=d;Target=rock;}}
        }
        public void Mine(HarvestableRock requested=null)
        {
            if(!game||game.Management.IsOpen||game.Adventure.InBattle||game.Expedition.Fishing||game.Night.Active||game.Night.TrapPlacing||game.Construction.Active||game.Fences.Active||game.Land.Active||game.Construction.Menu.IsOpen)return;
            if(!game.IsGrounded){game.ShowMessage("落地后再开采。");return;}
            UpdateTarget();if(!requested&&game.Inspection&&game.Inspection.HasSelection){requested=game.Inspection.Current.rock;if(!requested){game.ShowMessage("选中的目标不是石头。");return;}}var rock=requested?requested:Target;
            if(!Reachable(rock)){game.ShowMessage("走近石头再开采，第一人称需要面向石头。");return;}
            if(Time.time<nextSwing||!EquipmentCatalog.Require(game,"pick"))return;
            if(game.State.energy<2){game.ShowMessage("体力不足，使用料理或回小屋休息。");return;}
            if(!game.State.mining.Mine(rock.transform.position)){game.ShowMessage("石头已采完，或采集记录已满。");return;}
            nextSwing=Time.time+.45f;game.State.SpendEnergy(2);
            StartCoroutine(Shatter(rock));rock.Hide();
            int amount=3+game.State.adventure.toolLevel*2+WeatherCatalog.Get(game.State.Weather).extraStone;
            game.State.adventure.stone+=amount;game.State.weather.survivalExperience++;
            game.Adventure.UpdateTarget();UpdateTarget();game.Adventure.Commit("开采石料 +"+amount+"，体力 -2。");
        }
        IEnumerator Shatter(HarvestableRock rock)
        {
            var renderers=rock.GetComponentsInChildren<Renderer>();var bounds=new Bounds(rock.transform.position,Vector3.one*.5f);Material material=null;
            foreach(var r in renderers)if(r.enabled){bounds.Encapsulate(r.bounds);if(!material)material=r.sharedMaterial;}
            var debris=new GameObject("Rock shatter debris");debris.transform.SetParent(transform,false);
            const int count=10;var pieces=new Transform[count];var starts=new Vector3[count];var velocities=new Vector3[count];var scales=new Vector3[count];
            for(int i=0;i<count;i++)
            {
                float angle=i*Mathf.PI*2/count;
                var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name="Stone fragment";go.transform.SetParent(debris.transform,false);
                Destroy(go.GetComponent<Collider>());if(material)go.GetComponent<Renderer>().sharedMaterial=material;
                pieces[i]=go.transform;starts[i]=bounds.center+new Vector3(Mathf.Cos(angle)*bounds.extents.x*.4f,0,Mathf.Sin(angle)*bounds.extents.z*.4f);
                velocities[i]=new Vector3(Mathf.Cos(angle)*(1+i%3*.3f),2+i%3*.4f,Mathf.Sin(angle)*(1+i%3*.3f));
                scales[i]=Vector3.one*Mathf.Clamp(bounds.size.magnitude*.08f,.07f,.24f);pieces[i].position=starts[i];pieces[i].localScale=scales[i];
            }
            float elapsed=0;
            while(debris&&elapsed<.8f)
            {
                if(game.Management.IsOpen){yield return null;continue;}
                elapsed+=Time.deltaTime;
                for(int i=0;i<count;i++)
                {
                    var p=starts[i]+velocities[i]*elapsed+Vector3.down*(5*elapsed*elapsed);p.y=Mathf.Max(p.y,game.World.HexGrid.GroundHeight(p)+.04f);
                    pieces[i].position=p;pieces[i].Rotate(new Vector3(160,95,80)*Time.deltaTime);pieces[i].localScale=scales[i]*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.4f,.8f,elapsed)));
                }
                yield return null;
            }
            if(debris)Destroy(debris);
        }
    }
}
