using UnityEngine;
namespace StarfieldEra
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TerrainMapGraphic : UnityEngine.UI.MaskableGraphic
    {
        public HexWorldGrid grid;
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper helper)
        {
            helper.Clear();if(!grid)return;var rect=rectTransform.rect;
            for(int row=HexWorldGrid.MinRow;row<=HexWorldGrid.MaxRow;row++)for(int column=HexWorldGrid.MinColumn;column<=HexWorldGrid.MaxColumn;column++)
            {
                var cell=new Vector2Int(column,row);if(!grid.Contains(cell))continue;
                var p=HexWorldGrid.Center(column,row);ColorUtility.TryParseHtmlString("#"+TerrainTraits.Colors[(int)grid.TerrainAt(cell)],out var shade);
                int start=helper.currentVertCount;helper.AddVert(Map(p,rect),shade,Vector2.zero);
                for(int i=0;i<6;i++)helper.AddVert(Map(p+HexWorldGrid.Corner(i,HexWorldGrid.Radius*.95f),rect),shade,Vector2.zero);
                for(int i=0;i<6;i++)helper.AddTriangle(start,start+1+i,start+1+(i+1)%6);
            }
        }
        static Vector2 Map(Vector3 p,Rect rect)=>rect.center+new Vector2(p.x/(HexWorldGrid.HalfWidth*2)*rect.width,p.z/(HexWorldGrid.HalfDepth*2)*rect.height);
    }
}
