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
        public static Transform Rock(Transform parent,Vector3 position,float height,float yaw=0)
            =>Place("Environment/FieldRock",parent,position,height,height*2.5f,yaw,"Local moss rock");
        public static Transform Chest(Transform parent,Vector3 position)
            =>Place("Environment/SupplyChest",parent,position,.85f,1.4f,0,"Local supply chest");
        public static Transform Character(string resource,Transform parent,Vector3 position,float height)
        {
            var prefab=Resources.Load<GameObject>("Environment/"+resource);
            // Starfarer has an authored 1 m prefab. Animated renderer bounds include
            // swing poses and must not be used to lift or resize the standing actor.
            if(prefab && prefab.GetComponent<HeroFootContact>())
            {
                var root=Object.Instantiate(prefab,parent).transform;root.name="Local "+resource;
                root.localScale=Vector3.one*height;root.position=position;root.localRotation=Quaternion.identity;return root;
            }
            return Place("Environment/"+resource,parent,position,height,0,0,"Local "+resource);
        }
        public static Transform Fence(Transform parent,Vector3 a,Vector3 b,bool hideStart=false)
        {
            var prefab=Resources.Load<GameObject>("Environment/FantasyFence");if(!prefab)return null;
            var root=Object.Instantiate(prefab,parent).transform;root.name="Local painted fence";
            root.position=(a+b)*.5f;root.rotation=Quaternion.LookRotation((b-a).normalized);root.localScale=Vector3.one*WorldRelief.PlanarDistance(a,b);
            if(hideStart)foreach(var part in root.GetComponentsInChildren<Transform>())if(part.name=="FencePostStart")part.gameObject.SetActive(false);
            return root;
        }
        public static bool ReplaceRock(Transform anchor,float height)
        {
            if(!Resources.Load<GameObject>("Environment/FieldRock"))return false;
            foreach(var r in anchor.GetComponentsInChildren<Renderer>())if(!r.GetComponent<TMPro.TMP_Text>())r.enabled=false;
            return Rock(anchor,anchor.position,height,anchor.position.x*37)!=null;
        }
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
