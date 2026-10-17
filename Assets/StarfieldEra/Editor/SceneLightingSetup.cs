using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
namespace StarfieldEra.Editor
{
    public static class SceneLightingSetup
    {
        [MenuItem("StarfieldEra/Configure grounded storybook lighting")]
        public static void Configure()
        {
            if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Stop Play before configuring renderer assets.");
            var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
            pipeline.supportsHDR=true;pipeline.shadowDistance=110;pipeline.shadowCascadeCount=4;pipeline.cascade4Split=new Vector3(.12f,.30f,.58f);pipeline.cascadeBorder=.15f;
            var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/PC_Renderer.asset");
            var ao=renderer.rendererFeatures.OfType<ScreenSpaceAmbientOcclusion>().FirstOrDefault();
            if(!ao){ao=ScriptableObject.CreateInstance<ScreenSpaceAmbientOcclusion>();ao.name="Storybook ambient occlusion";AssetDatabase.AddObjectToAsset(ao,renderer);renderer.rendererFeatures.Add(ao);}
            var settings=new SerializedObject(ao);var p=settings.FindProperty("m_Settings");
            p.FindPropertyRelative("Intensity").floatValue=1.05f;p.FindPropertyRelative("Radius").floatValue=.38f;p.FindPropertyRelative("DirectLightingStrength").floatValue=.25f;
            p.FindPropertyRelative("Downsample").boolValue=true;p.FindPropertyRelative("AfterOpaque").boolValue=false;p.FindPropertyRelative("Source").enumValueIndex=1;
            p.FindPropertyRelative("Samples").enumValueIndex=1;p.FindPropertyRelative("BlurQuality").enumValueIndex=0;p.FindPropertyRelative("Falloff").floatValue=110;
            settings.ApplyModifiedPropertiesWithoutUndo();ao.SetActive(true);ao.Create();renderer.SetDirty();EditorUtility.SetDirty(ao);EditorUtility.SetDirty(renderer);EditorUtility.SetDirty(pipeline);AssetDatabase.SaveAssets();
        }
    }
}
