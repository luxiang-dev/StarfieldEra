using System;
using System.Collections.Generic;
using UnityEngine;
namespace StarfieldEra
{
    [Serializable] public sealed class ForestryState
    {
        public List<Vector2Int> felled=new List<Vector2Int>();
        public static Vector2Int Key(Vector3 p)=>new Vector2Int(Mathf.RoundToInt(p.x*100),Mathf.RoundToInt(p.z*100));
        public bool Contains(Vector3 p)=>felled.Contains(Key(p));
        public bool Fell(Vector3 p){var key=Key(p);if(felled.Count>=4096||felled.Contains(key))return false;felled.Add(key);return true;}
        public bool Valid()
        {
            if(felled==null||felled.Count>4096)return false;var seen=new HashSet<Vector2Int>();
            foreach(var key in felled)if(!seen.Add(key)||Math.Abs((long)key.x)>HexWorldGrid.HalfWidth*100||Math.Abs((long)key.y)>HexWorldGrid.HalfDepth*100)return false;
            return true;
        }
    }
}
