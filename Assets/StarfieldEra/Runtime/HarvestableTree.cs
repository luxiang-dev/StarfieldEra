using UnityEngine;
namespace StarfieldEra
{
    public sealed class HarvestableTree:MonoBehaviour
    {
        public bool planted,Felled;
        public Vector2Int cell;
        public int hits;
        public static void Track(Transform root)
        {
            var tree=root.GetComponent<HarvestableTree>();if(!tree)tree=root.gameObject.AddComponent<HarvestableTree>();
            var game=root.GetComponentInParent<FarmGame>();if(game&&game.Forestry)game.Forestry.Register(tree);if(game&&game.Inspection)WorldInspection.Volume(root);
        }
    }
}
