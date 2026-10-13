using System.Collections.Generic;
using UnityEngine;

namespace StarfieldEra
{
    public sealed class FarmWorld : MonoBehaviour
    {
        public Material materialTemplate;
        public TMPro.TMP_FontAsset labelFont;
        public AdventureWorld Adventure { get; private set; }
        public Transform Player { get; private set; }
        public CharacterController Controller { get; private set; }
        public Camera View { get; private set; }
        public readonly Vector3 Door = new Vector3(-5, 0, 3.4f);
        public readonly Vector3[] PlotPositions = new Vector3[FarmState.PlotCount];
        readonly Renderer[] soils = new Renderer[FarmState.PlotCount];
        readonly Transform[] crops = new Transform[FarmState.PlotCount];
        readonly List<Material> materials = new List<Material>();
        Mesh cone;
        Material dry, wet, leaf, ripe;
        Transform selection;

        public void Generate()
        {
            var grass = Mat("Meadow", "91AC69");
            var stone = Mat("Island edge", "526856");
            var sand = Mat("Warm stone", "CCBC8B");
            var wood = Mat("Timber", "715346");
            var cream = Mat("Plaster", "F1E2BC");
            var roof = Mat("Terracotta", "B56648");
            var green = Mat("Evergreen", "396952");
            var lightGreen = Mat("Canopy", "638D50");
            dry = Mat("Dry soil", "805643");
            wet = Mat("Watered soil", "483E36");
            leaf = Mat("Crop leaves", "77A34D");
            ripe = Mat("Golden turnip", "EAB84D");
            cone = CreateCone();

            Shape("Island", PrimitiveType.Cube, new Vector3(0, -.65f, 0), new Vector3(42, 1.2f, 38), stone, true);
            Shape("Meadow", PrimitiveType.Cube, new Vector3(0, -.08f, 0), new Vector3(41.7f, .18f, 37.7f), grass, true);
            Shape("Village lane", PrimitiveType.Cube, new Vector3(-2, .025f, 2.6f), new Vector3(14, .04f, 1.6f), sand);
            Shape("Farm lane", PrimitiveType.Cube, new Vector3(-2, .03f, -2), new Vector3(1.4f, .05f, 8), sand);

            // House doorway faces the farm; its collision never covers the interaction point.
            Shape("Farmhouse", PrimitiveType.Cube, new Vector3(-5, 1.3f, 5.3f), new Vector3(3.6f, 2.6f, 3), cream, true);
            var a = Shape("Roof west", PrimitiveType.Cube, new Vector3(-5.95f, 3, 5.3f), new Vector3(2.3f, .25f, 3.7f), roof);
            a.rotation = Quaternion.Euler(0, 0, 30);
            a = Shape("Roof east", PrimitiveType.Cube, new Vector3(-4.05f, 3, 5.3f), new Vector3(2.3f, .25f, 3.7f), roof);
            a.rotation = Quaternion.Euler(0, 0, -30);
            Shape("Door", PrimitiveType.Cube, new Vector3(-5, .75f, 3.77f), new Vector3(.85f, 1.5f, .08f), wood);
            Shape("Doorstep", PrimitiveType.Cube, new Vector3(-5, .08f, 3.1f), new Vector3(1.5f, .16f, .7f), sand);
            var blue = Mat("Window glass", "78A9AA");
            Shape("Window", PrimitiveType.Cube, new Vector3(-6.1f, 1.45f, 3.76f), new Vector3(.65f, .7f, .09f), blue);
            Shape("Window frame", PrimitiveType.Cube, new Vector3(-6.1f, 1.45f, 3.7f), new Vector3(.05f, .8f, .06f), cream);
            Shape("Chimney", PrimitiveType.Cube, new Vector3(-4.3f, 3.3f, 5.9f), new Vector3(.5f, 1.5f, .5f), wood);

            for (int i = 0; i < FarmState.PlotCount; i++)
            {
                Vector3 pos = new Vector3(.5f + (i % 4) * 1.85f, .075f, -3.3f + (i / 4) * 1.85f);
                PlotPositions[i] = pos;
                soils[i] = Shape("Plot " + (i + 1), PrimitiveType.Cube, pos, new Vector3(1.55f, .12f, 1.55f), dry).GetComponent<Renderer>();
                var cropRoot = new GameObject("Crop " + (i + 1)).transform;
                cropRoot.SetParent(transform);
                cropRoot.position = pos + Vector3.up * .1f;
                crops[i] = cropRoot;
                for (int j = 0; j < 3; j++)
                {
                    Vector3 offset = new Vector3((j - 1) * .42f, .16f, (j % 2 == 0 ? -.15f : .25f));
                    var bulb = Shape("Turnip", PrimitiveType.Sphere, cropRoot.position + offset, Vector3.one * .35f, ripe);
                    bulb.SetParent(cropRoot, true);
                    var sprout = Cone("Leaves", cropRoot.position + offset + Vector3.up * .13f, new Vector3(.35f, .55f, .35f), leaf);
                    sprout.SetParent(cropRoot, true);
                }
            }

            var markerMat = Mat("Selected plot", "F8DF94");
            selection = new GameObject("Interaction marker").transform;
            selection.SetParent(transform);
            for (int i = 0; i < 4; i++)
            {
                bool horizontal = i < 2;
                var border = Shape("Border", PrimitiveType.Cube,
                    horizontal ? new Vector3(0, .17f, i == 0 ? -.84f : .84f) : new Vector3(i == 2 ? -.84f : .84f, .17f, 0),
                    horizontal ? new Vector3(1.75f, .035f, .05f) : new Vector3(.05f, .035f, 1.75f), markerMat);
                border.SetParent(selection, false);
            }

            // Fixed seeds make the island reproducible without affecting gameplay random state.
            var random = new System.Random(37);
            for (int i = 0; i < 22; i++)
            {
                float x = i < 11 ? -9 + (float)random.NextDouble() * 1.5f : 8.5f + (float)random.NextDouble() * 1.4f;
                float z = -8.5f + (float)random.NextDouble() * 17;
                float height = 1.7f + (float)random.NextDouble() * 1.3f;
                Shape("Trunk", PrimitiveType.Cylinder, new Vector3(x, height / 3, z), new Vector3(.25f, height / 3, .25f), wood, true);
                Cone("Pine", new Vector3(x, height / 3, z), new Vector3(1.5f, height, 1.5f), i % 2 == 0 ? green : lightGreen);
            }
            for (int i = 0; i < 13; i++)
            {
                float x = -7 + (float)random.NextDouble() * 14;
                float z = -8.7f + (float)random.NextDouble();
                Shape("Boundary rock", PrimitiveType.Sphere, new Vector3(x, .15f, z), new Vector3(.7f, .5f, .7f), sand, true);
            }
            for (int i = 0; i < 8; i++)
            {
                float x = -.4f + i * 1.05f;
                Shape("Fence post", PrimitiveType.Cube, new Vector3(x, .43f, 1.9f), new Vector3(.12f, .85f, .12f), wood);
            }
            Shape("Fence rail", PrimitiveType.Cube, new Vector3(3.2f, .54f, 1.9f), new Vector3(7.5f, .14f, .12f), wood);

            var water = Mat("Pond", "639C9D");
            Shape("Pond rim", PrimitiveType.Cylinder, new Vector3(-5.6f, .035f, -4), new Vector3(3.7f, .08f, 3), sand);
            Shape("Pond water", PrimitiveType.Cylinder, new Vector3(-5.6f, .12f, -4), new Vector3(3.3f, .02f, 2.6f), water);
            Shape("Pond obstacle", PrimitiveType.Cube, new Vector3(-5.6f, -.45f, -4), new Vector3(3.2f, 1.2f, 2.5f), water, true).GetComponent<Renderer>().enabled = false;
            var flowers = Mat("Wildflowers", "E2C07F");
            for (int i = 0; i < 28; i++)
                Shape("Wildflower", PrimitiveType.Sphere, new Vector3(-7 + (float)random.NextDouble() * 14, .17f, 7 + (float)random.NextDouble()), Vector3.one * .16f, flowers);

            Player = new GameObject("Farmer").transform;
            Player.SetParent(transform);
            Controller = Player.gameObject.AddComponent<CharacterController>();
            Controller.height = 1.5f;
            Controller.radius = .28f;
            Controller.center = new Vector3(0, .8f, 0);
            Controller.stepOffset = .25f;
            var body = Shape("Coat", PrimitiveType.Capsule, new Vector3(0, .7f, 0), new Vector3(.58f, .45f, .58f), Mat("Blue coat", "466D88"));
            body.SetParent(Player, false);
            var head = Shape("Face", PrimitiveType.Sphere, new Vector3(0, 1.35f, 0), Vector3.one * .4f, Mat("Skin", "E4B38A"));
            head.SetParent(Player, false);
            var hat = Shape("Straw hat", PrimitiveType.Cylinder, new Vector3(0, 1.57f, 0), new Vector3(.75f, .07f, .75f), flowers);
            hat.SetParent(Player, false);
            var crown = Shape("Hat crown", PrimitiveType.Cylinder, new Vector3(0, 1.68f, 0), new Vector3(.42f, .08f, .42f), flowers);
            crown.SetParent(Player, false);

            View = new GameObject("Farm camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            View.transform.SetParent(transform);
            View.tag = "MainCamera";
            View.transform.position = new Vector3(18, 23, -25);
            View.transform.LookAt(new Vector3(0, 0, 0));
            View.orthographic = true;
            View.orthographicSize = 10f;
            View.nearClipPlane = .1f;
            View.farClipPlane = 100;
            View.clearFlags = CameraClearFlags.SolidColor;
            View.backgroundColor = new Color(.73f, .82f, .81f);
            var sun = new GameObject("Afternoon sun", typeof(Light)).GetComponent<Light>();
            sun.transform.SetParent(transform);
            sun.type = LightType.Directional;
            sun.transform.rotation = Quaternion.Euler(45, -35, 0);
            sun.color = new Color(1, .92f, .8f);
            sun.intensity = 1.6f;
            sun.shadows = LightShadows.Soft;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.68f, .74f, .72f);
            Adventure = gameObject.AddComponent<AdventureWorld>();
            Adventure.Build(this);
        }

        public void RefreshCrops(FarmState state)
        {
            for (int i = 0; i < FarmState.PlotCount; i++)
            {
                CropState crop = state.plots[i];
                soils[i].sharedMaterial = crop.watered ? wet : dry;
                crops[i].gameObject.SetActive(crop.planted);
                crops[i].localScale = Vector3.one * (crop.Ready ? 1 : .35f + crop.growth * .3f);
            }
        }

        public void Select(int index)
        {
            selection.gameObject.SetActive(index >= 0);
            if (index >= 0) selection.position = PlotPositions[index];
        }

        public Material Mat(string name, string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out Color color);
            var material = new Material(materialTemplate) { name = name, color = color };
            material.SetFloat("_Smoothness", .05f);
            materials.Add(material);
            return material;
        }

        public Transform Shape(string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material, bool solid = false)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(transform);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            var collider = go.GetComponent<Collider>();
            if (!solid) { collider.enabled = false; Destroy(collider); }
            return go.transform;
        }

        public Transform Cone(string name, Vector3 position, Vector3 scale, Material material)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(transform);
            go.transform.position = position;
            go.transform.localScale = scale;
            go.GetComponent<MeshFilter>().sharedMesh = cone;
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go.transform;
        }

        static Mesh CreateCone()
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            const int sides = 7;
            for (int i = 0; i < sides; i++)
            {
                float a = i * Mathf.PI * 2 / sides, b = (i + 1) * Mathf.PI * 2 / sides;
                int n = vertices.Count;
                vertices.Add(new Vector3(Mathf.Cos(a) * .5f, 0, Mathf.Sin(a) * .5f));
                vertices.Add(Vector3.up);
                vertices.Add(new Vector3(Mathf.Cos(b) * .5f, 0, Mathf.Sin(b) * .5f));
                triangles.Add(n); triangles.Add(n + 1); triangles.Add(n + 2);
            }
            var mesh = new Mesh { name = "Faceted canopy" };
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        void OnDestroy()
        {
            foreach (var material in materials) if (material != null) Destroy(material);
            if (cone != null) Destroy(cone);
        }
    }
}
