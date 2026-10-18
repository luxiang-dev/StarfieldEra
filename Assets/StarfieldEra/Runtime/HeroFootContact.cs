using UnityEngine;
namespace StarfieldEra
{
    /// <summary>Small terrain corrections on top of Blender animation; never replaces its body pose.</summary>
    [DefaultExecutionOrder(40)]
    public sealed class HeroFootContact:MonoBehaviour
    {
        sealed class Leg {public Transform upper,knee,foot;public float sole;public bool planted;public Vector3 point;}
        FarmGame game;Transform actor;Animator animator;Leg left,right;Vector3 previous,baseLocal;float speed;ParticleSystem dust;Material dustMaterial;
        Transform Bone(string name){foreach(var t in GetComponentsInChildren<Transform>())if(t.name==name)return t;return null;}
        void Start()
        {
            game=GetComponentInParent<FarmGame>();actor=transform.parent;animator=GetComponentInChildren<Animator>();
            if(!game||!actor||!animator){enabled=false;return;}
            left=MakeLeg("L");right=MakeLeg("R");if(left==null||right==null){enabled=false;return;}
            baseLocal=transform.localPosition;previous=actor.position;CreateDust();
        }
        Leg MakeLeg(string side)
        {
            var a=Bone("UpperLeg."+side);var b=Bone("LowerLeg."+side);var c=Bone("Foot."+side);if(!a||!b||!c)return null;
            // The Blender export keeps ankle height at 7.5% of standing height.
            return new Leg{upper=a,knee=b,foot=c,sole=.075f*1.78f};
        }
        Vector3 Ground(Vector3 p)
        {
            if(game.InCave)return new Vector3(p.x,CaveGame.Floor+.008f,p.z);
            var grid=game.World.HexGrid;
            return grid.RaycastGround(new Ray(p+Vector3.up*2,Vector3.down),out var hit)?hit+Vector3.up*.008f:grid.SurfacePoint(p,.008f);
        }
        void LateUpdate()
        {
            if(!animator||Time.deltaTime<=0)return;
            if(!game.IsGrounded){left.planted=right.planted=false;transform.localPosition=baseLocal;previous=actor.position;return;}
            var delta=actor.position-previous;delta.y=0;previous=actor.position;bool teleport=delta.sqrMagnitude>4;
            speed=teleport?0:delta.magnitude/Time.deltaTime;
            // CharacterController skin width must not lift the rendered soles off the ground.
            var root=actor.TransformPoint(baseLocal);root.y+=Mathf.Clamp(Ground(actor.position).y-actor.position.y,-.15f,.15f);transform.position=root;
            var state=animator.GetCurrentAnimatorStateInfo(0);bool moving=state.IsName("Locomotion")&&speed>.12f;
            float phase=Mathf.Repeat(state.normalizedTime,1),stance=Mathf.Lerp(.57f,.40f,Mathf.InverseLerp(1.4f,3.8f,speed));
            Contact(left,moving&&phase<stance,moving,teleport);
            Contact(right,moving&&Mathf.Repeat(phase+.5f,1)<stance,moving,teleport);
        }
        void Contact(Leg leg,bool stance,bool moving,bool teleport)
        {
            var animated=leg.foot.position;var sole=animated-Vector3.up*leg.sole;
            if(teleport)leg.planted=false;
            bool landing=stance&&!leg.planted;
            if(landing){leg.point=Ground(sole);if(speed>.3f)EmitStep(leg.point);}
            leg.planted=stance;
            var ground=Ground(sole);float lift=Mathf.Clamp(ground.y-sole.y,-.15f,.15f);
            if(moving&&!stance&&lift<0)return; // Preserve the authored swing clearance.
            var target=animated+Vector3.up*lift;
            if(stance)
            {
                var offset=leg.point-sole;offset.y=0;target+=Vector3.ClampMagnitude(offset,.09f);
            }
            var a=leg.upper.position;var b=leg.knee.position;var c=leg.foot.position;var footRotation=leg.foot.rotation;
            float l1=(b-a).magnitude,l2=(c-b).magnitude;var d=target-a;float distance=Mathf.Clamp(d.magnitude,.01f,l1+l2-.001f);var direction=d.normalized;
            var bend=Vector3.ProjectOnPlane(actor.forward,direction).normalized;if(bend.sqrMagnitude<.001f)return;
            float along=(l1*l1+distance*distance-l2*l2)/(2*distance);var knee=a+direction*along+bend*Mathf.Sqrt(Mathf.Max(0,l1*l1-along*along));
            leg.upper.rotation=Quaternion.FromToRotation(b-a,knee-a)*leg.upper.rotation;
            leg.knee.rotation=Quaternion.FromToRotation(leg.foot.position-leg.knee.position,target-leg.knee.position)*leg.knee.rotation;
            var grid=game.World.HexGrid;float e=.10f;var normal=new Vector3(grid.GroundHeight(sole-Vector3.right*e)-grid.GroundHeight(sole+Vector3.right*e),2*e,grid.GroundHeight(sole-Vector3.forward*e)-grid.GroundHeight(sole+Vector3.forward*e)).normalized;
            leg.foot.rotation=Quaternion.FromToRotation(Vector3.up,game.InCave?Vector3.up:Vector3.Slerp(Vector3.up,normal,.65f))*footRotation;
        }
        void CreateDust()
        {
            var shader=Resources.Load<Shader>("FootstepDust");if(!shader)return;
            var root=new GameObject("Footstep ground puffs");root.transform.SetParent(transform,false);dust=root.AddComponent<ParticleSystem>();dust.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=dust.main;main.loop=false;main.playOnAwake=false;main.simulationSpace=ParticleSystemSimulationSpace.World;main.maxParticles=32;main.startLifetime=new ParticleSystem.MinMaxCurve(.22f,.38f);main.startSize=new ParticleSystem.MinMaxCurve(.045f,.10f);main.startSpeed=.08f;main.gravityModifier=-.02f;
            var emission=dust.emission;emission.enabled=false;var shape=dust.shape;shape.enabled=false;
            var color=dust.colorOverLifetime;color.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(.6f,0),new GradientAlphaKey(0,1)});color.color=gradient;
            var size=dust.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,.5f,1,1.5f));
            var renderer=root.GetComponent<ParticleSystemRenderer>();dustMaterial=new Material(shader);renderer.sharedMaterial=dustMaterial;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;dust.Play();
        }
        void EmitStep(Vector3 point)
        {
            if(!dust)return;var terrain=game.World.HexGrid.TerrainAt(point);bool wet=game.State.Rainy||terrain==TerrainKind.Marsh;
            var color=wet?new Color(.45f,.61f,.54f,.20f):terrain==TerrainKind.Sand?new Color(.66f,.52f,.30f,.25f):new Color(.48f,.55f,.28f,.18f);
            for(int i=0;i<3;i++)dust.Emit(new ParticleSystem.EmitParams{position=point+Vector3.up*.025f+Random.insideUnitSphere*.025f,velocity=new Vector3(Random.Range(-.08f,.08f),Random.Range(.04f,.12f),Random.Range(-.08f,.08f)),startColor=color},1);
        }
        void OnDestroy(){if(dustMaterial)Destroy(dustMaterial);}
    }
}
