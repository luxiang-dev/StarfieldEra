using System;
using System.Collections.Generic;
using UnityEngine;
namespace StarfieldEra
{
    [Serializable] public sealed class MiningState
    {
        public List<Vector2Int> depleted=new List<Vector2Int>();
        public bool Contains(Vector3 p)=>depleted.Contains(ForestryState.Key(p));
        public bool Mine(Vector3 p)
        {
            var key=ForestryState.Key(p);
            if(depleted.Count>=4096||depleted.Contains(key)||Math.Abs((long)key.x)>HexWorldGrid.HalfWidth*100||Math.Abs((long)key.y)>HexWorldGrid.HalfDepth*100)return false;
            depleted.Add(key);return true;
        }
        public bool Valid()
        {
            if(depleted==null||depleted.Count>4096)return false;var seen=new HashSet<Vector2Int>();
            foreach(var key in depleted)if(!seen.Add(key)||Math.Abs((long)key.x)>HexWorldGrid.HalfWidth*100||Math.Abs((long)key.y)>HexWorldGrid.HalfDepth*100)return false;
            return true;
        }
    }
}
