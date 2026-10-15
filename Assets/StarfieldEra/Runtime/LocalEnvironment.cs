using UnityEngine;
namespace StarfieldEra
{
    /// <summary>Normalized local models share meshes/materials and leave gameplay on existing anchors.</summary>
    public static class LocalEnvironment
    {
        public static Transform Tree(Transform parent,Vector3 position,float height,float yaw=0)
            =>Place("Environment/PineTree",parent,position,height,0,yaw,"Local pine tree");
        public static Transform House(Transform parent,Vector3 position,float height,float width=5)
            =>Place("Environment/ResidentHouse",parent,position,height,width,0,"Local resident house");
        static Transform Place(string resource,Transform parent,Vector3 position,float height,float maxWidth,float yaw,string name)
        {
            var prefab=Resources.Load<GameObject>(resource);if(!prefab)return null;
            var root=Object.Instantiate(prefab,parent).transform;root.name=name;root.position=position;root.rotation=Quaternion.Euler(0,yaw,0);
            var renderers=root.GetComponentsInChildren<Renderer>();if(renderers.Length==0){Object.Destroy(root.gameObject);return null;}
            var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
            float scale=height/Mathf.Max(.001f,bounds.size.y);if(maxWidth>0)scale=Mathf.Min(scale,maxWidth/Mathf.Max(.001f,Mathf.Max(bounds.size.x,bounds.size.z)));
            root.localScale*=scale;root.position+=new Vector3(position.x-bounds.center.x,position.y-bounds.min.y,position.z-bounds.center.z)*scale;
            return root;
        }
    }
}
