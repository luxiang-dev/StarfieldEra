using UnityEngine;
namespace StarfieldEra
{
    /// <summary>Normalized local models share meshes/materials and leave gameplay on existing anchors.</summary>
    public static class LocalEnvironment
    {
        public static Transform Tree(Transform parent,Vector3 position,float height,float yaw=0)
            {var tree=Place("Environment/PineTree",parent,position,height,0,yaw,"Local pine tree");if(tree)HarvestableTree.Track(tree);return tree;}
        public static Transform House(Transform parent,Vector3 position,float height,float width=5)
            {var house=Place("Environment/ResidentHouse",parent,position,height,width,0,"Local resident house");if(house)house.rotation=parent.rotation;return house;}
        public static Transform Rock(Transform parent,Vector3 position,float height,float yaw=0)
            {var rock=Place("Environment/FieldRock",parent,position,height,height*2.5f,yaw,"Local moss rock");if(rock)HarvestableRock.Track(rock);return rock;}
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
        public static Transform Rabbit(Transform parent,Vector3 position)
            =>Place("Environment/MeadowRabbit",parent,position,.92f,1.15f,0,"Local meadow rabbit");
        public static Transform Wolf(Transform parent,Vector3 position)
            =>Place("Environment/NightWolf",parent,position,.95f,2.1f,0,"Night raid wolf");
        public static Transform Pillar(Transform parent,Vector3 position,float height,float yaw=0)
            =>Place("Environment/AncientPillar",parent,position,height,1.8f,yaw,"Local ancient pillar");
        public static Transform Workshop(Transform parent,Vector3 position)
        {var visual=Place("Environment/Workshop",parent,position,3.1f,4.6f,0,"Local workshop");if(visual)visual.rotation=parent.rotation;return visual;}
        public static Transform Warehouse(Transform parent,Vector3 position,float height,float width)
        {var visual=Place("Environment/Warehouse",parent,position,height,width,0,"Local warehouse");if(visual)visual.rotation=parent.rotation;return visual;}
        public static Transform TownBoard(Transform parent,Vector3 position)
            =>Place("Environment/TownBoard",parent,position,2.15f,2.6f,0,"Local town board");
        public static Transform Trap(Transform parent,Vector3 position,bool spent=false)
        {
            var visual=Place("Environment/AnimalTrap",parent,position,.48f,1.05f,0,"Local animal trap");
            if(visual&&spent)
            {
                visual.localScale=new Vector3(visual.localScale.x,visual.localScale.y*.45f,visual.localScale.z);
                var tint=new MaterialPropertyBlock();tint.SetColor("_BaseColor",new Color(.43f,.39f,.34f,1));
                foreach(var renderer in visual.GetComponentsInChildren<Renderer>())renderer.SetPropertyBlock(tint);
            }
            return visual;
        }
        public static Transform Gate(Transform parent,float span,bool open)
        {
            var prefab=Resources.Load<GameObject>("Environment/PastureGate");if(!prefab)return null;
            var art=Object.Instantiate(prefab,parent).transform;art.name="Local pasture gate";
            art.localScale=new Vector3(PastureFences.SegmentLength,PastureFences.SegmentLength,span);
            if(open)foreach(var part in art.GetComponentsInChildren<Transform>())
            {
                bool start=part.name=="GateLeafStart";if(!start&&part.name!="GateLeafEnd")continue;
                var hinge=new GameObject(start?"Gate hinge start":"Gate hinge end").transform;
                hinge.SetParent(parent,false);hinge.localPosition=new Vector3(0,0,(start?-.43f:.43f)*span);
                part.SetParent(hinge,true);hinge.localRotation=Quaternion.Euler(0,start?85:-85,0);
            }
            return art;
        }
        public static Transform Camp(Transform parent,Vector3 position,float height=2.6f,float width=3.2f)
        {
            var visual=Place("Environment/Camp",parent,position,height,width,0,"Local camp");
            if(visual)visual.rotation=parent.rotation;return visual;
        }
        public static Transform ProductionBuilding(int kind,Transform parent,Vector3 position,float height,float width)
        {
            if(kind>1)return null;
            var visual=Place("Environment/"+(kind==0?"Lumberyard":"Quarry"),parent,position,height,width,0,kind==0?"Local lumberyard":"Local quarry");
            if(visual)visual.rotation=parent.rotation;return visual;
        }
        public static bool HasBuildingVisual(Transform root)
        {
            foreach(var node in root.GetComponentsInChildren<Transform>(true))if(node.name=="Local lumberyard"||node.name=="Local quarry"||node.name=="Local workshop"||node.name=="Local camp"||node.name=="Local resident house"||node.name=="Local warehouse")return true;
            return false;
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
