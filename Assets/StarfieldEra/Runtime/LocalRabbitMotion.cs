using UnityEngine;
namespace StarfieldEra
{
    /// <summary>Subtle idle breathing and short ground-relative hops on the visual only.</summary>
    public sealed class LocalRabbitMotion:MonoBehaviour
    {
        Transform actor;Vector3 previous,restPosition,restScale;Quaternion restRotation;float blend,phase;
        void Start(){actor=transform.parent;previous=actor?actor.position:transform.position;restPosition=transform.localPosition;restRotation=transform.localRotation;restScale=transform.localScale;}
        void LateUpdate()
        {
            if(!actor||Time.deltaTime<=0)return;var delta=actor.position-previous;delta.y=0;previous=actor.position;
            float speed=delta.sqrMagnitude>4?0:delta.magnitude/Time.deltaTime;blend=Mathf.MoveTowards(blend,speed>.12f?1:0,Time.deltaTime*8);
            phase+=Time.deltaTime*Mathf.Lerp(0,17,blend);float lift=Mathf.Max(0,Mathf.Sin(phase))*.07f*blend;
            transform.localPosition=restPosition+Vector3.up*lift;
            transform.localRotation=restRotation*Quaternion.Euler(Mathf.Sin(phase)*4*blend,0,0);
            transform.localScale=Vector3.Scale(restScale,new Vector3(1,1+Mathf.Sin(Time.time*2.2f)*.004f*(1-blend),1));
        }
    }
}
