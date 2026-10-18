using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
namespace StarfieldEra
{
    public sealed class GardenActor:MonoBehaviour
    {
        public GardenUnit unit;public GardenZombie zombie;public GardenDefense owner;
        PlayableGraph graph;AnimationMixerPlayable mixer;AnimationClipPlayable walk,sad;
        public int Health=>unit!=null?unit.health:zombie!=null?zombie.health:0;
        public int MaxHealth=>unit!=null?unit.MaxHealth:60;
        public string DisplayName=>zombie!=null?"僵尸":unit.kind==0?"豌豆射手":unit.kind==1?"防御城墙":"僵尸墓碑";
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
        void OnDestroy(){if(graph.IsValid())graph.Destroy();}
    }
}
