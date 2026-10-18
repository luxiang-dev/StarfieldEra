using UnityEditor;
using UnityEngine;
namespace StarfieldEra.Editor
{
    public static class AnimalArtSetup
    {
        [MenuItem("StarfieldEra/Connect claimed animal art")]
        public static void Connect()
        {
            if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Stop play before connecting animals.");
            var catalog=AssetDatabase.LoadAssetAtPath<StoreVisuals>("Assets/StarfieldEra/Resources/StoreVisuals.asset");
            if(!catalog){catalog=ScriptableObject.CreateInstance<StoreVisuals>();AssetDatabase.CreateAsset(catalog,"Assets/StarfieldEra/Resources/StoreVisuals.asset");}
            // The body-only URP variant suits a large map and avoids expensive layered fur rendering.
            catalog.boar=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Wild Boar/Prefabs/URP/Mobile/Wild_Boar.prefab");
            EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();Debug.Log(catalog.boar?"Claimed wild boar connected with Idle/Walking animations.":"Wild boar prefab missing; import its URP variant first.");
        }
    }
}
