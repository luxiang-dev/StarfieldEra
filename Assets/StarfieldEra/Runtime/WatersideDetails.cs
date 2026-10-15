using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace StarfieldEra
{
    public sealed class WatersideDetails:MonoBehaviour
    {
        readonly List<Object> owned=new List<Object>();FarmWorld world;
        Material timber,iron,glow;Transform root;
        public void Build(FarmWorld owner)
        {
            world=owner;root=new GameObject("Waterside atmosphere").transform;root.SetParent(transform,false);
            timber=Create("Weathered dock timber","StarfieldEra/Weathered Dock",new Color(.42f,.28f,.15f));iron=Create("Dock iron","StarfieldEra/Storybook Models",new Color(.12f,.15f,.13f));glow=Create("Warm lantern glass","StarfieldEra/Storybook Models",new Color(1,.62f,.17f));glow.SetColor("_EmissionColor",new Color(4,1.7f,.32f));
            Dock();Lantern(world.Door+new Vector3(-1.65f,0,-.35f),"Farmhouse lantern");Lantern(new Vector3(-10.2f,0,-7.9f),"Jetty lantern");
            var gate=new Vector3(10.8f,0,16);var lamp=new GameObject("Violet ancient gate light",typeof(Light)).GetComponent<Light>();lamp.transform.SetParent(root,false);lamp.transform.position=world.HexGrid.SurfacePoint(gate,1.4f);lamp.type=LightType.Point;lamp.color=new Color(.63f,.31f,1);lamp.range=4.5f;lamp.intensity=1.3f;lamp.shadows=LightShadows.None;
            var violet=Create("Ancient gate emissive rune","StarfieldEra/Storybook Models",new Color(.53f,.28f,.78f));violet.SetColor("_EmissionColor",new Color(1.2f,.3f,2.2f));
            foreach(var renderer in world.GetComponentsInChildren<Renderer>())if(renderer.name=="Crystal spire")renderer.sharedMaterial=violet;
        }
        Material Create(string name,string shader,Color color){var m=new Material(Shader.Find(shader)){name=name};m.SetColor("_BaseColor",color);owned.Add(m);return m;}
        Transform Piece(string name,PrimitiveType shape,Vector3 position,Vector3 scale,Material material)
        {var t=world.Shape(name,shape,position,scale,material);t.SetParent(root,true);return t;}
        void Dock()
        {
            foreach(var renderer in world.GetComponentsInChildren<Renderer>())if(renderer.name=="Jetty plank")renderer.enabled=false;
            var surface=world.HexGrid.GetComponent<ContinuousTerrain>();float water=surface.WaterLevelAt(new Vector3(-11.5f,0,-9.3f));float deck=water+.24f;timber.SetFloat("_Waterline",water);
            for(int i=0;i<8;i++)
            {
                float z=-8.3f-i*.34f;var plank=Piece("Weathered jetty board",PrimitiveType.Cube,new Vector3(-11.5f,deck+(i%3-1)*.007f,z),new Vector3(2.15f,.13f,.305f),timber);plank.Rotate(0,(i%3-1)*.55f,0);
                for(int side=-1;side<=1;side+=2)Piece("Iron plank nail",PrimitiveType.Cylinder,new Vector3(-11.5f+side*.86f,deck+.074f,z),new Vector3(.045f,.006f,.045f),iron);
            }
            for(int side=-1;side<=1;side+=2)
            {
                Piece("Dock underbeam",PrimitiveType.Cube,new Vector3(-11.5f+side*.82f,deck-.16f,-9.49f),new Vector3(.16f,.18f,2.85f),timber);
                for(int end=0;end<2;end++)
                {
                    var p=new Vector3(-11.5f+side*1.03f,water-.04f,end==0?-8.3f:-10.68f);Piece("Water stained support post",PrimitiveType.Cylinder,p,new Vector3(.19f,.61f,.19f),timber);
                    Piece("Dock post iron collar",PrimitiveType.Cylinder,p+Vector3.up*.37f,new Vector3(.20f,.035f,.20f),iron);Ripple(new Vector3(p.x,water+.015f,p.z),side+end*2);
                }
            }
        }
        void Ripple(Vector3 p,float phase)
        {
            var mat=new Material(Shader.Find("StarfieldEra/Dock Ripples"));mat.SetFloat("_Phase",phase);owned.Add(mat);
            var ring=Piece("Water ripples around timber",PrimitiveType.Quad,p,new Vector3(.95f,.95f,1),mat);ring.rotation=Quaternion.Euler(90,0,0);ring.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
        }
        void Lantern(Vector3 position,string name)
        {
            position=world.HexGrid.SurfacePoint(position);Piece(name+" post",PrimitiveType.Cylinder,position+Vector3.up*.8f,new Vector3(.095f,.8f,.095f),timber);
            Piece(name+" frame",PrimitiveType.Cube,position+new Vector3(.18f,1.54f,0),new Vector3(.34f,.40f,.30f),iron);
            Piece(name+" glass",PrimitiveType.Cube,position+new Vector3(.18f,1.54f,-.16f),new Vector3(.23f,.27f,.035f),glow);
            var light=new GameObject(name+" warm point light",typeof(Light)).GetComponent<Light>();light.transform.SetParent(root,false);light.transform.position=position+new Vector3(.18f,1.55f,-.35f);light.type=LightType.Point;light.range=4.3f;light.intensity=1.7f;light.color=new Color(1,.57f,.24f);light.shadows=LightShadows.None;
        }
        void OnDestroy(){foreach(var item in owned)if(item)Destroy(item);}
    }
}
