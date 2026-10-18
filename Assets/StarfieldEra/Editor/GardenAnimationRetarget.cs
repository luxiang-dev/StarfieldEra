using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace StarfieldEra.Editor
{
    // FBX round-trips change bone axes. Bake world-space motion deltas into each new bind pose.
    public static class GardenAnimationRetarget
    {
        sealed class Bone
        {
            public string path;public Transform source,target;public Vector3 sourcePosition,targetPosition,position,scale;public Quaternion sourceRotation,targetRotation,rotation;
            public readonly List<Keyframe>[] curves=Enumerable.Range(0,7).Select(_=>new List<Keyframe>()).ToArray();
        }
        static int Depth(Transform t){int depth=0;while(t.parent){depth++;t=t.parent;}return depth;}
        public static AnimationClip Bake(GameObject sourcePrefab,GameObject targetPrefab,AnimationClip sourceClip,string path)
        {
            var source=UnityEngine.Object.Instantiate(sourcePrefab);var target=UnityEngine.Object.Instantiate(targetPrefab);
            try
            {
                var src=source.transform.GetChild(0);var dst=target.transform.GetChild(0);
                // Imported files may carry a posed default frame; use the true skin bind pose.
                foreach(var renderer in source.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    var bind=renderer.sharedMesh.bindposes;
                    foreach(int i in Enumerable.Range(0,renderer.bones.Length).OrderBy(i=>Depth(renderer.bones[i])))
                    {var matrix=renderer.localToWorldMatrix*bind[i].inverse;renderer.bones[i].SetPositionAndRotation(matrix.GetColumn(3),matrix.rotation);}
                }
                var paths=AnimationUtility.GetCurveBindings(sourceClip).Select(b=>b.path).Distinct().OrderBy(p=>p.Count(c=>c=='/')).ToArray();
                var bones=paths.Select(p=>
                {
                    var a=src.Find(p);var b=dst.Find(p);if(!a||!b)throw new InvalidOperationException("Missing animation bone "+p);
                    return new Bone{path=p,source=a,target=b,sourcePosition=a.position,targetPosition=b.position,sourceRotation=a.rotation,targetRotation=b.rotation,position=b.localPosition,rotation=b.localRotation,scale=b.localScale};
                }).ToArray();
                float ratio=dst.lossyScale.x/src.lossyScale.x;
                int frames=Mathf.CeilToInt(sourceClip.length*30);
                for(int frame=0;frame<=frames;frame++)
                {
                    float time=Mathf.Min(sourceClip.length,frame/30f);sourceClip.SampleAnimation(src.gameObject,time);
                    foreach(var b in bones){b.target.localPosition=b.position;b.target.localRotation=b.rotation;b.target.localScale=b.scale;}
                    foreach(var b in bones)
                    {
                        b.target.rotation=b.source.rotation*Quaternion.Inverse(b.sourceRotation)*b.targetRotation;
                        if(b.path=="Armature"||b.path=="Armature/root"||b.target.name=="Hips")b.target.position=b.targetPosition+(b.source.position-b.sourcePosition)*ratio;
                        var p=b.target.localPosition;var q=b.target.localRotation;float[] values={p.x,p.y,p.z,q.x,q.y,q.z,q.w};
                        for(int i=0;i<7;i++)b.curves[i].Add(new Keyframe(time,values[i]));
                    }
                }
                var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);if(!clip){clip=new AnimationClip();AssetDatabase.CreateAsset(clip,path);}clip.ClearCurves();clip.frameRate=30;
                string[] properties={"m_LocalPosition.x","m_LocalPosition.y","m_LocalPosition.z","m_LocalRotation.x","m_LocalRotation.y","m_LocalRotation.z","m_LocalRotation.w"};
                foreach(var b in bones)for(int i=0;i<7;i++)AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(b.path,typeof(Transform),properties[i]),new AnimationCurve(b.curves[i].ToArray()));
                clip.EnsureQuaternionContinuity();var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=true;AnimationUtility.SetAnimationClipSettings(clip,settings);EditorUtility.SetDirty(clip);return clip;
            }
            finally{UnityEngine.Object.DestroyImmediate(source);UnityEngine.Object.DestroyImmediate(target);}
        }
    }
}
