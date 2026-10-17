using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
namespace StarfieldEra.Editor
{
    /// <summary>Import the Blender-authored rig/actions without changing the boss or world anchors.</summary>
    public static class StarfarerHeroSetup
    {
        const string Folder="Assets/StarfieldEra/Environment/LocalModels/Starfarer";
        [MenuItem("StarfieldEra/Set up new Starfarer hero")]
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play before importing the hero.");
            string path=Folder+"/Starfarer.fbx";
            var importer=(ModelImporter)AssetImporter.GetAtPath(path);
            importer.importAnimation=true;importer.animationType=ModelImporterAnimationType.Generic;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
            importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.isReadable=false;importer.meshCompression=ModelImporterMeshCompression.Off;
            importer.animationCompression=ModelImporterAnimationCompression.Off;importer.optimizeGameObjects=false;
            var clips=importer.defaultClipAnimations;
            foreach(var c in clips)
            {
                c.name=c.name.Contains("Run")?"Run":c.name.Contains("Walk")?"Walk":"Idle";
                c.loopTime=true;c.loopPose=false;c.lockRootRotation=c.lockRootHeightY=c.lockRootPositionXZ=true;
                c.keepOriginalOrientation=c.keepOriginalPositionY=c.keepOriginalPositionXZ=true;
            }
            importer.clipAnimations=clips;importer.SaveAndReimport();
            var texture=(TextureImporter)AssetImporter.GetAtPath(Folder+"/Starfarer_BaseColor.png");texture.maxTextureSize=2048;texture.sRGBTexture=true;texture.mipmapEnabled=true;texture.textureCompression=TextureImporterCompression.Compressed;texture.SaveAndReimport();
            string materialPath=Folder+"/Starfarer.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(!material){material=new Material(Shader.Find("StarfieldEra/Storybook Models"));AssetDatabase.CreateAsset(material,materialPath);}
            material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/Starfarer_BaseColor.png"));material.SetColor("_BaseColor",Color.white);material.enableInstancing=true;EditorUtility.SetDirty(material);
            var imported=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
            var idle=imported.First(c=>c.name=="Idle");var walk=imported.First(c=>c.name=="Walk");var run=imported.First(c=>c.name=="Run");
            string controllerPath=Folder+"/Starfarer.controller";var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if(!controller)controller=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            controller.parameters=new[]{new AnimatorControllerParameter{name="Speed",type=AnimatorControllerParameterType.Float},new AnimatorControllerParameter{name="MotionRate",type=AnimatorControllerParameterType.Float,defaultFloat=1}};
            var machine=controller.layers[0].stateMachine;foreach(var state in machine.states)machine.RemoveState(state.state);
            var stand=machine.AddState("Idle");stand.motion=idle;machine.defaultState=stand;
            var movement=machine.AddState("Locomotion");movement.speedParameter="MotionRate";movement.speedParameterActive=true;
            var tree=new BlendTree{name="Walk and Run",blendType=BlendTreeType.Simple1D,blendParameter="Speed",useAutomaticThresholds=false};AssetDatabase.AddObjectToAsset(tree,controller);tree.AddChild(walk,1.4f);tree.AddChild(run,3.8f);movement.motion=tree;
            var go=stand.AddTransition(movement);go.hasExitTime=false;go.duration=.14f;go.AddCondition(AnimatorConditionMode.Greater,.12f,"Speed");
            var stop=movement.AddTransition(stand);stop.hasExitTime=false;stop.duration=.18f;stop.AddCondition(AnimatorConditionMode.Less,.12f,"Speed");
            var root=new GameObject("Hero");
            try
            {
                var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),root.transform);
                foreach(var r in model.GetComponentsInChildren<Renderer>()){r.sharedMaterial=material;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;r.receiveShadows=true;}
                var animator=model.GetComponent<Animator>();if(!animator)animator=model.AddComponent<Animator>();animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
                // Bind-pose height is 1.8 m; the existing spawn helper sets final gameplay height.
                model.transform.localScale=Vector3.one/1.8f;root.AddComponent<LocalCharacterMotion>();root.AddComponent<HeroFootContact>();
                PrefabUtility.SaveAsPrefabAsset(root,"Assets/StarfieldEra/Resources/Environment/Hero.prefab");
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
            // Keep the legacy source intact for old scenes; the resource now references Starfarer.
            var legacy=(ModelImporter)AssetImporter.GetAtPath("Assets/StarfieldEra/Environment/LocalModels/Hero.fbx");
            if(legacy && !legacy.importAnimation){legacy.importAnimation=true;legacy.SaveAndReimport();}
            AssetDatabase.SaveAssets();Debug.Log("Starfarer hero ready: Blender Idle / Walk / Run, 22-bone generic rig.");
        }
    }
}
