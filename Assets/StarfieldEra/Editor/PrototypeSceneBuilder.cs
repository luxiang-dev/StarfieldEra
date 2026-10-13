using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace StarfieldEra.Editor
{
    public static class PrototypeSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/FarmPrototype.unity";

        [MenuItem("StarfieldEra/Create Farm Prototype")]
        public static void Create()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play Mode before generating the scene.");
            // Preserve the user's existing scene before opening a separate prototype scene.
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                var open = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if (!open.isDirty) continue;
                if (string.IsNullOrEmpty(open.path))
                    throw new System.InvalidOperationException("Save the unnamed scene before creating the prototype.");
                EditorSceneManager.SaveScene(open);
            }
            Directory.CreateDirectory("Assets/StarfieldEra/Art");
            AssetDatabase.Refresh();
            string fontPath = "Assets/StarfieldEra/Art/FarmChinese.asset";
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
            if (font == null)
            {
                var source = AssetDatabase.LoadAssetAtPath<Font>("Assets/StarfieldEra/Art/FarmChinese.otf");
                if (source == null) throw new System.InvalidOperationException("The Chinese font source is missing.");
                font = TMP_FontAsset.CreateFontAsset(source, 48, 5, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic);
                font.name = "FarmChinese";
                string characters = File.ReadAllText("Assets/StarfieldEra/Art/Glyphs.txt");
                if (!font.TryAddCharacters(characters, out string missing))
                    throw new System.InvalidOperationException("Font is missing glyphs: " + missing);
                font.atlasPopulationMode = AtlasPopulationMode.Static;
                AssetDatabase.CreateAsset(font, fontPath);
                AssetDatabase.AddObjectToAsset(font.material, font);
                foreach (var texture in font.atlasTextures) AssetDatabase.AddObjectToAsset(texture, font);
                EditorUtility.SetDirty(font);
            }
            string materialPath = "Assets/StarfieldEra/Art/FarmSurface.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                material.name = "FarmSurface";
                AssetDatabase.CreateAsset(material, materialPath);
            }
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("StarfieldEra Farm Prototype");
            var game = root.AddComponent<FarmGame>();
            game.interfaceFont = font;
            game.worldMaterial = material;
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            PlayerSettings.productName = "StarfieldEra";
            AssetDatabase.SaveAssets();
            Debug.Log("Farm prototype scene created: " + ScenePath);
        }
    }
}
