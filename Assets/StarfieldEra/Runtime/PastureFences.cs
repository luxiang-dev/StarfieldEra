using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

namespace StarfieldEra
{
    [Serializable] public sealed class FenceRecord { public int column,row,edge,damage; public bool gate,open;
        // Old records retain their original hex edge. New fences use world-space post centres.
        public bool free;public float ax,az,bx,bz; }
    public sealed class FenceTarget : MonoBehaviour { public FenceRecord record; }
    public sealed class PastureFences : MonoBehaviour
    {
        FarmGame game; Material timber,allowed,blocked; GameObject preview;
        readonly Dictionary<string,FenceRecord> edges=new Dictionary<string,FenceRecord>();
        readonly Dictionary<string,GameObject> models=new Dictionary<string,GameObject>();
        string previewKey="";bool previewValid;int mode=-1;
        public const float SegmentLength=1.15f;
        Vector3? anchor;Vector3 chainStart;Vector3 chainDirection=Vector3.forward;
        GameObject fencePrefab;
        readonly List<FenceRecord> plan=new List<FenceRecord>(); public bool Active=>mode>=0; public string Hint {get;private set;}="";
        public FenceRecord Inspected {get;private set;}
        static readonly Vector2Int[] Steps={new Vector2Int(0,1),new Vector2Int(-1,1),new Vector2Int(-1,0),new Vector2Int(0,-1),new Vector2Int(1,-1),new Vector2Int(1,0)};
        public static Vector2Int Neighbor(Vector2Int c,int edge)
        {int q=c.x-(c.y-(c.y&1))/2;var d=Steps[edge];int r=c.y+d.y;return new Vector2Int(q+d.x+(r-(r&1))/2,r);}
        public static string Key(Vector2Int c,int edge)
        {var n=Neighbor(c,edge);if(n.y<c.y||(n.y==c.y&&n.x<c.x)){c=n;edge=(edge+3)%6;}return c.x+":"+c.y+":"+edge;}
        public static void Endpoints(FenceRecord f,out Vector3 a,out Vector3 b)
        {
            if(f.free){a=new Vector3(f.ax,0,f.az);b=new Vector3(f.bx,0,f.bz);return;}
            var center=HexWorldGrid.Center(f.column,f.row);
            a=center+HexWorldGrid.Corner(f.edge,HexWorldGrid.Radius);b=center+HexWorldGrid.Corner((f.edge+1)%6,HexWorldGrid.Radius);
        }
        static string PointKey(Vector3 p)=>Mathf.RoundToInt(p.x*1000)+","+Mathf.RoundToInt(p.z*1000);
        static string RecordKey(FenceRecord f)
        {
            if(!f.free)return Key(new Vector2Int(f.column,f.row),f.edge);
            Endpoints(f,out var a,out var b);string x=PointKey(a),y=PointKey(b);
            return "line:"+(string.CompareOrdinal(x,y)<0?x+":"+y:y+":"+x);
        }
        public static bool Valid(List<FenceRecord> list)
        {
            if(list==null||list.Count>600)return false;var keys=new HashSet<string>();
            foreach(var f in list)
            {
                if(f==null||f.damage<0||f.damage>2||f.edge<0||f.edge>5||f.column<HexWorldGrid.MinColumn||f.column>HexWorldGrid.MaxColumn||f.row<HexWorldGrid.MinRow||f.row>HexWorldGrid.MaxRow||(!f.gate&&f.open))return false;
                if(f.free)
                {
                    Endpoints(f,out var a,out var b);float length=WorldRelief.PlanarDistance(a,b);
                    if(float.IsNaN(length)||float.IsInfinity(length)||length<.2f||length>SegmentLength*1.6f||Mathf.Abs(a.x)>HexWorldGrid.HalfWidth||Mathf.Abs(b.x)>HexWorldGrid.HalfWidth||Mathf.Abs(a.z)>HexWorldGrid.HalfDepth||Mathf.Abs(b.z)>HexWorldGrid.HalfDepth)return false;
                }
                if(!keys.Add(RecordKey(f)))return false;
            }
            return true;
        }
        public void Initialize(FarmGame owner)
        {
            game=owner;fencePrefab=Resources.Load<GameObject>("Environment/FantasyFence");timber=game.World.Mat("Pasture timber","8C603D");allowed=game.World.Mat("Fence preview allowed","91C77B");blocked=game.World.Mat("Fence preview blocked","D16B54");
            foreach(var f in game.State.adventure.fences){var key=RecordKey(f);edges[key]=f;models[key]=Model(f,false);}
            RefreshJunctions();
        }
        public void Choose(int next)
        {if(game.Night&&game.Night.Active)return;game.Night?.CancelTrap();game.Land?.Cancel();game.Construction.Cancel();Cancel();mode=next;game.Adventure.Hud.Hide();game.ShowMessage(next==2?"围栏拆除：选择围栏，返还一半木材。":"围栏建造：选择起点与结束位置，连续直线摆放；连接点自动连接。围栏木2，牧场门木4。");}
        public void RepairInspected()
        {
            if(Inspected==null||Inspected.damage==0||game.State.adventure.wood<1)return;
            Endpoints(Inspected,out var a,out var b);if(WorldRelief.PlanarDistance(game.World.Player.position,(a+b)*.5f)>6){game.ShowMessage("走近围栏六步内修补。");return;}
            game.State.adventure.wood--;Inspected.damage=0;game.Adventure.Commit("围栏已修补。");
        }
        public void Damage(FenceRecord f)
        {
            if(f==null||!game.State.adventure.fences.Contains(f))return;
            f.damage++;
            if(f.damage<3)return;
            string key=RecordKey(f);game.State.adventure.fences.Remove(f);edges.Remove(key);
            if(models.TryGetValue(key,out var root)){root.SetActive(false);Destroy(root);models.Remove(key);}f.damage=0;
            RefreshJunctions();game.Animals.RefreshPens();game.Persist();
        }
        public void Cancel(){mode=-1;anchor=null;plan.Clear();Hint="";ClearPreview();}
        void ClearPreview(){if(preview){preview.SetActive(false);Destroy(preview);}preview=null;previewKey="";}
        static float Cross(Vector3 a,Vector3 b)=>a.x*b.z-a.z*b.x;
        static bool Crosses(Vector3 a,Vector3 b,Vector3 c,Vector3 d)
        {
            var ab=b-a;var cd=d-c;float denominator=Cross(ab,cd);
            if(Mathf.Abs(denominator)<.00001f)return false;
            float t=Cross(c-a,cd)/denominator,u=Cross(c-a,ab)/denominator;
            return t>=-.001f&&t<=1.001f&&u>=-.001f&&u<=1.001f;
        }
        public bool Blocks(Vector2Int c,int edge)
        {
            if(edges.TryGetValue(Key(c,edge),out var f)&&(!f.gate||!f.open))return true;
            var a=HexWorldGrid.Center(c.x,c.y);var n=Neighbor(c,edge);var b=HexWorldGrid.Center(n.x,n.y);
            foreach(var item in edges.Values)if(item.free&&(!item.gate||!item.open))
            {Endpoints(item,out var p,out var q);if(Crosses(a,b,p,q))return true;}
            return false;
        }
        public bool Occupies(Vector2Int cell)
        {
            var center=HexWorldGrid.Center(cell.x,cell.y);
            foreach(var f in edges.Values)
            {
                Endpoints(f,out var a,out var b);var ab=b-a;
                var nearest=a+ab*Mathf.Clamp01(Vector3.Dot(center-a,ab)/ab.sqrMagnitude);
                if(WorldRelief.PlanarDistance(center,nearest)<HexWorldGrid.Radius+.14f)return true;
            }
            return false;
        }
        public List<Vector2Int> Enclosure(Vector2Int start)
        {
            var result=new List<Vector2Int>();var seen=new HashSet<Vector2Int>{start};var queue=new Queue<Vector2Int>();queue.Enqueue(start);
            while(queue.Count>0)
            {
                var c=queue.Dequeue();if(!game.World.HexGrid.IsDry(c))return null;result.Add(c);if(result.Count>64)return null;
                for(int e=0;e<6;e++){if(Blocks(c,e))continue;var n=Neighbor(c,e);if(!game.World.HexGrid.Contains(n))return null;if(seen.Add(n))queue.Enqueue(n);}
            }
            bool hasFree=false;foreach(var fence in edges.Values)if(fence.free){hasFree=true;break;}
            return result.Count>=3&&(!hasFree||ClosedBoundaryContains(HexWorldGrid.Center(start.x,start.y)))?result:null;
        }
        // A gap can fall between hex-centre paths. Require an actual closed fence loop,
        // so opening any gate or leaving a short gap cannot create a false sealed pasture.
        bool ClosedBoundaryContains(Vector3 point)
        {
            var records=new List<FenceRecord>();var joins=new Dictionary<string,List<int>>();
            foreach(var record in edges.Values)
            {
                if(record.gate&&record.open)continue;
                Endpoints(record,out var a,out var b);int index=records.Count;records.Add(record);
                foreach(var key in new[]{PointKey(a),PointKey(b)}){if(!joins.TryGetValue(key,out var connected)){connected=new List<int>();joins[key]=connected;}connected.Add(index);}
            }
            var degree=new Dictionary<string,int>();var queue=new Queue<string>();var removed=new bool[records.Count];
            foreach(var join in joins){degree[join.Key]=join.Value.Count;if(join.Value.Count<2)queue.Enqueue(join.Key);}
            while(queue.Count>0)
            {
                var key=queue.Dequeue();foreach(int index in joins[key])
                {
                    if(removed[index])continue;removed[index]=true;Endpoints(records[index],out var a,out var b);
                    foreach(var endpoint in new[]{PointKey(a),PointKey(b)})if(--degree[endpoint]==1)queue.Enqueue(endpoint);
                }
            }
            // Test individual cycles instead of parity across the whole graph: adjoining
            // or nested pens may share posts/rails and must each remain valid pastures.
            var visited=new HashSet<string>();var active=new Dictionary<string,int>();var path=new List<Vector3>();
            bool Visit(string key)
            {
                visited.Add(key);active[key]=path.Count;
                foreach(int index in joins[key])if(!removed[index]){Endpoints(records[index],out var p,out var q);path.Add(PointKey(p)==key?p:q);break;}
                foreach(int index in joins[key])if(!removed[index])
                {
                    Endpoints(records[index],out var p,out var q);string other=PointKey(p)==key?PointKey(q):PointKey(p);
                    if(active.TryGetValue(other,out int first)&&path.Count-first>=3)
                    {
                        bool inside=false;
                        for(int i=first,j=path.Count-1;i<path.Count;j=i++)
                        {var a=path[i];var b=path[j];if((a.z>point.z)!=(b.z>point.z)&&point.x<(b.x-a.x)*(point.z-a.z)/(b.z-a.z)+a.x)inside=!inside;}
                        if(inside)return true;
                    }
                    else if(!visited.Contains(other)&&Visit(other))return true;
                }
                active.Remove(key);path.RemoveAt(path.Count-1);return false;
            }
            foreach(var join in joins)if(degree[join.Key]>=2&&!visited.Contains(join.Key)&&Visit(join.Key))return true;
            return false;
        }

        public List<Vector2Int> FindPasture()
        {
            List<Vector2Int> best=null;float distance=10;
            var candidates=new HashSet<Vector2Int>();
            foreach(var f in edges.Values){var cell=new Vector2Int(f.column,f.row);candidates.Add(cell);for(int edge=0;edge<6;edge++)candidates.Add(Neighbor(cell,edge));}
            foreach(var c in candidates)
            {var pen=Enclosure(c);if(pen==null)continue;var center=Vector3.zero;foreach(var tile in pen)center+=HexWorldGrid.Center(tile.x,tile.y);center/=pen.Count;float d=WorldRelief.PlanarDistance(center,game.World.Player.position);if(d<distance){distance=d;best=pen;}}
            return best;
        }
        public void Inspect(FenceRecord f){Inspected=f;game.Construction.Cancel();Cancel();game.Adventure.Hud.Hide();game.Adventure.Hud.Show("fence");}
        public void ToggleGate()
        {
            if(Inspected==null||!Inspected.gate)return;Inspected.open=!Inspected.open;var key=RecordKey(Inspected);models[key].SetActive(false);Destroy(models[key]);models[key]=Model(Inspected,false);RefreshJunctions();game.Adventure.Commit(Inspected.open?"牧场门已打开。关门后恢复封闭牧场。":"牧场门已关闭。");game.Animals.RefreshPens();
        }
        public void RemoveInspected(){if(Inspected!=null)Remove(Inspected);}
        void Remove(FenceRecord f)
        {if(!EquipmentCatalog.Require(game,"hammer"))return;var key=RecordKey(f);game.State.adventure.fences.Remove(f);edges.Remove(key);models[key].SetActive(false);Destroy(models[key]);models.Remove(key);RefreshJunctions();game.State.adventure.wood+=f.gate?2:1;Inspected=null;game.Adventure.Hud.Hide();game.Animals.RefreshPens();game.Adventure.Commit("已拆除围栏，返还一半木材。");}
        bool GameManagementOpen()=>game.Management&&game.Management.IsOpen;
        // Starting inside a hex should attach to its nearest existing post. While drawing,
        // exclude the starting post so it cannot mask a nearby closing/connecting endpoint.
        public static bool TrySnapEndpoint(IEnumerable<FenceRecord> fences,Vector3 point,Vector3? exclude,float radius,out Vector3 snapped)
        {
            snapped=point;float nearest=radius;bool found=false;
            foreach(var f in fences)
            {
                Endpoints(f,out var a,out var b);
                foreach(var endpoint in new[]{a,b})
                {
                    if(exclude.HasValue&&WorldRelief.PlanarDistance(exclude.Value,endpoint)<.2f)continue;
                    float distance=WorldRelief.PlanarDistance(point,endpoint);
                    if(distance<nearest){nearest=distance;snapped=endpoint;found=true;}
                }
            }
            return found;
        }
        bool SnapEndpoint(Vector3 p,out Vector3 snapped)
            =>TrySnapEndpoint(edges.Values,p,anchor,anchor.HasValue?.7f:HexWorldGrid.Radius,out snapped);
        FenceRecord Line(Vector3 a,Vector3 b)
        {
            game.World.HexGrid.TryCell((a+b)*.5f,out var cell);
            return new FenceRecord{free=true,ax=a.x,az=a.z,bx=b.x,bz=b.z,column=cell.x,row=cell.y,gate=mode==1};
        }
        void MakePlan(Vector3 cursor)
        {
            plan.Clear();
            if(!anchor.HasValue)
            {
                SnapEndpoint(cursor,out var start);plan.Add(Line(start,start+chainDirection*SegmentLength));return;
            }
            var a=anchor.Value;var delta=cursor-a;delta.y=0;
            bool snapped=SnapEndpoint(cursor,out var end)&&WorldRelief.PlanarDistance(a,end)>.25f;
            if(!snapped&&WorldRelief.PlanarDistance(cursor,chainStart)<.42f&&WorldRelief.PlanarDistance(a,chainStart)>.25f){end=chainStart;snapped=true;}
            int count;
            if(snapped){count=Mathf.Clamp(Mathf.RoundToInt(WorldRelief.PlanarDistance(a,end)/SegmentLength),1,12);if(WorldRelief.PlanarDistance(a,end)>count*SegmentLength*1.6f)snapped=false;}
            if(!snapped)
            {
                float angle=Mathf.Round(Mathf.Atan2(delta.z,delta.x)*Mathf.Rad2Deg/15)*15*Mathf.Deg2Rad;
                var direction=delta.sqrMagnitude<.12f?chainDirection:new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                count=Mathf.Clamp(Mathf.RoundToInt(delta.magnitude/SegmentLength),1,12);end=a+direction*(count*SegmentLength);
            }
            count=Mathf.Clamp(Mathf.RoundToInt(WorldRelief.PlanarDistance(a,end)/SegmentLength),1,12);
            for(int n=0;n<count;n++)plan.Add(Line(Vector3.Lerp(a,end,(float)n/count),Vector3.Lerp(a,end,(float)(n+1)/count)));
        }
        bool PlanAllowed(out string why)
        {
            why="";if(game.State.adventure.fences.Count+plan.Count>600){why="围栏已达600段上限";return false;}
            if(game.State.adventure.wood<plan.Count*(mode==1?4:2)){why="木材不足";return false;}
            foreach(var record in plan)if(!Allowed(record,out why))return false;return true;
        }
        void Update()
        {
            if(!Active||GameManagementOpen())return;if(game.Adventure.InBattle){Cancel();return;}if(!WorldPointer.Available)return;
            if(WorldPointer.Cancel){if(anchor.HasValue){anchor=null;ClearPreview();}else Cancel();return;}
            var p=WorldPointer.Position;
            if((WorldPointer.Blocked)||p.y<Screen.height*.12f||p.y>Screen.height*.91f){if(preview)preview.SetActive(false);return;}
            var ray=game.World.View.ScreenPointToRay(p);
            if(mode==2)
            {
                ClearPreview();FenceRecord target=null;float nearestHit=float.MaxValue;
                foreach(var hit in Physics.RaycastAll(ray,200)){var f=hit.collider.GetComponentInParent<FenceTarget>();if(f!=null&&hit.distance<nearestHit){target=f.record;nearestHit=hit.distance;}}
                Hint="围栏拆除模式";if(WorldPointer.Pressed){if(target!=null)Remove(target);else game.ShowMessage("选择要拆除的围栏。");}return;
            }
            if(!game.World.HexGrid.RaycastGround(ray,out var cursor)){if(preview)preview.SetActive(false);return;}
            MakePlan(cursor);bool valid=PlanAllowed(out var why);
            string key=(anchor.HasValue?"end:":"start:")+string.Join("|",plan.ConvertAll(RecordKey));
            if(!preview||previewKey!=key)
            {
                ClearPreview();preview=new GameObject("Fence line preview");preview.transform.SetParent(transform,false);
                for(int i=0;i<plan.Count;i++)
                {
                    var model=Model(plan[i],true);model.transform.SetParent(preview.transform,true);
                    if(i>0)foreach(var part in model.GetComponentsInChildren<Transform>())if(part.name=="FencePostStart")part.gameObject.SetActive(false);
                }
                previewKey=key;previewValid=!valid;
            }
            preview.SetActive(true);if(previewValid!=valid){foreach(var r in preview.GetComponentsInChildren<Renderer>())r.sharedMaterial=valid?allowed:blocked;previewValid=valid;}
            Hint=!anchor.HasValue?"选择围栏起点 · 靠近已有立柱自动连接":(mode==1?"牧场门":"木围栏")+" 共"+plan.Count+" · 木材 "+(plan.Count*(mode==1?4:2))+" · "+(valid?"选择结束位置，连续建造":why);
            if(!WorldPointer.Pressed)return;
            if(!anchor.HasValue)
            {
                if(!EquipmentCatalog.Require(game,"hammer"))return;
                SnapEndpoint(cursor,out var start);anchor=start;chainStart=start;ClearPreview();return;
            }
            if(!valid){game.ShowMessage(why);return;}
            foreach(var record in plan){game.State.adventure.fences.Add(record);edges[RecordKey(record)]=record;models[RecordKey(record)]=Model(record,false);}
            game.State.adventure.wood-=plan.Count*(mode==1?4:2);
            Endpoints(plan[plan.Count-1],out var lastStart,out var lastEnd);chainDirection=(lastEnd-lastStart).normalized;anchor=lastEnd;
            RefreshJunctions();ClearPreview();game.Animals.RefreshPens();game.Adventure.Commit("围栏建造完成，可从连接处继续摆放。");
        }
        bool Allowed(FenceRecord f,out string why)
        {
            why="";if(!EquipmentCatalog.Has(game,"hammer")){why="需要持有建造锤";return false;}
            Endpoints(f,out var a,out var b);var grid=game.World.HexGrid;
            if(edges.ContainsKey(RecordKey(f))){why="该位置已有围栏";return false;}
            if(WorldRelief.PlanarDistance(a,game.World.Player.position)>10||WorldRelief.PlanarDistance(b,game.World.Player.position)>10){why="请走近十步以内";return false;}
            foreach(var existing in edges.Values)
            {
                Endpoints(existing,out var c,out var d);
                bool endpoint=WorldRelief.PlanarDistance(a,c)<.04f||WorldRelief.PlanarDistance(a,d)<.04f||WorldRelief.PlanarDistance(b,c)<.04f||WorldRelief.PlanarDistance(b,d)<.04f;
                if(!endpoint&&Crosses(a,b,c,d)){why="围栏不能相交，请连接连接点";return false;}
                // Reject overlapping collinear spans, even when sharing an endpoint.
                var axis=(b-a).normalized;float tc=Vector3.Dot(c-a,axis),td=Vector3.Dot(d-a,axis);
                if(Mathf.Abs(Cross(axis,c-a))<.08f&&Mathf.Abs(Cross(axis,d-a))<.08f&&Mathf.Min(Mathf.Max(tc,td),(b-a).magnitude)-Mathf.Max(Mathf.Min(tc,td),0)>.12f){why="该位置已有围栏";return false;}
            }
            float startHeight=grid.GroundHeight(a),endHeight=grid.GroundHeight(b);
            if(Mathf.Abs(startHeight-endHeight)>.45f){why="地面不平，请选择平缓陆地";return false;}
            for(int n=0;n<=4;n++)
            {
                var p=Vector3.Lerp(a,b,n/4f);
                if(!grid.TryCell(p,out var cell)||!grid.CanBuildOn(cell)||!grid.CanStand(p,.12f)){why="选择稳定陆地，避开水面、河道和沼泽";return false;}
                foreach(var building in game.State.adventure.buildings)if(BuildingFootprints.Cells(building).Contains(cell)){why="围栏覆盖建筑占地";return false;}
                p=grid.SurfacePoint(p);
                foreach(var col in Physics.OverlapSphere(p+Vector3.up*.55f,.18f))if(!col.isTrigger&&!grid.IsGroundCollider(col)&&!col.GetComponentInParent<FenceTarget>()){why="围栏位置有障碍或角色";return false;}
            }
            return true;
        }
        void RefreshJunctions()
        {
            var seen=new HashSet<string>();
            foreach(var item in models.Values)
            {
                var target=item.GetComponent<FenceTarget>();if(!target)continue;Endpoints(target.record,out var a,out var b);
                foreach(var part in item.GetComponentsInChildren<Transform>(true))
                    if(part.name=="FencePostStart"||part.name=="FencePostEnd")
                    {var key=PointKey(part.name=="FencePostStart"?a:b);part.gameObject.SetActive(seen.Add(key));}
            }
        }
        GameObject Model(FenceRecord f,bool ghost)
        {
            var root=new GameObject(ghost?"Fence preview":f.gate?"Pasture gate":"Pasture fence");root.transform.SetParent(transform,false);
            Endpoints(f,out var a,out var b);a=game.World.HexGrid.SurfacePoint(a);b=game.World.HexGrid.SurfacePoint(b);
            float span=WorldRelief.PlanarDistance(a,b);root.transform.position=(a+b)*.5f;
            root.transform.rotation=Quaternion.LookRotation((b-a).normalized,Vector3.up);
            if(fencePrefab)
            {
                var art=Instantiate(fencePrefab,root.transform);art.name="Painted fence";art.transform.localScale=new Vector3(SegmentLength,SegmentLength,Vector3.Distance(a,b));
                foreach(var part in art.GetComponentsInChildren<Transform>())
                {
                    if(part.name!="FencePostStart"&&part.name!="FencePostEnd")continue;
                    var renderer=part.GetComponent<Renderer>();if(!renderer)continue;
                    var endpoint=part.name=="FencePostStart"?a:b;
                    var offset=endpoint-renderer.bounds.center;offset.y=0;part.position+=offset;
                }
                if(f.gate&&f.open)
                {
                    var hinge=new GameObject("Open gate hinge").transform;hinge.SetParent(root.transform,false);hinge.localPosition=new Vector3(0,0,-Vector3.Distance(a,b)*.5f);
                    foreach(var part in art.GetComponentsInChildren<Transform>())if(part.name=="FenceRails")part.SetParent(hinge,true);
                    hinge.localRotation=Quaternion.Euler(0,85,0);
                }
            }
            else
            {
                Piece(root,"FencePostStart",new Vector3(0,.5f,-span*.5f),new Vector3(.14f,1,.14f),false);Piece(root,"FencePostEnd",new Vector3(0,.5f,span*.5f),new Vector3(.14f,1,.14f),false);
                for(int i=0;i<2;i++)Piece(root,"Rail",new Vector3(0,.35f+i*.35f,0),new Vector3(.1f,.12f,span),false);
            }
            if(!ghost)
            {
                var target=root.AddComponent<FenceTarget>();target.record=f;
                var col=root.AddComponent<BoxCollider>();col.center=new Vector3(0,.57f,0);col.size=new Vector3(.22f,1.3f,Vector3.Distance(a,b));col.isTrigger=f.gate&&f.open;
                if(f.gate&&f.open)foreach(float sign in new[]{-1f,1f}){var post=new GameObject("Gatepost collision");post.transform.SetParent(root.transform,false);post.transform.localPosition=new Vector3(0,.57f,sign*span*.5f);var c=post.AddComponent<BoxCollider>();c.size=new Vector3(.18f,1.2f,.18f);}
            }
            return root;
        }
        void Piece(GameObject root,string name,Vector3 p,Vector3 s,bool solid)
        {var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(root.transform,false);go.transform.localPosition=p;go.transform.localScale=s;go.GetComponent<Renderer>().sharedMaterial=timber;go.GetComponent<Collider>().enabled=solid;}
    }
}
