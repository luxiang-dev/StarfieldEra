using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
namespace StarfieldEra
{
    public sealed class GardenActor:MonoBehaviour
    {
        public bool chasing,dying;public float alertUntil,lostSight,thinkAt,thinkTime,repathAt,stunnedUntil,slowedUntil;
        public Vector3 pathGoal;public int pathIndex;public readonly System.Collections.Generic.List<Vector3> path=new System.Collections.Generic.List<Vector3>();
        Renderer[] surfaces;MaterialPropertyBlock tint;float flashUntil,fallTime;Quaternion fallRotation;
        public CaveEnemy caveEnemy;
        public Light lamp;public Transform armedSignal;
        public GardenUnit unit;public GardenZombie zombie;public GardenDefense owner;
        PlayableGraph graph;AnimationMixerPlayable mixer;AnimationClipPlayable walk,sad;
        public int Health=>unit!=null?unit.health:zombie!=null?zombie.health:0;
        public int MaxHealth=>caveEnemy?(caveEnemy.index==4?900:caveEnemy.index<2?180:220):unit!=null?unit.MaxHealth:zombie!=null?zombie.MaxHealth:60;
        public string DisplayName=>caveEnemy?(caveEnemy.index==4?"矿主 · 首领":"矿洞精英守卫"):zombie!=null?(zombie.variant==1?"路障僵尸":zombie.variant==2?"铁桶僵尸":zombie.variant==3?"壮汉僵尸":zombie.tier==0?"僵尸":zombie.tier==1?"荒野守卫":"远岭守卫"):unit.kind==2?(unit.tier==0?"营地墓碑":unit.tier==1?"荒野墓碑":"远岭墓碑"):GardenDefenseState.Names[unit.kind];
        public void SetAnimation(Animator animator,AnimationClip walkClip,AnimationClip sadClip)
        {
            if(!animator||!walkClip||!sadClip)return;
            animator.applyRootMotion=false;graph=PlayableGraph.Create("Supplied zombie walk and dismay");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var output=AnimationPlayableOutput.Create(graph,"Zombie",animator);mixer=AnimationMixerPlayable.Create(graph,2);walk=AnimationClipPlayable.Create(graph,walkClip);sad=AnimationClipPlayable.Create(graph,sadClip);
            walk.SetApplyFootIK(false);sad.SetApplyFootIK(false);graph.Connect(walk,0,mixer,0);graph.Connect(sad,0,mixer,1);mixer.SetInputWeight(0,1);output.SetSourcePlayable(mixer);graph.Play();
        }
        public void Animate(float delta,bool moving)
        {
            if(!graph.IsValid())return;
            if(walk.GetTime()>=walk.GetAnimationClip().length)walk.SetTime(walk.GetTime()%walk.GetAnimationClip().length);
            if(sad.GetTime()>=sad.GetAnimationClip().length)sad.SetTime(sad.GetTime()%sad.GetAnimationClip().length);
            float weight=Mathf.MoveTowards(mixer.GetInputWeight(0),moving?1:0,delta*5);mixer.SetInputWeight(0,weight);mixer.SetInputWeight(1,1-weight);graph.Evaluate(delta);
        }
        public void HitFlash(){if(surfaces==null)surfaces=GetComponentsInChildren<Renderer>();if(tint==null)tint=new MaterialPropertyBlock();flashUntil=Time.time+.14f;}
        public void Fall()
        {dying=true;fallTime=0;fallRotation=transform.rotation;foreach(var c in GetComponentsInChildren<Collider>())c.enabled=false;}
        void LateUpdate()
        {
            if(dying){if(owner&&owner.Game.SimulationPaused)return;fallTime+=Time.deltaTime;transform.rotation=fallRotation*Quaternion.Euler(Mathf.SmoothStep(0,82,fallTime/.5f),0,0);if(fallTime>.65f)Destroy(gameObject);}
            if(surfaces==null)return;
            bool flashing=Time.time<flashUntil;foreach(var r in surfaces)if(r){if(flashing){r.GetPropertyBlock(tint);tint.SetColor("_BaseColor",new Color(2.2f,.65f,.5f,1));r.SetPropertyBlock(tint);}else r.SetPropertyBlock(null);}
            if(!flashing)surfaces=null;
        }
        void OnDestroy(){if(graph.IsValid())graph.Destroy();}
    }
}
