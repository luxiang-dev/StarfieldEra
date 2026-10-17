using UnityEngine;
namespace StarfieldEra
{
    /// <summary>Use actual actor travel, keeping the controller authoritative and animation in place.</summary>
    public sealed class LocalCharacterMotion:MonoBehaviour
    {
        Animator animator;Vector3 previous;Transform actor;
        void Awake(){animator=GetComponentInChildren<Animator>();if(animator)animator.applyRootMotion=false;actor=transform.parent?transform.parent:transform;previous=actor.position;}
        void LateUpdate()
        {
            if(!animator||Time.deltaTime<=0)return;var delta=actor.position-previous;delta.y=0;previous=actor.position;
            float speed=delta.sqrMagnitude>4?0:delta.magnitude/Time.deltaTime;animator.SetFloat("Speed",speed,.10f,Time.deltaTime);
            if(GetComponent<HeroFootContact>())
            {
                float nominal=Mathf.Lerp(1.4f,3.8f,Mathf.InverseLerp(1.4f,3.8f,speed));
                animator.SetFloat("MotionRate",speed>.12f?Mathf.Clamp(speed/nominal,.18f,1.5f):1);animator.speed=1;
            }
            else animator.speed=speed>.15f?Mathf.Clamp(speed/2.5f,.75f,1.8f):1;
        }
        public static bool AttachHero(Transform player)
        {
            if(!Resources.Load<GameObject>("Environment/Hero"))return false;
            foreach(var r in player.GetComponentsInChildren<Renderer>())r.enabled=false;
            var hero=LocalEnvironment.Character("Hero",player,player.position,1.78f);return hero!=null;
        }
        public static bool AttachBoss(Transform anchor)
        {
            if(!LocalEnvironment.Character("WildBarbarian",anchor,anchor.position,2.6f))return false;
            var obstacle=anchor.gameObject.AddComponent<CapsuleCollider>();obstacle.height=2.6f;obstacle.radius=.55f;obstacle.center=Vector3.up*1.3f;
            var plate=anchor.GetComponentInChildren<WorldNameplate>();if(plate)plate.transform.localPosition=Vector3.up*3.1f;
            else {var label=anchor.GetComponentInChildren<TMPro.TMP_Text>();if(label)label.transform.localPosition=Vector3.up*3.1f;}
            return true;
        }
    }
}
