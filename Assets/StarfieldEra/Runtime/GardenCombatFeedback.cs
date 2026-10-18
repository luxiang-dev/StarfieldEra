using System.Collections.Generic;
using UnityEngine;
namespace StarfieldEra
{
    /// <summary>Small reusable bursts and synthesized positional cues, without per-hit materials/audio objects.</summary>
    public sealed class GardenCombatFeedback:MonoBehaviour
    {
        sealed class Burst {public Transform root;public Transform[] bits;public float age;public Vector3 origin;}
        readonly List<Burst> active=new List<Burst>();readonly Stack<Burst> pool=new Stack<Burst>();
        Material hitMaterial,dustMaterial;AudioClip hit,alert,crumble;readonly AudioSource[] sources=new AudioSource[4];int next;float nextSound;
        public void Initialize(FarmGame game)
        {
            hitMaterial=game.World.Mat("Garden impact sparks","C9DE75");dustMaterial=game.World.Mat("Garden falling dust","A89472");
            hit=Tone("Garden impact",.11f,170,.6f);alert=Tone("Garden alert",.19f,620,.08f);crumble=Tone("Garden crumble",.3f,90,.85f);
            for(int i=0;i<sources.Length;i++){var root=new GameObject("Garden positional sound");root.transform.SetParent(transform,false);var a=root.AddComponent<AudioSource>();a.playOnAwake=false;a.spatialBlend=1;a.rolloffMode=AudioRolloffMode.Linear;a.minDistance=2;a.maxDistance=24;sources[i]=a;}
        }
        static AudioClip Tone(string name,float duration,float frequency,float noise)
        {
            var samples=new float[(int)(22050*duration)];uint seed=8731;
            for(int i=0;i<samples.Length;i++){seed=seed*1664525+1013904223;float t=i/22050f,envelope=Mathf.Sin(Mathf.PI*i/samples.Length)*Mathf.Exp(-t*12);samples[i]=(Mathf.Sin(t*frequency*6.28318f)*(.7f-noise*.4f)+((seed>>8)/(float)0xffffff*2-1)*noise)*envelope*.35f;}
            var clip=AudioClip.Create(name,samples.Length,1,22050,false);clip.SetData(samples,0);return clip;
        }
        public void Sound(Vector3 position,bool warning=false,bool falling=false)
        {if(Time.time<nextSound)return;nextSound=Time.time+.065f;var source=sources[next++%sources.Length];source.transform.position=position;source.PlayOneShot(warning?alert:falling?crumble:hit,warning?.65f:.8f);}
        public void Emit(Vector3 point,bool falling=false)
        {
            if(active.Count>=16)return;Burst burst;
            if(pool.Count>0)burst=pool.Pop();else
            {burst=new Burst{root=new GameObject("Pooled garden burst").transform,bits=new Transform[6]};burst.root.SetParent(transform,false);for(int i=0;i<6;i++){var bit=GameObject.CreatePrimitive(PrimitiveType.Cube);bit.transform.SetParent(burst.root,false);var col=bit.GetComponent<Collider>();col.enabled=false;Destroy(col);bit.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;burst.bits[i]=bit.transform;}}
            burst.age=0;burst.origin=point;burst.root.position=point;burst.root.gameObject.SetActive(true);
            foreach(var bit in burst.bits)bit.GetComponent<Renderer>().sharedMaterial=falling?dustMaterial:hitMaterial;active.Add(burst);Sound(point,false,falling);
        }
        void Update()
        {
            var game=GetComponentInParent<FarmGame>();if(game&&game.SimulationPaused)return;
            for(int n=active.Count-1;n>=0;n--){var b=active[n];b.age+=Time.deltaTime;for(int i=0;i<b.bits.Length;i++){float a=i*2.39996f;var p=new Vector3(Mathf.Cos(a),.8f,Mathf.Sin(a))*b.age*2;p.y-=b.age*b.age*3;b.bits[i].localPosition=p;b.bits[i].localScale=Vector3.one*Mathf.Max(0,.11f*(1-b.age/.55f));b.bits[i].Rotate(80*Time.deltaTime,130*Time.deltaTime,0);}if(b.age>=.55f){b.root.gameObject.SetActive(false);pool.Push(b);active.RemoveAt(n);}}
        }
        void OnDestroy(){if(hitMaterial)Destroy(hitMaterial);if(dustMaterial)Destroy(dustMaterial);if(hit)Destroy(hit);if(alert)Destroy(alert);if(crumble)Destroy(crumble);}
    }
}
