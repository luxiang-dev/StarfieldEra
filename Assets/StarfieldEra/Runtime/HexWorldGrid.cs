using System.Collections.Generic;
using UnityEngine;

namespace StarfieldEra
{
    /// <summary>Pointy-top, odd-row offset hex coordinates shared by terrain and farming.</summary>
    public sealed partial class HexWorldGrid : MonoBehaviour
    {
        public const float Radius = 1.15f;
        public const float HalfWidth=42, HalfDepth=36;
        public const float RowSpacing = Radius * 1.5f;
        public static float ColumnSpacing => Mathf.Sqrt(3) * Radius;
        public static Vector3 Center(int column, int row) =>
            new Vector3(.5f + ColumnSpacing * (column + .5f * (row & 1)), 0, .15f + RowSpacing * row);

        readonly Dictionary<Vector2Int,TerrainKind> terrain=new Dictionary<Vector2Int,TerrainKind>();
        readonly HashSet<Vector2Int> cells=new HashSet<Vector2Int>();

        public void Build(FarmWorld world, Material edge)
        {
            // Cells are gameplay records, not separate renderers.
            for (int row=-20;row<=20;row++)for(int column=-23;column<=22;column++)
            {
                var position=Center(column,row);
                if(position.x < -HalfWidth+1 || position.x > HalfWidth-1 || position.z < -HalfDepth+1 || position.z > HalfDepth-1)continue;
                var cell=new Vector2Int(column,row);cells.Add(cell);terrain[cell]=TerrainKind.Meadow;
            }
            // Continuous support beneath the narrow tile seams keeps walking smooth.
            var support = gameObject.AddComponent<BoxCollider>();
            support.center = new Vector3(0, -.4f, 0); support.size = new Vector3(HalfWidth*2, .8f, HalfDepth*2);
        }

        public float GroundHeight(Vector3 p){var surface=GetComponent<ContinuousTerrain>();return surface?surface.HeightAt(p):0;}
        public Vector3 SurfacePoint(Vector3 p,float offset=0){p.y=GroundHeight(p)+offset;return p;}
        public bool IsGroundCollider(Collider col)=>col==GetComponent<Collider>()||(GetComponent<ContinuousTerrain>()&&GetComponent<ContinuousTerrain>().IsGround(col));
        public bool RaycastGround(Ray ray,out Vector3 point)
        {var surface=GetComponent<ContinuousTerrain>();if(surface&&surface.Raycast(ray,out var hit)){point=hit.point;return true;}point=default;return false;}
        public IEnumerable<Vector2Int> Cells=>cells;
        public void SetTerrain(Vector2Int cell,TerrainKind kind){if(cells.Contains(cell))terrain[cell]=kind;}
        public bool Contains(Vector2Int cell)=>cells.Contains(cell);
        public TerrainKind TerrainAt(Vector2Int cell)=>terrain.TryGetValue(cell,out var kind)?kind:TerrainKind.Water;
        public TerrainKind TerrainAt(Vector3 position)=>TryCell(position,out var cell)?TerrainAt(cell):TerrainKind.Water;
        public bool IsDry(Vector2Int cell)=>Contains(cell)&&TerrainTraits.Walkable(TerrainAt(cell));
        public bool CanBuildOn(Vector2Int cell)=>Contains(cell)&&TerrainTraits.Buildable(TerrainAt(cell));
        public bool CanStand(Vector3 position,float radius=.3f)
        {
            if(!TerrainTraits.Walkable(TerrainAt(position)))return false;
            for(int i=0;i<12;i++)
            {float angle=i*Mathf.PI/6;if(!TerrainTraits.Walkable(TerrainAt(position+new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius))))return false;}
            return true;
        }
        public Vector3 ConstrainMovement(Vector3 start,Vector3 displacement,float radius=.3f)
        {
            displacement.y=0;int steps=Mathf.Max(1,Mathf.CeilToInt(displacement.magnitude/.12f));var step=displacement/steps;var position=start;
            for(int i=0;i<steps;i++)
            {
                if(CanStand(position+step,radius))position+=step;
                else
                {
                    var x=new Vector3(step.x,0,0);var z=new Vector3(0,0,step.z);
                    if(CanStand(position+x,radius))position+=x;
                    if(CanStand(position+z,radius))position+=z;
                }
            }
            return position-start;
        }
        public Vector3 SafeLand(Vector3 desired,float radius=.3f, bool obstacles=true)
        {
            desired=SurfacePoint(desired,Mathf.Clamp(desired.y,0,.25f));
            if(CanStand(desired,radius)&&(!obstacles||Clear(desired,radius)))return desired;
            Vector3 best=Center(0,0)+Vector3.up*desired.y;float nearest=float.MaxValue;
            foreach(var cell in cells)
            {var p=SurfacePoint(Center(cell.x,cell.y),.08f);float d=Mathf.Pow(WorldRelief.PlanarDistance(p,desired),2);if(d>=nearest||!CanStand(p,radius)||(obstacles&&!Clear(p,radius)))continue;nearest=d;best=p;}
            return best;
        }
        bool Clear(Vector3 p,float radius)
        {
            foreach(var collider in Physics.OverlapSphere(p+Vector3.up*.75f,radius,~0,QueryTriggerInteraction.Ignore))
                if(!IsGroundCollider(collider)&&!(collider is CharacterController))return false;
            return true;
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
            return cells.Contains(cell)&&nearest<=Radius*Radius;
        }

        public void PaintBox(Vector3 center, Vector3 size, Material material,TerrainKind? kind=null)
        {
            foreach(var cell in cells)
            {
                var p=Center(cell.x,cell.y);
                if(Mathf.Abs(p.x-center.x)>size.x*.5f||Mathf.Abs(p.z-center.z)>size.z*.5f)continue;
                if(kind.HasValue)terrain[cell]=kind.Value;
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

    }
}
