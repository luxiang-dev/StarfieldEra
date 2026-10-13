using System.Collections.Generic;
using UnityEngine;

namespace StarfieldEra
{
    /// <summary>Pointy-top, odd-row offset hex coordinates shared by terrain and farming.</summary>
    public sealed class HexWorldGrid : MonoBehaviour
    {
        public const float Radius = 1.15f;
        public const float HalfWidth=42, HalfDepth=36;
        public const float RowSpacing = Radius * 1.5f;
        public static float ColumnSpacing => Mathf.Sqrt(3) * Radius;
        public static Vector3 Center(int column, int row) =>
            new Vector3(.5f + ColumnSpacing * (column + .5f * (row & 1)), 0, .15f + RowSpacing * row);

        readonly List<Renderer> tiles = new List<Renderer>();
        Mesh tileMesh;
        readonly Dictionary<Vector2Int,Renderer> cells=new Dictionary<Vector2Int,Renderer>();
        readonly Dictionary<Material,Material> surfaces = new Dictionary<Material,Material>();
        Material Surface(Material source)
        {
            if(surfaces.TryGetValue(source,out var surface))return surface;
            var shader=Shader.Find("StarfieldEra/Painterly Ground");
            if(!shader)return source;
            surface=new Material(shader){name=source.name+" painted",color=source.color};
            surfaces[source]=surface;return surface;
        }

        public void Build(FarmWorld world, Material edge)
        {
            tileMesh = Prism(Radius - .008f, .65f, "Hex terrain tile");
            var meadow = new[] {world.Mat("Hex meadow A", "87A653"),world.Mat("Hex meadow B", "90AD5F"),world.Mat("Hex meadow C", "819C50")};
            for (int row = -20; row <= 20; row++)
            for (int column = -23; column <= 22; column++)
            {
                var position = Center(column, row);
                if (position.x < -HalfWidth+1 || position.x > HalfWidth-1 || position.z < -HalfDepth+1 || position.z > HalfDepth-1) continue;
                var go = new GameObject($"Hex [{column}, {row}]", typeof(MeshFilter), typeof(MeshRenderer));
                go.transform.SetParent(transform, false); go.transform.localPosition = position;
                go.GetComponent<MeshFilter>().sharedMesh = tileMesh;
                var renderer = go.GetComponent<MeshRenderer>();
                int variant = (column * 31 + row * 17) % 3;
                if (variant < 0) variant += 3;
                renderer.sharedMaterials = new[] { Surface(meadow[variant]), edge };
                tiles.Add(renderer);
                cells[new Vector2Int(column,row)]=renderer;
            }
            // Continuous support beneath the narrow tile seams keeps walking smooth.
            var support = gameObject.AddComponent<BoxCollider>();
            support.center = new Vector3(0, -.4f, 0); support.size = new Vector3(HalfWidth*2, .8f, HalfDepth*2);
        }

        public bool Contains(Vector2Int cell)=>cells.ContainsKey(cell);
        public bool IsDry(Vector2Int cell)
        {
            if(!cells.TryGetValue(cell,out var renderer))return false;
            string name=renderer.sharedMaterials[0].name;
            return !name.StartsWith("Lake") && !name.StartsWith("Pond");
        }
        public bool TryCell(Vector3 position,out Vector2Int cell)
        {
            cell=default;float nearest=float.MaxValue;
            int row=Mathf.RoundToInt((position.z-.15f)/RowSpacing);
            for(int r=row-1;r<=row+1;r++)
            {
                int column=Mathf.RoundToInt((position.x-.5f)/ColumnSpacing-.5f*(r&1));
                for(int c=column-1;c<=column+1;c++)
                {
                    var delta=position-Center(c,r);delta.y=0;
                    if(delta.sqrMagnitude<nearest){nearest=delta.sqrMagnitude;cell=new Vector2Int(c,r);}
                }
            }
            return cells.ContainsKey(cell)&&nearest<=Radius*Radius;
        }

        public void PaintBox(Vector3 center, Vector3 size, Material material)
        {
            foreach (var renderer in tiles)
            {
                var p = renderer.transform.position;
                if (Mathf.Abs(p.x - center.x) > size.x * .5f || Mathf.Abs(p.z - center.z) > size.z * .5f) continue;
                var palette = renderer.sharedMaterials; palette[0] = Surface(material); renderer.sharedMaterials = palette;
            }
        }

        public static Vector3 Corner(int index, float radius)
        {
            float angle = (30 + 60 * index) * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
        }

        public static Mesh Prism(float radius, float depth, string name)
        {
            var vertices = new List<Vector3>();
            var top = new List<int>(); var sides = new List<int>();
            vertices.Add(Vector3.zero);
            for (int i = 0; i < 6; i++) vertices.Add(Corner(i, radius));
            for (int i = 0; i < 6; i++)
            {
                top.Add(0); top.Add((i + 1) % 6 + 1); top.Add(i + 1);
                int n = vertices.Count;
                var a = Corner(i, radius); var b = Corner((i + 1) % 6, radius);
                vertices.Add(a); vertices.Add(b); vertices.Add(a - Vector3.up * depth); vertices.Add(b - Vector3.up * depth);
                sides.AddRange(new[] {n, n + 1, n + 2, n + 1, n + 3, n + 2});
            }
            var mesh = new Mesh { name = name, subMeshCount = 2 };
            mesh.SetVertices(vertices); mesh.SetTriangles(top, 0); mesh.SetTriangles(sides, 1);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        public static Mesh Outline(float radius, float width)
        {
            var vertices = new List<Vector3>(); var indices = new List<int>();
            for (int i = 0; i < 6; i++) {vertices.Add(Corner(i, radius));vertices.Add(Corner(i, radius - width));}
            for (int i = 0; i < 6; i++)
            {
                int a = i * 2, b = ((i + 1) % 6) * 2;
                indices.AddRange(new[] {a, a + 1, b, b, a + 1, b + 1});
            }
            var mesh = new Mesh { name = "Hex selection outline" };
            mesh.SetVertices(vertices); mesh.SetTriangles(indices, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }
        void OnDestroy() { if (tileMesh) Destroy(tileMesh); foreach(var material in surfaces.Values)if(material)Destroy(material); }
    }
}
