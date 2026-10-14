using UnityEngine;
namespace StarfieldEra
{
    /// <summary>Deterministic landform heights. Hex coordinates and save files stay two-dimensional.</summary>
    public static class WorldRelief
    {
        static float Terrace(Vector3 p,float x0,float x1,float z0,float z1,float skirt)
        {
            float dx=Mathf.Max(x0-p.x,0,p.x-x1),dz=Mathf.Max(z0-p.z,0,p.z-z1);
            return 1-Mathf.SmoothStep(0,1,Mathf.Max(dx,dz)/skirt);
        }
        static float Hill(Vector3 p,float x,float z,float radius,float height)
        {float d=new Vector2(p.x-x,p.z-z).magnitude/radius;return height*(1-Mathf.SmoothStep(0,1,d));}
        public static float LandHeight(Vector3 p)
        {
            float h=.18f+.07f*Mathf.Sin(p.x*.2f)*Mathf.Sin(p.z*.18f);
            h+=Hill(p,17,1,13,1.45f)+Hill(p,-19,-22,11,2.15f)+Hill(p,-30,22,11,1.35f);
            h+=Hill(p,28,28,18,4.2f)+Hill(p,32,32,9,3.2f);
            h=Mathf.Lerp(h,.85f,Terrace(p,-1,8,-9,2,2.7f));
            h=Mathf.Lerp(h,1.85f,Terrace(p,-11,1,4.5f,14,2.5f));
            h=Mathf.Lerp(h,3.45f,Terrace(p,4,17,11,19,4));
            // Foundations are broad flat pads with approach ramps, rather than tilted buildings.
            h=Mathf.Lerp(h,1.85f,Terrace(p,-7.1f,-2.9f,3.1f,7.3f,2.8f));
            h=Mathf.Lerp(h,2.1f,Terrace(p,-16,-12,11.5f,15.5f,2));
            h=Mathf.Lerp(h,.85f,Terrace(p,-3,1,-14.5f,-10.5f,2));
            return h;
        }
        public static float PlanarDistance(Vector3 a,Vector3 b)=>new Vector2(a.x-b.x,a.z-b.z).magnitude;
    }
}
