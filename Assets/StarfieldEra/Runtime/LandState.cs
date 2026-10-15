using System;
using System.Collections.Generic;
using UnityEngine;
namespace StarfieldEra
{
    [Serializable] public sealed class LandCell
    {
        public int column,row,original,kind,treeDay,treeGrowth,dryDays;
        public bool channel,tilled,tree,treeWatered;
        public CropState crop=new CropState();
        public Vector2Int Cell=>new Vector2Int(column,row);
    }
    [Serializable] public sealed class LandState
    {
        public int soil;
        public List<LandCell> cells=new List<LandCell>();
        public LandCell Find(Vector2Int cell)=>cells.Find(x=>x.column==cell.x&&x.row==cell.y);
        public void Upgrade(){if(cells==null)cells=new List<LandCell>();foreach(var c in cells)if(c.crop==null)c.crop=new CropState();}
        public bool Valid()
        {
            if(soil<0||cells==null||cells.Count>512)return false;var seen=new HashSet<Vector2Int>();
            foreach(var c in cells)if(c==null||!seen.Add(c.Cell)||c.column< -23||c.column>22||c.row< -20||c.row>20||c.kind<0||c.kind>7||c.original<0||c.original>7||c.dryDays<0||c.treeDay<0||c.treeGrowth<0||c.treeGrowth>3||c.crop==null||c.crop.kind<0||c.crop.kind>2||c.crop.growth<0||c.crop.growth>c.crop.RequiredDays||(!c.crop.planted&&(c.crop.watered||c.crop.growth!=0)))return false;
            return true;
        }
    }
}
