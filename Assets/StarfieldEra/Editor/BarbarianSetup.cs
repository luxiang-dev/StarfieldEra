using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace StarfieldEra.Editor
{
    public static class BarbarianSetup
    {
        const string Folder = "Assets/StarfieldEra/Characters/Barbarian";
        [MenuItem("StarfieldEra/Set up barbarian player")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop play before setting up character assets.");
            var importer = (ModelImporter)AssetImporter.GetAtPath(Folder + "/Barbarian.fbx");
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.importAnimation = true;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            var clips = importer.defaultClipAnimations;
            foreach (var clip in clips)
            {
                clip.name = clip.name.Contains("Walk") ? "Walk" : "Idle";
                clip.loopTime = true;
                clip.loopPose = true;
                clip.lockRootRotation = true;
                clip.lockRootHeightY = true;
                clip.lockRootPositionXZ = true;
            }
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
            var animations = AssetDatabase.LoadAllAssetsAtPath(Folder + "/Barbarian.fbx").OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")).ToArray();
            var idle = animations.First(c => c.name == "Idle");
            var walk = animations.First(c => c.name == "Walk");
            var material = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/Barbarian.mat");
            if (!material)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, Folder + "/Barbarian.mat");
            }
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/Barbarian_BaseColor.png"));
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Smoothness", .22f);
            EditorUtility.SetDirty(material);
            string controllerPath = Folder + "/Barbarian.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (!controller) controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            controller.parameters = new[] { new AnimatorControllerParameter { name = "Speed", type = AnimatorControllerParameterType.Float } };
            var machine = controller.layers[0].stateMachine;
            foreach (var state in machine.states) machine.RemoveState(state.state);
            var idleState = machine.AddState("Idle"); idleState.motion = idle;
            var walkState = machine.AddState("Walk"); walkState.motion = walk;
            machine.defaultState = idleState;
            var start = idleState.AddTransition(walkState); start.hasExitTime = false; start.duration = .15f;
            start.AddCondition(AnimatorConditionMode.Greater, .15f, "Speed");
            var stop = walkState.AddTransition(idleState); stop.hasExitTime = false; stop.duration = .18f;
            stop.AddCondition(AnimatorConditionMode.Less, .15f, "Speed");
            var root = new GameObject("BarbarianPlayer");
            try
            {
                var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/Barbarian.fbx"), root.transform);
                foreach (var renderer in model.GetComponentsInChildren<Renderer>()) renderer.sharedMaterial = material;
                var animator = model.GetComponent<Animator>();
                if (!animator) animator = model.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                root.AddComponent<BarbarianLocomotion>();
                var renderers = model.GetComponentsInChildren<Renderer>();
                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                float scale = 1.85f / bounds.size.y;
                model.transform.localScale *= scale;
                model.transform.localPosition = new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z) * scale;
                if (!AssetDatabase.IsValidFolder("Assets/StarfieldEra/Resources")) AssetDatabase.CreateFolder("Assets/StarfieldEra", "Resources");
                PrefabUtility.SaveAsPrefabAsset(root, "Assets/StarfieldEra/Resources/BarbarianPlayer.prefab");
                AssetDatabase.SaveAssets();
                Debug.Log($"Barbarian ready: {animations.Length} clips, {bounds.size}, height 1.85m");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
    }
}
