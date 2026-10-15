using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
namespace StarfieldEra.Editor
{
    public static class LocalGameModelsSetup
    {
        const string Folder="Assets/StarfieldEra/Environment/LocalModels";
        [MenuItem("StarfieldEra/Set up local hero, rocks, chests and boss")]
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play before importing models.");
            foreach(string name in new[]{"FieldRock","SupplyChest","Hero","WildBarbarian"})
            {
                bool character=name=="Hero"||name=="WildBarbarian";string path=Folder+"/"+name+".fbx";
                var importer=(ModelImporter)AssetImporter.GetAtPath(path);importer.importAnimation=character;importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.isReadable=false;importer.meshCompression=ModelImporterMeshCompression.Low;
                if(character)
                {
                    importer.animationType=ModelImporterAnimationType.Generic;
                    var clips=importer.defaultClipAnimations;foreach(var clip in clips){clip.name=clip.name.Contains("Walk")?"Walk":"Idle";clip.loopTime=clip.loopPose=true;clip.lockRootRotation=clip.lockRootHeightY=clip.lockRootPositionXZ=true;}importer.clipAnimations=clips;
                }
                importer.SaveAndReimport();
                string texturePath=Folder+"/"+name+"_BaseColor.png";var tex=(TextureImporter)AssetImporter.GetAtPath(texturePath);tex.maxTextureSize=2048;tex.mipmapEnabled=true;tex.sRGBTexture=true;tex.textureCompression=TextureImporterCompression.Compressed;tex.SaveAndReimport();
                string matPath=Folder+"/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);if(!mat){mat=new Material(Shader.Find("StarfieldEra/Storybook Models"));AssetDatabase.CreateAsset(mat,matPath);}mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));mat.SetColor("_BaseColor",Color.white);mat.enableInstancing=true;EditorUtility.SetDirty(mat);
                var root=new GameObject(name);
                try
                {
                    var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),root.transform);foreach(var r in model.GetComponentsInChildren<Renderer>())r.sharedMaterial=mat;
                    if(character)
                    {
                        var clips=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();var idle=clips.First(c=>c.name=="Idle");var walk=clips.First(c=>c.name=="Walk");string controllerPath=Folder+"/"+name+".controller";var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);if(!controller)controller=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
                        controller.parameters=new[]{new AnimatorControllerParameter{name="Speed",type=AnimatorControllerParameterType.Float}};var machine=controller.layers[0].stateMachine;foreach(var state in machine.states)machine.RemoveState(state.state);var idleState=machine.AddState("Idle");idleState.motion=idle;var walkState=machine.AddState("Walk");walkState.motion=walk;machine.defaultState=idleState;
                        var start=idleState.AddTransition(walkState);start.hasExitTime=false;start.duration=.15f;start.AddCondition(AnimatorConditionMode.Greater,.15f,"Speed");var stop=walkState.AddTransition(idleState);stop.hasExitTime=false;stop.duration=.15f;stop.AddCondition(AnimatorConditionMode.Less,.15f,"Speed");
                        var animator=model.GetComponent<Animator>();if(!animator)animator=model.AddComponent<Animator>();animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
                    }
                    var renderers=model.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);float scale=1/Mathf.Max(.001f,bounds.size.y);model.transform.localScale*=scale;model.transform.position+=new Vector3(-bounds.center.x,-bounds.min.y,-bounds.center.z)*scale;
                    if(character)root.AddComponent<LocalCharacterMotion>();else if(name=="SupplyChest")root.AddComponent<LocalChestVisual>();
                    PrefabUtility.SaveAsPrefabAsset(root,"Assets/StarfieldEra/Resources/Environment/"+name+".prefab");Debug.Log(name+" ready, bounds "+bounds.size);
                }
                finally{UnityEngine.Object.DestroyImmediate(root);}
            }
            AssetDatabase.SaveAssets();
        }
    }
}
