using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace StarfieldEra.Editor
{
    public static class StoreArtSetup
    {
        const string Local = "Assets/StarfieldEra/LocalStoreArt";
        const string Characters = "Assets/ithappy/Creative_Characters_FREE/";
        static GameObject Prefab(string suffix) => AssetDatabase.GetAllAssetPaths()
            .Where(p => p.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            .Select(p => AssetDatabase.LoadAssetAtPath<GameObject>(p)).FirstOrDefault(p => p);
        static AnimationClip Clip(string suffix) => AssetDatabase.GetAllAssetPaths()
            .Where(p => p.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            .SelectMany(AssetDatabase.LoadAllAssetsAtPath).OfType<AnimationClip>()
            .FirstOrDefault(c => !c.name.StartsWith("__preview__"));

        [MenuItem("StarfieldEra/Connect claimed store art")]
        public static void Connect()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop play before connecting art.");
            if (!AssetDatabase.IsValidFolder(Local)) AssetDatabase.CreateFolder("Assets/StarfieldEra", "LocalStoreArt");
            if (!AssetDatabase.IsValidFolder("Assets/StarfieldEra/Resources")) AssetDatabase.CreateFolder("Assets/StarfieldEra", "Resources");
            var catalog = AssetDatabase.LoadAssetAtPath<StoreVisuals>("Assets/StarfieldEra/Resources/StoreVisuals.asset");
            if (!catalog) { catalog = ScriptableObject.CreateInstance<StoreVisuals>(); AssetDatabase.CreateAsset(catalog, "Assets/StarfieldEra/Resources/StoreVisuals.asset"); }
            catalog.farmer = Character("Farmer", false);
            catalog.villager = Character("Elder", true);
            catalog.tree = Prefab("PT_Pine_Tree_03_green.prefab");
            catalog.rock = Prefab("PT_Generic_Rock_01.prefab");
            catalog.quarry = Prefab("Stone_Mine.prefab");
            catalog.grass = Prefab("PT_Grass_02.prefab");
            catalog.flower = Prefab("PT_Poppy_02.prefab");
            catalog.shrub = Prefab("PT_Generic_Shrub_01.prefab");
            catalog.ore = Prefab("PT_Ore_Rock_01.prefab");
            catalog.fallenLog = Prefab("PT_Pine_Tree_03_logs.prefab");
            catalog.market = Prefab("Fish_Market_lvl3.prefab");
            catalog.workshop = Prefab("Lumber_Mill.prefab");
            catalog.library = Prefab("Stone_Tower.prefab");
            catalog.guardian = Prefab("StarFishPolyart.prefab");
            // Two purchased monster designs are used as temporary skins, without changing species/save IDs.
            catalog.companions = new[] { Prefab("BlueSlimePolyart.prefab"), Prefab("BlueSlimePolyart.prefab"), Prefab("StarFishPolyart.prefab") };
            catalog.idle = Clip("HumanM@Idle01.fbx");
            catalog.walk = Clip("HumanM@Walk01_Forward.fbx");
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log("Store art connected: " + (catalog.tree ? "trees " : "TREE MISSING ") + (catalog.idle ? "idle " : "IDLE MISSING ") + (catalog.walk ? "walk" : "WALK MISSING"));
        }

        static GameObject Character(string name, bool elder)
        {
            var source = Prefab("/Base_Mesh.prefab");
            if (!source) return null;
            var root = UnityEngine.Object.Instantiate(source); root.name = name;
            foreach (var r in root.GetComponentsInChildren<SkinnedMeshRenderer>(true)) r.enabled = false;
            SetPart(root, "Body", "Body/Body_010.prefab");
            SetPart(root, "Faces", "Faces/Male_emotion_usual_001.prefab");
            SetPart(root, "Hairstyle", elder ? "Hairstyle/Hairstyle_Male_005.prefab" : "Hairstyle/Hairstyle_Male_001.prefab");
            SetPart(root, "Outerwear", elder ? "Outwear/Outwear_043.prefab" : "Outwear/Outwear_004.prefab");
            SetPart(root, "Pants", "Pants/Pants_009.prefab");
            SetPart(root, "Shoes", "Shoes/Shoe_Sneakers_009.prefab");
            SetPart(root, "Hat", "Hat/Hat_010.prefab");
            var animator = root.GetComponentInChildren<Animator>();
            if (animator) animator.runtimeAnimatorController = null;
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(root);
            var saved = PrefabUtility.SaveAsPrefabAsset(root, Local + "/" + name + ".prefab");
            UnityEngine.Object.DestroyImmediate(root);
            return saved;
        }
        static void SetPart(GameObject root, string slot, string suffix)
        {
            var part = AssetDatabase.LoadAssetAtPath<GameObject>(Characters + "Prefabs/" + suffix);
            if (!part) return;
            var source = part.GetComponentInChildren<SkinnedMeshRenderer>(true);
            var target = root.GetComponentsInChildren<SkinnedMeshRenderer>(true).FirstOrDefault(r => r.name == slot);
            if (!source || !target) return;
            target.sharedMesh = source.sharedMesh; target.sharedMaterials = source.sharedMaterials;
            target.enabled = true;
        }
    }
}
