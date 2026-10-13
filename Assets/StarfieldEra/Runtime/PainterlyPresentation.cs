using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace StarfieldEra
{
    /// <summary>Art direction layered over the playable world, without changing saved gameplay.</summary>
    public sealed class PainterlyPresentation : MonoBehaviour
    {
        VolumeProfile profile;
        public void Build(FarmWorld world)
        {
            var cameraData = world.View.GetComponent<UniversalAdditionalCameraData>() ?? world.View.gameObject.AddComponent<UniversalAdditionalCameraData>();
            cameraData.renderPostProcessing = true;
            cameraData.volumeLayerMask = 1;
            world.View.allowHDR = true;
            var volume = gameObject.AddComponent<Volume>(); volume.isGlobal = true; volume.priority = 10;
            profile = ScriptableObject.CreateInstance<VolumeProfile>(); volume.sharedProfile = profile;
            profile.Add<Tonemapping>(true).mode.value = TonemappingMode.Neutral;
            var colors = profile.Add<ColorAdjustments>(true);
            colors.contrast.value = 12; colors.saturation.value = 8; colors.postExposure.value = .1f;
            profile.Add<WhiteBalance>(true).temperature.value = 4;
            var bloom = profile.Add<Bloom>(true); bloom.threshold.value = 1.2f; bloom.intensity.value = .18f; bloom.scatter.value = .45f;
            var vignette = profile.Add<Vignette>(true); vignette.intensity.value = .14f; vignette.smoothness.value = .7f;

            var random = new System.Random(815);
            var art = StoreVisuals.Current;
            var straw = world.Mat("Warm straw", "B58B44");
            var timber = world.Mat("Weathered wood", "735039");
            var plaster = world.Mat("Cream masonry", "DCCBA6");
            var roof = world.Mat("Roof tile clay", "C97648");
            var roofLight = world.Mat("Roof tile light", "DF8C56");
            var darkStone = world.Mat("Ruins joints", "7D8D79");
            var ivy = world.Mat("Ruins ivy", "64833F");
            var ocean = world.Mat("Surrounding sea", "389DAB");
            world.Shape("Island sea", PrimitiveType.Cube, new Vector3(0,-.85f,0),new Vector3(170,.12f,150),ocean);

            // Break up the flat field with grass and flowers, leaving interaction paths clear.
            if (art)
            for (int i=0;i<420;i++)
            {
                float x=-19+(float)random.NextDouble()*38, z=-16+(float)random.NextDouble()*32;
                if ((x>-8 && x<8.8f && z>-5 && z<7) || (x<-7 && z<-4) || (z>7 && x<3) || (x>3 && z>9)) continue;
                var prefab=i%4==0?art.flower:art.grass;
                art.Place(prefab,transform,new Vector3(x,.025f,z),i%4==0?.7f:.5f,random.Next(360));
            }

            var petal = world.Mat("Daisy petals", "E9DEAE");
            var pollen = world.Mat("Daisy centers", "D4A13D");
            for(int i=0;i<42;i++)
            {
                var pos = i<22 ? new Vector3(-1.5f+i*.45f,0,-5.3f+(float)random.NextDouble()*.45f) : new Vector3(-7.7f+(float)random.NextDouble()*1.1f,0,1.4f+(i-22)*.38f);
                world.Shape("Flower stem",PrimitiveType.Cylinder,pos+Vector3.up*.16f,new Vector3(.035f,.16f,.035f),ivy);
                for(int j=0;j<5;j++)
                {
                    float angle=j*Mathf.PI*2/5;
                    world.Shape("Daisy petal",PrimitiveType.Sphere,pos+new Vector3(Mathf.Cos(angle)*.1f,.33f,Mathf.Sin(angle)*.1f),new Vector3(.15f,.07f,.15f),petal);
                }
                world.Shape("Daisy pollen",PrimitiveType.Sphere,pos+Vector3.up*.35f,new Vector3(.09f,.07f,.09f),pollen);
            }

            // Individually laid clay roof tiles and raised stonework on the existing farmhouse.
            for(int side=0;side<2;side++)
            for(int row=0;row<5;row++)
            for(int col=0;col<7;col++)
            {
                float dx=.22f+row*.38f;
                var tile=world.Shape("Clay roof tile",PrimitiveType.Cube,new Vector3(-5+(side==0?-dx:dx),3.74f-dx*.577f,3.7f+col*.52f),new Vector3(.45f,.13f,.55f),(row+col)%3==0?roofLight:roof);
                tile.rotation=Quaternion.Euler(0,0,side==0?30:-30);
            }
            for(int row=0;row<5;row++)
            for(int col=0;col<6;col++)
            {
                float x=-6.65f+col*.58f+(row%2)*.15f;
                if(x>-5.6f && x<-4.4f && row<3)continue;
                if(x<-5.75f && row>1 && row<4)continue;
                world.Shape("Farmhouse stonework",PrimitiveType.Cube,new Vector3(x,.28f+row*.48f,3.775f),new Vector3(.51f,.4f,.045f),plaster);
            }
            for(int i=0;i<4;i++)world.Shape("Door planks",PrimitiveType.Cube,new Vector3(-5.32f+i*.21f,.75f,3.70f),new Vector3(.18f,1.38f,.06f),timber);
            world.Shape("Door latch",PrimitiveType.Sphere,new Vector3(-4.75f,.8f,3.65f),Vector3.one*.1f,straw);
            for(int i=0;i<3;i++)
            {
                var pos=new Vector3(-7.4f+i*.7f,.35f,4.3f);
                world.Shape("Porch crate",PrimitiveType.Cube,pos,new Vector3(.6f,.65f,.6f),timber);
                world.Shape("Crate strap",PrimitiveType.Cube,pos+new Vector3(0,0,-.31f),new Vector3(.61f,.09f,.035f),straw);
            }

            var shadow = world.Mat("Soft contact shadow", "1E3024");
            var shadowShader = Shader.Find("StarfieldEra/Contact Shadow");
            if(shadowShader)
            {
                shadow.shader=shadowShader;shadow.color=new Color(.08f,.13f,.09f,.4f);
                var anchors=world.GetComponentsInChildren<Transform>();
                foreach(var anchor in anchors)
                    if(anchor.name.StartsWith("Store art - PT_Pine_Tree")) GroundShadow(world,anchor.position,1.6f,1.3f,shadow);
                GroundShadow(world,new Vector3(-5,0,5.3f),5.1f,4.4f,shadow);
                for(int i=0;i<6;i++)GroundShadow(world,new Vector3(7.5f+(i%3)*2.6f,0,12+(i/3)*3.5f),1.8f,1.5f,shadow);
            }

            // Joints, weathering and climbing plants give the ruins a more inhabited silhouette.
            for(int i=0;i<6;i++)
            {
                var pos=new Vector3(7.5f+(i%3)*2.6f,0,12+(i/3)*3.5f);
                float height=2.2f+i%2*.8f;
                for(int j=1;j<height/.48f;j++)
                    world.Shape("Column stone joint",PrimitiveType.Cylinder,pos+Vector3.up*(j*.48f),new Vector3(.81f,.016f,.81f),darkStone);
                for(int j=0;j<6;j++)
                {
                    var leaf=world.Shape("Climbing ivy",PrimitiveType.Sphere,pos+new Vector3(.32f+Mathf.Sin(j)*.12f,.4f+j*.36f,-.31f),new Vector3(.28f,.12f,.25f),ivy);
                    leaf.rotation=Quaternion.Euler(15,j*35,20);
                }
                world.Shape("Ruins rubble",PrimitiveType.Cube,pos+new Vector3(-.7f,.15f,-.3f),new Vector3(.55f,.28f,.4f),plaster).rotation=Quaternion.Euler(0,i*29,8);
            }
            for(int i=0;i<10;i++)
                world.Shape("Portal masonry",PrimitiveType.Cube,new Vector3(7.8f+i*.49f,3.34f,16),new Vector3(.46f,.17f,1.05f),plaster);
        }
        static void GroundShadow(FarmWorld world,Vector3 position,float width,float depth,Material material)
        {
            var quad=world.Shape("Soft ground contact",PrimitiveType.Quad,new Vector3(position.x,.018f,position.z),new Vector3(width,depth,1),material);
            quad.rotation=Quaternion.Euler(90,0,0);
        }
        void OnDestroy()
        {
            if (!profile) return;
            foreach(var component in profile.components)if(component)Destroy(component);
            Destroy(profile);
        }
    }
}
