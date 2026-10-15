using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

namespace StarfieldEra
{
    [Serializable] public sealed class FenceRecord { public int column,row,edge; public bool gate,open; }
    public sealed class FenceTarget : MonoBehaviour { public FenceRecord record; }
    public sealed class PastureFences : MonoBehaviour
    {
        FarmGame game; Material timber,allowed,blocked; GameObject preview;
        readonly Dictionary<string,FenceRecord> edges=new Dictionary<string,FenceRecord>();
        readonly Dictionary<string,GameObject> models=new Dictionary<string,GameObject>();
        string previewKey="";bool previewValid;int mode=-1; public bool Active=>mode>=0; public string Hint {get;private set;}="";
        public FenceRecord Inspected {get;private set;}
        static readonly Vector2Int[] Steps={new Vector2Int(0,1),new Vector2Int(-1,1),new Vector2Int(-1,0),new Vector2Int(0,-1),new Vector2Int(1,-1),new Vector2Int(1,0)};
        public static Vector2Int Neighbor(Vector2Int c,int edge)
        {int q=c.x-(c.y-(c.y&1))/2;var d=Steps[edge];int r=c.y+d.y;return new Vector2Int(q+d.x+(r-(r&1))/2,r);}
        public static string Key(Vector2Int c,int edge)
        {var n=Neighbor(c,edge);if(n.y<c.y||(n.y==c.y&&n.x<c.x)){c=n;edge=(edge+3)%6;}return c.x+":"+c.y+":"+edge;}
        public static bool Valid(List<FenceRecord> list)
        {
            if(list==null||list.Count>600)return false;var keys=new HashSet<string>();
            foreach(var f in list){if(f==null||f.edge<0||f.edge>5||f.column<-23||f.column>22||f.row<-20||f.row>20||(!f.gate&&f.open)||!keys.Add(Key(new Vector2Int(f.column,f.row),f.edge)))return false;}
            return true;
        }
        public void Initialize(FarmGame owner)
        {
            game=owner;timber=game.World.Mat("Pasture timber","8C603D");allowed=game.World.Mat("Fence preview allowed","91C77B");blocked=game.World.Mat("Fence preview blocked","D16B54");
            foreach(var f in game.State.adventure.fences){var key=Key(new Vector2Int(f.column,f.row),f.edge);edges[key]=f;models[key]=Model(f,false);}
        }
        public void Choose(int next)
        {game.Land?.Cancel();game.Construction.Cancel();Cancel();mode=next;game.Adventure.Hud.Hide();game.ShowMessage(next==2?"围栏拆除：点击边缘，返还一份木材。":"围栏沿六边形边缘放置，左键连续建造；右键取消。围栏木2，牧场门木4。围住至少三格可驯养野猪。");}
        public void Cancel(){mode=-1;Hint="";if(preview)Destroy(preview);preview=null;previewKey="";}
        public bool Blocks(Vector2Int c,int edge)=>edges.TryGetValue(Key(c,edge),out var f)&&(!f.gate||!f.open);
        public List<Vector2Int> Enclosure(Vector2Int start)
        {
            var result=new List<Vector2Int>();var seen=new HashSet<Vector2Int>{start};var queue=new Queue<Vector2Int>();queue.Enqueue(start);
            while(queue.Count>0)
            {
                var c=queue.Dequeue();if(!game.World.HexGrid.IsDry(c))return null;result.Add(c);if(result.Count>64)return null;
                for(int e=0;e<6;e++){if(Blocks(c,e))continue;var n=Neighbor(c,e);if(!game.World.HexGrid.Contains(n))return null;if(seen.Add(n))queue.Enqueue(n);}
            }
            return result.Count>=3?result:null;
        }
        public List<Vector2Int> FindPasture()
        {
            List<Vector2Int> best=null;float distance=10;
            foreach(var f in edges.Values)foreach(var c in new[]{new Vector2Int(f.column,f.row),Neighbor(new Vector2Int(f.column,f.row),f.edge)})
            {var pen=Enclosure(c);if(pen==null)continue;var center=Vector3.zero;foreach(var tile in pen)center+=HexWorldGrid.Center(tile.x,tile.y);center/=pen.Count;float d=WorldRelief.PlanarDistance(center,game.World.Player.position);if(d<distance){distance=d;best=pen;}}
            return best;
        }
        public void Inspect(FenceRecord f){Inspected=f;game.Construction.Cancel();Cancel();game.Adventure.Hud.Hide();game.Adventure.Hud.Show("fence");}
        public void ToggleGate()
        {
            if(Inspected==null||!Inspected.gate)return;Inspected.open=!Inspected.open;var key=Key(new Vector2Int(Inspected.column,Inspected.row),Inspected.edge);models[key].SetActive(false);Destroy(models[key]);models[key]=Model(Inspected,false);game.Adventure.Commit(Inspected.open?"牧场门已打开。关门后恢复封闭牧场。":"牧场门已关闭。");game.Animals.RefreshPens();
        }
        public void RemoveInspected(){if(Inspected!=null)Remove(Inspected);}
        void Remove(FenceRecord f)
        {if(!EquipmentCatalog.Require(game,"hammer"))return;var key=Key(new Vector2Int(f.column,f.row),f.edge);game.State.adventure.fences.Remove(f);edges.Remove(key);models[key].SetActive(false);Destroy(models[key]);models.Remove(key);game.State.adventure.wood+=f.gate?2:1;Inspected=null;game.Adventure.Hud.Hide();game.Animals.RefreshPens();game.Adventure.Commit("已拆除围栏，返还一半木材。");}
        bool GameManagementOpen()=>game.Management&&game.Management.IsOpen;
        void Update()
        {
            if(!Active||GameManagementOpen())return;if(game.Adventure.InBattle){Cancel();return;}var mouse=Mouse.current;if(mouse==null)return;
            if(mouse.rightButton.wasPressedThisFrame){Cancel();return;}var p=mouse.position.ReadValue();
            if((EventSystem.current&&EventSystem.current.IsPointerOverGameObject())||p.y<Screen.height*.12f||p.y>Screen.height*.91f){if(preview)preview.SetActive(false);return;}
            var ray=game.World.View.ScreenPointToRay(p);if(!game.World.HexGrid.RaycastGround(ray,out var groundHit)||!game.World.HexGrid.TryCell(groundHit,out var cell)){if(preview)preview.SetActive(false);return;}
            var hit=groundHit-HexWorldGrid.Center(cell.x,cell.y);int edge=((Mathf.RoundToInt((Mathf.Atan2(hit.z,hit.x)*Mathf.Rad2Deg-60)/60)%6)+6)%6;
            var record=new FenceRecord{column=cell.x,row=cell.y,edge=edge,gate=mode==1};var key=Key(cell,edge);
            bool existing=edges.TryGetValue(key,out var old);bool valid=mode==2?existing:Allowed(record,out _);
            if(!preview||previewKey!=key){if(preview)Destroy(preview);preview=Model(record,true);previewKey=key;previewValid=!valid;}
            preview.SetActive(true);if(previewValid!=valid){foreach(var r in preview.GetComponentsInChildren<Renderer>())r.sharedMaterial=valid?allowed:blocked;previewValid=valid;}
            string why="";if(mode!=2)Allowed(record,out why);
            Hint=mode==2?"左键拆除围栏，右键取消":(record.gate?"牧场门":"木围栏")+" · "+(valid?"左键建造，移动鼠标连续放置":why);
            if(!mouse.leftButton.wasPressedThisFrame)return;
            if(!valid){game.ShowMessage(mode==2?"该边缘没有自建围栏。":why);return;}
            if(mode==2)Remove(old);
            else{game.State.adventure.wood-=record.gate?4:2;game.State.adventure.fences.Add(record);edges[key]=record;models[key]=Model(record,false);game.Animals.RefreshPens();game.Adventure.Commit("建成"+(record.gate?"牧场门":"木围栏")+"，可继续沿边缘放置。");}
        }
        bool Allowed(FenceRecord f,out string why)
        {
            why="";if(!EquipmentCatalog.Has(game,"hammer")){why="需要持有建造锤";return false;}var c=new Vector2Int(f.column,f.row);var n=Neighbor(c,f.edge);var center=HexWorldGrid.Center(c.x,c.y)+(HexWorldGrid.Corner(f.edge,HexWorldGrid.Radius)+HexWorldGrid.Corner((f.edge+1)%6,HexWorldGrid.Radius))*.5f;
            if(edges.ContainsKey(Key(c,f.edge))){why="该边缘已有围栏";return false;}
            if(game.State.adventure.fences.Count>=600){why="围栏已达600段上限";return false;}
            if(!game.World.HexGrid.Contains(n)||!game.World.HexGrid.CanBuildOn(c)||!game.World.HexGrid.CanBuildOn(n)){why="选择稳定陆地内部边缘，避开水面和沼泽";return false;}
            if(WorldRelief.PlanarDistance(center,game.World.Player.position)>10){why="请走近十步以内";return false;}
            foreach(var b in game.State.adventure.buildings){var cells=BuildingFootprints.Cells(b);if(cells.Contains(c)||cells.Contains(n)){why="边缘覆盖建筑占地";return false;}}
            center=game.World.HexGrid.SurfacePoint(center);
            foreach(var col in Physics.OverlapSphere(center+Vector3.up*.55f,.24f))if(!col.isTrigger&&!game.World.HexGrid.IsGroundCollider(col)&&!col.GetComponentInParent<FenceTarget>()){why="边缘有障碍或角色";return false;}
            if(game.State.adventure.wood<(f.gate?4:2)){why="木材不足";return false;}return true;
        }
        GameObject Model(FenceRecord f,bool ghost)
        {
            var root=new GameObject(ghost?"Fence preview":f.gate?"Pasture gate":"Pasture fence");root.transform.SetParent(transform,false);
            var center=HexWorldGrid.Center(f.column,f.row);var a=center+HexWorldGrid.Corner(f.edge,HexWorldGrid.Radius);var b=center+HexWorldGrid.Corner((f.edge+1)%6,HexWorldGrid.Radius);
            a=game.World.HexGrid.SurfacePoint(a);b=game.World.HexGrid.SurfacePoint(b);
            root.transform.position=(a+b)*.5f;root.transform.rotation=Quaternion.LookRotation((b-a).normalized,Vector3.up);
            Piece(root,"Post",new Vector3(0,.5f,-HexWorldGrid.Radius*.5f),new Vector3(.14f,1,.14f),!ghost);Piece(root,"Post",new Vector3(0,.5f,HexWorldGrid.Radius*.5f),new Vector3(.14f,1,.14f),!ghost);
            var leaf=new GameObject("Rails");leaf.transform.SetParent(root.transform,false);
            if(f.gate&&f.open){leaf.transform.localPosition=new Vector3(0,0,-HexWorldGrid.Radius*.5f);leaf.transform.localRotation=Quaternion.Euler(0,85,0);}
            for(int i=0;i<2;i++)Piece(leaf,"Rail",new Vector3(0,.35f+i*.35f,f.gate&&f.open?HexWorldGrid.Radius*.5f:0),new Vector3(.1f,.12f,HexWorldGrid.Radius),false);
            if(!ghost){var target=root.AddComponent<FenceTarget>();target.record=f;var col=root.AddComponent<BoxCollider>();col.center=new Vector3(0,.5f,0);col.size=new Vector3(.15f,1,HexWorldGrid.Radius);col.isTrigger=f.gate&&f.open;}
            return root;
        }
        void Piece(GameObject root,string name,Vector3 p,Vector3 s,bool solid)
        {var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(root.transform,false);go.transform.localPosition=p;go.transform.localScale=s;go.GetComponent<Renderer>().sharedMaterial=timber;go.GetComponent<Collider>().enabled=solid;}
    }
}
