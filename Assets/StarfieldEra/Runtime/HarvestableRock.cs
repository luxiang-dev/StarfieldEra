using UnityEngine;
namespace StarfieldEra
{
    public sealed class HarvestableRock:MonoBehaviour
    {
        public bool Depleted {get;internal set;}
        public Transform source;
        public static void Track(Transform root)
        {
            var rock=root.GetComponent<HarvestableRock>();if(!rock)rock=root.gameObject.AddComponent<HarvestableRock>();
            var game=root.GetComponentInParent<FarmGame>();if(game&&game.Mining)game.Mining.Register(rock);
        }
        public void Hide()
        {
            Depleted=true;
            if(source)source.gameObject.SetActive(false);
            gameObject.SetActive(false);
        }
    }
}
