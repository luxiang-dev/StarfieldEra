using UnityEngine;
namespace StarfieldEra
{
    /// <summary>Saved absolute heights with compact smooth shoulders; untouched areas retain natural relief.</summary>
    public static class LandElevation
    {
        public static float Height(Vector3 p,LandState state)
        {
            float natural=WorldRelief.LandHeight(p);if(state==null||state.cells.Count==0)return natural;
            int row=Mathf.RoundToInt((p.z-.15f)/HexWorldGrid.RowSpacing);float sum=0,target=0;
            for(int r=row-2;r<=row+2;r++)
            {
                int col=Mathf.RoundToInt((p.x-.5f)/HexWorldGrid.ColumnSpacing-.5f*(r&1));
                for(int c=col-2;c<=col+2;c++)
                {
                    var edit=state.Find(new Vector2Int(c,r));if(edit==null||!edit.elevationEdited)continue;
                    float d=WorldRelief.PlanarDistance(p,HexWorldGrid.Center(c,r));
                    float weight=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.35f,1.95f,d));
                    sum+=weight;target+=weight*edit.elevation;
                }
            }
            return sum>0?Mathf.Lerp(natural,target/sum,Mathf.Clamp01(sum)):natural;
        }
    }
}
