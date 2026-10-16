using System;
using System.Collections.Generic;
using UnityEngine;
namespace StarfieldEra
{
    [Serializable] public sealed class LandCell
    {
        public int column,row,original,kind,treeDay,treeGrowth,dryDays;
        public bool channel,tilled,tree,treeWatered,drainage;
        public bool elevationEdited;
        public float elevation;
        public CropState crop=new CropState();
        public Vector2Int Cell=>new Vector2Int(column,row);
    }
    [Serializable] public sealed class LandState
    {
        public int soil;
        public List<LandCell> cells=new List<LandCell>();
        [NonSerialized] Dictionary<Vector2Int,LandCell> lookup;
        [NonSerialized] int indexedCount=-1;
        public LandCell Find(Vector2Int cell)
        {
            if(lookup==null||indexedCount!=cells.Count){lookup=new Dictionary<Vector2Int,LandCell>();foreach(var c in cells)if(c!=null)lookup[c.Cell]=c;indexedCount=cells.Count;}
            return lookup.TryGetValue(cell,out var found)?found:null;
        }
        public void Upgrade(){if(cells==null)cells=new List<LandCell>();foreach(var c in cells)if(c.crop==null)c.crop=new CropState();}
        public bool Valid()
        {
            if(soil<0||cells==null||cells.Count>2048)return false;var seen=new HashSet<Vector2Int>();
            foreach(var c in cells)if(c==null||!seen.Add(c.Cell)||c.column<HexWorldGrid.MinColumn||c.column>HexWorldGrid.MaxColumn||c.row<HexWorldGrid.MinRow||c.row>HexWorldGrid.MaxRow||float.IsNaN(c.elevation)||float.IsInfinity(c.elevation)||c.elevation<0||c.elevation>12||c.kind<0||c.kind>7||c.original<0||c.original>7||c.dryDays<0||c.treeDay<0||c.treeGrowth<0||c.treeGrowth>3||c.crop==null||float.IsNaN(c.crop.growthBonus)||float.IsInfinity(c.crop.growthBonus)||c.crop.growthBonus<0||c.crop.growthBonus>=1||c.crop.kind<0||c.crop.kind>3||c.crop.growth<0||c.crop.growth>c.crop.RequiredDays||(!c.crop.planted&&(c.crop.watered||c.crop.growth!=0||c.crop.growthBonus!=0)))return false;
            return true;
        }
    }
}
