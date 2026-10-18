using System.Collections.Generic;
using UnityEngine;
namespace StarfieldEra
{
    /// <summary>Ground-relative quadruped gait on the wolf's authored bones.</summary>
    public sealed class NightWolfMotion:MonoBehaviour
    {
        readonly List<Transform> legs=new List<Transform>(),paws=new List<Transform>();
        readonly List<Quaternion> legRest=new List<Quaternion>(),pawRest=new List<Quaternion>();
        Transform actor,body,head,tail;Vector3 previous,restPosition;Quaternion headRest,tailRest;float phase,blend;
        void Start()
        {
            actor=transform.parent;previous=actor?actor.position:transform.position;foreach(var node in GetComponentsInChildren<Transform>())if(node.name=="NightWolfRig")body=node;if(body)restPosition=body.localPosition;
            foreach(string name in new[]{"ForeLeg_L","ForeLeg_R","HindLeg_L","HindLeg_R"})
            {
                Transform leg=null,paw=null;foreach(var node in GetComponentsInChildren<Transform>()){if(node.name==name)leg=node;if(node.name==name.Replace("Leg","Paw"))paw=node;}
                if(leg&&paw){legs.Add(leg);legRest.Add(leg.localRotation);paws.Add(paw);pawRest.Add(paw.localRotation);}
            }
            foreach(var node in GetComponentsInChildren<Transform>()){if(node.name=="WolfHead"){head=node;headRest=node.localRotation;}if(node.name=="WolfTail"){tail=node;tailRest=node.localRotation;}}
        }
        void LateUpdate()
        {
            if(!actor||Time.deltaTime<=0)return;var movement=actor.position-previous;movement.y=0;previous=actor.position;
            float speed=movement.sqrMagnitude>4?0:movement.magnitude/Time.deltaTime;blend=Mathf.MoveTowards(blend,speed>.08f?1:0,Time.deltaTime*6);phase+=Time.deltaTime*Mathf.Lerp(1,8,blend);
            for(int i=0;i<legs.Count;i++)
            {
                float swing=Mathf.Sin(phase+(i==0||i==3?0:Mathf.PI));
                legs[i].localRotation=legRest[i]*Quaternion.Euler(18*swing*blend,0,0);
                paws[i].localRotation=pawRest[i]*Quaternion.Euler(-13*Mathf.Max(0,swing)*blend,0,0);
            }
            if(body)body.localPosition=restPosition+Vector3.up*(Mathf.Abs(Mathf.Sin(phase))*.015f*blend);
            if(head)head.localRotation=headRest*Quaternion.Euler(Mathf.Sin(phase)*2*blend,0,0);
            if(tail)tail.localRotation=tailRest*Quaternion.Euler(0,Mathf.Sin(Time.time*2)*5,0);
        }
    }
}
