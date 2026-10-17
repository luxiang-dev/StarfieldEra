using System.Collections.Generic;
using UnityEngine;

namespace StarfieldEra
{
    public static class BuildingFootprints
    {
        public static readonly int[] Counts={7,7,4,3,4,4,7,4,7,4,4,4};
        static readonly Vector2Int[] Ring={new Vector2Int(0,0),new Vector2Int(1,0),new Vector2Int(1,-1),new Vector2Int(0,-1),new Vector2Int(-1,0),new Vector2Int(-1,1),new Vector2Int(0,1)};
        static readonly Vector2Int[] School={new Vector2Int(0,0),new Vector2Int(1,0),new Vector2Int(0,1),new Vector2Int(1,1)};
        static readonly Vector2Int[] Camp={new Vector2Int(0,0),new Vector2Int(1,0),new Vector2Int(0,1)};
        public static List<Vector2Int> Cells(PlacedBuilding building) => building.footprintVersion==0
            ? new List<Vector2Int>{new Vector2Int(building.column,building.row)}
            : Cells(new Vector2Int(building.column,building.row),building.kind,building.rotation);
        public static List<Vector2Int> Cells(Vector2Int anchor,int kind,int facing)
        {
            var offsets=(kind<2||kind==6||kind==8)?Ring:(kind==2||kind>=4)?School:Camp;
            int q=anchor.x-(anchor.y-(anchor.y&1))/2,r=anchor.y;
            var result=new List<Vector2Int>();
            foreach(var offset in offsets)
            {
                int x=offset.x,z=offset.y;
                for(int i=0;i<facing;i++){int next=-z;z=x+z;x=next;}
                int row=r+z,column=q+x+(row-(row&1))/2;
                result.Add(new Vector2Int(column,row));
            }
            return result;
        }
        public static bool Valid(List<PlacedBuilding> buildings)
        {
            var claimed=new HashSet<Vector2Int>();
            foreach(var building in buildings)
            {
                if(building.footprintVersion<0||building.footprintVersion>1||building.builtDay<0)return false;
                foreach(var cell in Cells(building))
                {
                    var center=HexWorldGrid.Center(cell.x,cell.y);
                    if(cell.y<HexWorldGrid.MinRow||cell.y>HexWorldGrid.MaxRow||cell.x<HexWorldGrid.MinColumn||cell.x>HexWorldGrid.MaxColumn||Mathf.Abs(center.x)>HexWorldGrid.HalfWidth-1||Mathf.Abs(center.z)>HexWorldGrid.HalfDepth-1||!claimed.Add(cell))return false;
                }
            }
            return true;
        }
    }

    public sealed class BuildingInspectable : MonoBehaviour
    {
        public PlacedBuilding record;
        public string displayName;
        public int kind=-1;
        [TextArea] public string description;
    }
}
