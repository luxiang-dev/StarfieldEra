using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
namespace StarfieldEra
{
    /// <summary>Saved hex edits, source-connected waterways and local soil stewardship.</summary>
    public sealed class LandStewardship:MonoBehaviour
    {
        public static readonly string[] Names={"开垦土地","植树造林","开挖河道","填埋土地","覆草复绿","查看土地","削低山丘","填土造丘","修排水沟"};
        public static readonly string[] Costs={"体力2","体力2 · 木材2 · 浆果1","体力3 · 获得土方3","体力3 · 土方2 · 石料2","体力2 · 纤维2","免费","体力3 · 获得土方2","体力3 · 土方2","体力3 · 石料1"};
        public static readonly string[] Descriptions={
            "将草地或沙地开垦为新的农田。\n回到地块旁播种、收获或浇水。\n使用背包中当前选择的种子。",
            "种下树苗，滋养三天后长成林木。\n成树为两格以内的土地保湿固土。\n沙地上的树苗需要水源或雨水滋养，也可手动浇水。",
            "挖出低洼河床并获得土方。\n河道连通水源后才会出现水流。\n从湖岸向外逐格开挖，自动按水面降低河床。\n水面不可行走，可灌溉邻接农田。",
            "用土方和石料填平水面或干河道。\n填后变为沙地，可再覆草或植树。\n填断水路会让下游人工河道干涸。",
            "将裸露沙土覆草恢复成草地。\n水源和树木能帮助保持土地湿润。\n无水无树的裸露地与新开农田，\n持续干旱后会沙化；有作物时减缓。",
            "查看地形、土壤湿度、干旱天数、\n河道水源与树木生长状态。\n新农田也可直接播种或收获。",
            "每次将地面削低半米并获得土方2。\n可连续施工，将山丘逐步挖平。\n最低高度为0.2米，边缘自动过渡成缓坡。\n需要铁锹，避开建筑、农田、水岸和动物。",
            "每次消耗土方2，将地面抬高半米。\n可连续填土，将平地堆成山丘，最高12米。\n挖山取得的土方可以搬到这里使用。\n需要铁锹，施工结果自动保存。",
            "在空陆地修建排水沟，暴雨前布置。\n与水源或低处连通后，持续排走两格内积水。\n接通后水位持续下降，无需等待过夜。\n再次点击已有排水沟可移除。"};
        FarmGame game;HexWorldGrid grid;LandState S=>game.State.land;
        readonly Dictionary<Vector2Int,TerrainKind> baseline=new Dictionary<Vector2Int,TerrainKind>();
        readonly List<GameObject> visuals=new List<GameObject>();readonly List<Object> owned=new List<Object>();
        int tool=-1;GameObject preview;Material good,bad;Mesh outline;
        public bool Active=>tool>=0;
        public string Hint {get;private set;}="";
        public static void ApplySaved(HexWorldGrid grid,LandState state)
        {if(state==null)return;foreach(var c in state.cells)grid.SetTerrain(c.Cell,(TerrainKind)c.kind);}
        public void Initialize(FarmGame owner)
        {
            game=owner;grid=game.World.HexGrid;
            foreach(var c in grid.Cells)baseline[c]=grid.TerrainAt(c);
            foreach(var c in S.cells)baseline[c.Cell]=(TerrainKind)c.original;
            good=game.World.Mat("Land tool allowed","A6CD82");bad=game.World.Mat("Land tool blocked","DA846C");outline=HexWorldGrid.Outline(HexWorldGrid.Radius,.055f);
            RefreshArt();
        }
        public void Choose(int kind)
        {
            if(game.Night&&game.Night.Active)return;game.Night?.CancelTrap();
            if(kind<0||kind>=Names.Length||game.Adventure.InBattle||game.Expedition.Fishing)return;
            string missing=EquipmentCatalog.LandMissing(game,kind);if(missing!=""){game.ShowMessage(missing+"，请先学习图纸并制作。");return;}
            game.Management.Close();game.Construction.Cancel();game.Fences.Cancel();game.Adventure.Hud.Hide();Cancel();tool=kind;
            preview=new GameObject("Land tool target",typeof(MeshFilter),typeof(MeshRenderer));preview.GetComponent<MeshFilter>().sharedMesh=outline;preview.GetComponent<MeshRenderer>().sharedMaterial=good;
            game.ShowMessage(Names[kind]+"：点击身边六步内地块；右键或 Esc 取消。J 打开土地建造面板。");
        }
        public void Cancel(){tool=-1;Hint="";if(preview)Destroy(preview);preview=null;}
        void Update()
        {
            if(!Active)return;
            if(game.Management.IsOpen||game.Adventure.InBattle||game.Expedition.Fishing||game.Construction.Active||game.Fences.Active){Cancel();return;}
            if(!WorldPointer.Available)return;
            if(WorldPointer.Cancel){Cancel();return;}
            if(WorldPointer.Blocked){preview.SetActive(false);return;}
            if(!grid.RaycastGround(game.World.View.ScreenPointToRay(WorldPointer.Position),out var p)||!grid.TryCell(p,out var cell)){preview.SetActive(false);return;}
            bool valid=Allowed(cell,tool,out var reason);preview.SetActive(true);preview.transform.position=grid.SurfacePoint(HexWorldGrid.Center(cell.x,cell.y),.15f);if(grid.TerrainAt(cell)==TerrainKind.Water){var raised=preview.transform.position;raised.y=grid.GetComponent<ContinuousTerrain>().WaterLevelAt(raised)+.08f;preview.transform.position=raised;}preview.GetComponent<MeshRenderer>().sharedMaterial=valid?good:bad;
            Hint=Names[tool]+" · "+(valid?(tool==2?CanalStatus(cell,true):Describe(cell)):reason)+"";
            if(WorldPointer.Pressed)Execute(cell,tool);
        }
        LandCell Record(Vector2Int cell)
        {
            var found=S.Find(cell);if(found!=null)return found;
            found=new LandCell{column=cell.x,row=cell.y,original=(int)baseline[cell],kind=(int)grid.TerrainAt(cell)};S.cells.Add(found);return found;
        }
        bool Reserved(Vector2Int cell)
        {
            var p=HexWorldGrid.Center(cell.x,cell.y);
            foreach(var b in game.State.adventure.buildings)if(BuildingFootprints.Cells(b).Contains(cell))return true;
            if(game.Fences.Occupies(cell))return true;
            foreach(var e in game.World.Adventure.Encounters)if(WorldRelief.PlanarDistance(p,e.Position)<3.2f)return true;
            foreach(var plot in game.World.PlotPositions)if(WorldRelief.PlanarDistance(p,plot)<1.85f)return true;
            foreach(var root in game.World.GetComponentsInChildren<BuildingInspectable>())if(WorldRelief.PlanarDistance(p,root.transform.position)<4)return true;
            foreach(var a in game.State.adventure.animals)if(cell==new Vector2Int(a.homeColumn,a.homeRow))return true;
            // Preserve authored scenery, buildings and forest colliders. Decorative ground is excluded.
            foreach(var hit in Physics.OverlapSphere(grid.SurfacePoint(p,.7f),1.02f,~0,QueryTriggerInteraction.Ignore))
                if(!grid.IsGroundCollider(hit)&&!(hit is CharacterController)&&!hit.GetComponentInParent<AnimalActor>())return true;
            return false;
        }
        bool ActorHere(Vector2Int cell)
        {
            var p=HexWorldGrid.Center(cell.x,cell.y);if(WorldRelief.PlanarDistance(p,game.World.Player.position)<1.9f)return true;
            foreach(var actor in game.Animals.GetComponentsInChildren<AnimalActor>())if(WorldRelief.PlanarDistance(p,actor.transform.position)<1.9f)return true;
            foreach(var actor in game.World.GetComponentsInChildren<AnimalActor>())if(WorldRelief.PlanarDistance(p,actor.transform.position)<1.9f)return true;
            return false;
        }
        public bool Allowed(Vector2Int cell,int action,out string reason)
        {
            reason="";if(action<0||action>=Names.Length){reason="请选择土地工具";return false;}if(!grid.Contains(cell)||WorldRelief.PlanarDistance(HexWorldGrid.Center(cell.x,cell.y),game.World.Player.position)>6){reason="请走近六步以内";return false;}
            if(action==5)return true;
            reason=EquipmentCatalog.LandMissing(game,action);if(reason!="")return false;
            if(Reserved(cell)){reason="请避开农场、建筑、围栏、资源点与牧场";return false;}
            if(S.cells.Count>=2048&&S.Find(cell)==null){reason="已达到土地改造上限";return false;}
            var kind=grid.TerrainAt(cell);var c=S.Find(cell);var a=game.State.adventure;
            if(c?.drainage==true&&action!=8){reason="请先用排水沟工具移除沟槽";return false;}
            if(c!=null&&(c.tree||c.crop.planted)){reason="先收获作物，保留已栽种的树木";return false;}
            if(action==0&&(kind!=TerrainKind.Meadow&&kind!=TerrainKind.Sand||c?.tilled==true)){reason="只能开垦空草地或沙地";return false;}
            if(action==1&&(kind!=TerrainKind.Meadow&&kind!=TerrainKind.Sand)){reason="树苗需要空草地或沙地";return false;}
            if(action==1&&(a.wood<2||a.berries<1)){reason="需要木材2、浆果1";return false;}
            if(action==2&&(kind!=TerrainKind.Meadow&&kind!=TerrainKind.Sand||c?.channel==true)){reason="只能开挖空草地或沙地";return false;}
            if((action==1||action==2||action==3||action==8)&&ActorHere(cell)){reason="请让角色和动物离开目标地块";return false;}
            if(action==2)
            {
                var flow=CanalHydrology.Resolve(grid,S,cell);
                if(!flow.ContainsKey(cell))for(int edge=0;edge<6;edge++)if(flow.ContainsKey(PastureFences.Neighbor(cell,edge))){reason="地势过高，请绕行低处";return false;}
            }
            if(action==3&&(kind!=TerrainKind.Water&&c?.channel!=true)){reason="只能填埋水面或干河道";return false;}
            if(action==3&&(S.soil<2||a.stone<2)){reason="需要土方2、石料2；开挖可获得土方";return false;}
            if(action==4&&(kind!=TerrainKind.Sand&&c?.tilled!=true)){reason="选择沙地或空耕地进行复绿";return false;}
            if(action==4&&a.expedition.fiber<2){reason="需要纤维2";return false;}
            if(action==8)
            {
                if(kind==TerrainKind.Water||c?.channel==true){reason="排水沟需要空陆地，不能替代河道";return false;}
                if(c?.drainage!=true&&a.stone<1){reason="需要石料1加固排水沟";return false;}
            }
            if(action==6||action==7)
            {
                if(!grid.IsDry(cell)||c?.channel==true){reason="只能改造干燥陆地";return false;}
                for(int e=0;e<6;e++)if(grid.TerrainAt(PastureFences.Neighbor(cell,e))==TerrainKind.Water){reason="请保留水岸，避免改变水源";return false;}
                if(ActorHere(cell)){reason="请让角色和动物离开施工地块";return false;}
                for(int r=cell.y-3;r<=cell.y+3;r++)for(int col=cell.x-3;col<=cell.x+3;col++)
                {
                    var neighbor=new Vector2Int(col,r);if(!grid.Contains(neighbor))continue;
                    float distance=WorldRelief.PlanarDistance(HexWorldGrid.Center(col,r),HexWorldGrid.Center(cell.x,cell.y));
                    if(distance<4.7f&&grid.TerrainAt(neighbor)==TerrainKind.Water){reason="请保留水岸缓坡，选择更远的陆地";return false;}
                    var managed=S.Find(neighbor);
                    if(distance<3.4f&&(Reserved(neighbor)||managed!=null&&(managed.tilled||managed.tree||managed.channel))){reason="缓坡范围内有建筑、农田、树木或设施";return false;}
                }
                float height=c?.elevationEdited==true?c.elevation:grid.GroundHeight(HexWorldGrid.Center(cell.x,cell.y));
                if(action==6&&height<=.201f){reason="地面已经挖平";return false;}
                if(action==7&&(height>=11.999f||S.soil<2)){reason=height>=11.999f?"已达到最高地势":"需要土方2；削低山丘可以获得土方";return false;}
            }
            if(game.State.energy<(action==2||action==3||action>=6?3:2)){reason="体力不足";return false;}
            return true;
        }
        public void Execute(Vector2Int cell,int action)
        {
            if(game.Adventure.InBattle||game.Expedition.Fishing)return;
            if(!Allowed(cell,action,out var reason)){game.ShowMessage(reason);return;}
            if(action==5){game.ShowMessage(Describe(cell));return;}
            // Check the whole newly connected flood area before spending resources.
            if(action==2)
            {
                var wet=WetChannels(cell);foreach(var flood in wet)if(grid.TerrainAt(flood)!=TerrainKind.Water&&ActorHere(flood)){game.ShowMessage("接通水源会淹到角色或动物，请先离开河道。");return;}
            }
            var c=Record(cell);var a=game.State.adventure;game.State.SpendEnergy(action==2||action==3||action>=6?3:2);c.dryDays=0;
            if(action==0){c.tilled=true;c.channel=false;c.kind=(int)TerrainKind.Meadow;}
            else if(action==1){a.wood-=2;a.berries--;c.tree=true;c.treeDay=game.State.day;c.tilled=false;}
            else if(action==2){c.channel=true;c.tilled=false;c.kind=(int)TerrainKind.Rock;S.soil+=3;}
            else if(action==3){S.soil-=2;a.stone-=2;c.channel=false;c.tilled=false;c.kind=(int)TerrainKind.Sand;}
            else if(action==8){bool remove=c.drainage;c.drainage=!remove;c.tilled=false;if(!remove)a.stone--;}
            else if(action==6||action==7)
            {
                float height=c.elevationEdited?c.elevation:grid.GroundHeight(HexWorldGrid.Center(cell.x,cell.y));
                c.elevationEdited=true;c.elevation=Mathf.Clamp(height+(action==6?-.5f:.5f),.2f,12);
                S.soil+=action==6?2:-2;
            }
            else {a.expedition.fiber-=2;c.tilled=false;c.channel=false;c.kind=(int)TerrainKind.Meadow;}
            UpdateWater();if(action==6||action==7){var center=HexWorldGrid.Center(cell.x,cell.y);grid.GetComponent<ContinuousTerrain>().RefreshHeights(center);game.World.GetComponent<GrassPatches>().RefreshHeights(game.World,center);game.World.GetComponent<ExpandedWilderness>()?.RefreshGround();}else RefreshTerrain();game.Settlement.RefreshEnvironment();game.Adventure.Commit(Names[action]+"完成。"+(action==2?CanalStatus(cell):Describe(cell)));
        }
        HashSet<Vector2Int> WetChannels(Vector2Int? extra=null)
        {
            var levels=CanalHydrology.Resolve(grid,S,extra);var wet=new HashSet<Vector2Int>();
            foreach(var c in S.cells)if(c.channel&&levels.ContainsKey(c.Cell))wet.Add(c.Cell);
            if(extra.HasValue&&levels.ContainsKey(extra.Value))wet.Add(extra.Value);return wet;
        }
        void UpdateWater()=>CanalHydrology.Apply(grid,S);
        public string CanalStatus(Vector2Int cell,bool prospective=false)
        {
            var levels=CanalHydrology.Resolve(grid,S,prospective?(Vector2Int?)cell:null);
            if(levels.ContainsKey(cell))return "水源已连通";
            for(int edge=0;edge<6;edge++)if(levels.ContainsKey(PastureFences.Neighbor(cell,edge)))return "地势过高，请绕行低处";
            return "尚未接通水源，请连接湖岸与河道";
        }
        public bool Moist(Vector2Int cell)
        {
            if(game.State.Rainy)return true;var p=HexWorldGrid.Center(cell.x,cell.y);
            for(int i=0;i<6;i++)if(grid.TerrainAt(PastureFences.Neighbor(cell,i))==TerrainKind.Water)return true;
            foreach(var c in S.cells)if(c.tree&&c.treeGrowth>=3&&WorldRelief.PlanarDistance(p,HexWorldGrid.Center(c.column,c.row))<4.2f)return true;
            for(int r=cell.y-2;r<=cell.y+2;r++)for(int col=cell.x-2;col<=cell.x+2;col++){var n=new Vector2Int(col,r);if(grid.TerrainAt(n)==TerrainKind.Forest&&grid.Contains(n)&&WorldRelief.PlanarDistance(p,HexWorldGrid.Center(col,r))<4.2f)return true;}
            return false;
        }
        bool Irrigated(Vector2Int cell){if(!game.State.adventure.irrigation)return false;for(int i=0;i<6;i++)if(grid.TerrainAt(PastureFences.Neighbor(cell,i))==TerrainKind.Water)return true;return false;}
        public string Describe(Vector2Int cell)
        {
            var c=S.Find(cell);return (c?.tilled==true?"新农田":TerrainTraits.Names[(int)grid.TerrainAt(cell)])+" · "+(Moist(cell)?"湿润":"偏干")+(c==null?"":" · 干旱 "+c.dryDays+" 天")+(c?.channel==true?" · "+CanalStatus(cell):"")+(c?.drainage==true?game.Settlement.ConnectedDrains().Contains(cell)?" · 排水沟已连通 · 持续排水":" · 排水沟缺少出口 · 请继续挖向低处或水边":"")+" · 积水 "+(game.State.storm.Depth(cell)*100).ToString("0")+" 厘米"+(c?.tree==true?" · 树苗成长 "+c.treeGrowth+" / 3":"")+" · 海拔 "+grid.GroundHeight(HexWorldGrid.Center(cell.x,cell.y)).ToString("0.0")+" 米";
        }
        public bool NearNewPlot
        {get {foreach(var c in S.cells)if(c.tilled&&WorldRelief.PlanarDistance(HexWorldGrid.Center(c.column,c.row),game.World.Player.position)<1.7f)return true;return false;}}
        public bool InteractNearby(bool water)
        {
            if(Active||game.Construction.Active||game.Fences.Active)return false;LandCell chosen=null;float best=1.7f;
            foreach(var c in S.cells)if(c.tilled||(water&&c.tree&&c.treeGrowth<3)){float d=WorldRelief.PlanarDistance(HexWorldGrid.Center(c.column,c.row),game.World.Player.position);if(d<best){chosen=c;best=d;}}
            if(chosen==null)return false;return InteractCell(chosen.Cell,water);
        }
        public bool InteractCell(Vector2Int cell,bool water)
        {
            if(Active||game.Construction.Active||game.Fences.Active||game.Management.IsOpen||game.Adventure.InBattle||game.Expedition.Fishing)return false;
            var chosen=S.Find(cell);if(chosen==null||(!chosen.tilled&&!(chosen.tree&&water)))return false;
            if(WorldRelief.PlanarDistance(HexWorldGrid.Center(cell.x,cell.y),game.World.Player.position)>=1.7f){game.ShowMessage("请靠近选中的农田或树苗。");return true;}
            if(game.State.storm.Depth(cell)>.15f){game.ShowMessage("地块积水过深，请先修排水沟。");return true;}
            var crop=chosen.crop;var s=game.State;
            if((water||chosen.tree)&&!EquipmentCatalog.Require(game,"can"))return true;
            if(!water&&!chosen.tree&&(crop.Ready||!crop.planted)&&!EquipmentCatalog.Require(game,crop.Ready?"sickle":"hoe"))return true;
            if(chosen.tree)
            {if(chosen.treeWatered){game.ShowMessage("树苗今天已浇水。");return true;}if(!s.SpendEnergy(1)){game.ShowMessage("体力不足。");return true;}chosen.treeWatered=true;game.Adventure.Commit("树苗浇水完成，夜间会成长。");return true;}
            if(water)
            {if(!crop.planted||crop.Ready||crop.watered){game.ShowMessage("这块农田无需浇水。");return true;}if(!s.SpendEnergy(1)){game.ShowMessage("体力不足。");return true;}crop.watered=true;}
            else if(crop.Ready)
            {if(!s.SpendEnergy(1)){game.ShowMessage("体力不足。");return true;}int kind=crop.kind;s.coins+=kind==0?12:kind==1?24:kind==2?18:60;if(kind==3)s.adventure.village.pantry+=3;s.harvests++;s.weather.survivalExperience+=2;if(kind==0)s.adventure.homestead.carrots+=2;if(kind==2){crop.growth=0;crop.growthBonus=0;crop.watered=s.Rainy;s.adventure.berries+=2;}else chosen.crop=new CropState();}
            else if(!crop.planted)
            {if(s.SelectedSeeds<1||s.energy<1){game.ShowMessage("需要当前作物种子和体力1。");return true;}s.SpendEnergy(1);crop.kind=s.selectedCrop;crop.planted=true;crop.watered=s.Rainy||Irrigated(chosen.Cell);if(s.selectedCrop==0)s.seeds--;else if(s.selectedCrop==1)s.wheatSeeds--;else if(s.selectedCrop==2)s.berrySeeds--;else s.starSeeds--;}
            else {game.ShowMessage(crop.watered?"新农田今天已浇水，等待成长。":"新农田需要浇水，按 R。" );return true;}
            RefreshArt();game.Adventure.Commit(water?"新农田浇水完成。":"农田操作完成。");return true;
        }
        public void BeforeSleep()
        {for(int i=0;i<game.State.plots.Length;i++)if(game.State.storm.DepthAt(game.World.PlotPositions[i])>.15f)game.State.plots[i].watered=false;
            foreach(var c in S.cells)if(c.tree&&Moist(c.Cell))c.treeWatered=true;
            foreach(var c in S.cells)if(c.tilled&&c.crop.planted){if(Irrigated(c.Cell))c.crop.watered=true;c.crop.Advance(game.State.storm.Depth(c.Cell)>.15f?0:WeatherCatalog.Get(game.State.Weather).cropGrowthMultiplier);}}
        public void NewDay()
        {
            foreach(var c in S.cells)
            {
                bool moist=Moist(c.Cell);c.crop.watered=c.crop.planted&&(game.State.Rainy||Irrigated(c.Cell));
                // Only managed ground changes; authored scenery and productive crops stay safe.
                if(c.tree)
                {
                    if(moist||c.treeWatered){c.treeGrowth=Mathf.Min(3,c.treeGrowth+1);c.dryDays=0;}else c.dryDays++;
                    c.treeWatered=false;if(c.treeGrowth>=3)c.kind=(int)TerrainKind.Forest;continue;
                }
                if(c.channel)continue;
                if(moist)c.dryDays=Mathf.Max(0,c.dryDays-2);else c.dryDays++;
                int limit=game.State.Season=="夏季"?4:8;if(c.crop.planted)limit*=2;
                if(c.dryDays>=limit&&!c.crop.planted){c.kind=(int)TerrainKind.Sand;c.tilled=false;}
            }
            UpdateWater();RefreshTerrain();
        }
        public void FinishLogging(Vector2Int cell)
        {
            var c=Record(cell);c.tree=false;c.treeGrowth=0;c.treeWatered=false;c.kind=(int)TerrainKind.Meadow;RefreshTerrain();
        }
        void RefreshTerrain()
        {
            ApplySaved(grid,S);grid.GetComponent<ContinuousTerrain>().RebuildSurface();
            game.World.GetComponent<GrassPatches>().Build(game.World);RefreshArt();game.Animals.RefreshPens();
        }
        void RefreshArt()
        {
            foreach(var go in visuals)if(go){go.SetActive(false);Destroy(go);}visuals.Clear();foreach(var asset in owned)if(asset)Destroy(asset);owned.Clear();
            foreach(var c in S.cells)
            {
                var p=grid.SurfacePoint(HexWorldGrid.Center(c.column,c.row),.018f);
                if(c.tree)
                {var tree=game.World.GetComponent<FantasyPresentation>().PlantTree(p,Mathf.Lerp(.65f,3.5f,Mathf.Clamp01(c.treeGrowth/3f)));if(tree){var marker=tree.GetComponent<HarvestableTree>();if(marker){marker.planted=true;marker.cell=c.Cell;marker.Felled=false;tree.gameObject.SetActive(true);if(game.Inspection)WorldInspection.Volume(tree);game.Inspection?.RebindPlantedTree(c.Cell,marker);}visuals.Add(tree.gameObject);}}
                if(!c.tilled)continue;
                var vertices=new List<Vector3>();var colors=new List<Color>();var uv=new List<Vector2>();var tris=new List<int>();
                const int count=22;
                for(int z=0;z<=count;z++)for(int x=0;x<=count;x++)
                {
                    var v=p+new Vector3((x/(float)count-.5f)*2.25f,0,(z/(float)count-.5f)*2.25f);var delta=v-p;float edge=-10;
                    for(int i=0;i<6;i++){float angle=i*Mathf.PI/3;edge=Mathf.Max(edge,delta.x*Mathf.Cos(angle)+delta.z*Mathf.Sin(angle));}
                    float fade=Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.02f,.75f,edge));v.y=grid.GroundHeight(v)+.01f+FarmFieldArt.Ridge(v)*fade;
                    vertices.Add(v);uv.Add(new Vector2(v.x-FarmFieldArt.Bend(v.z),v.z));colors.Add(new Color(c.crop.watered?.85f:0,c.crop.planted?1:0,.3f,fade));
                    if(x<count&&z<count){int a=z*(count+1)+x;tris.AddRange(new[]{a,a+count+1,a+1,a+1,a+count+1,a+count+2});}
                }
                var mesh=new Mesh{name="Player tilled soft furrows"};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetColors(colors);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateBounds();owned.Add(mesh);
                var mat=new Material(Shader.Find("StarfieldEra/Cultivated Soil"));mat.SetTexture("_Atlas",Resources.Load<Texture2D>("TerrainArt/ReferenceAtlas"));owned.Add(mat);
                var bed=new GameObject("Player cultivated bed",typeof(MeshFilter),typeof(MeshRenderer));bed.transform.SetParent(transform,false);bed.GetComponent<MeshFilter>().sharedMesh=mesh;bed.GetComponent<MeshRenderer>().sharedMaterial=mat;visuals.Add(bed);var bedTarget=bed.AddComponent<WorldPickTarget>();bedTarget.land=true;bedTarget.cell=c.Cell;
                if(!c.crop.planted)continue;
                for(int row=-1;row<=1;row++)for(int i=-1;i<=1;i++)
                {
                    var v=p+new Vector3(i*.48f,0,row*.52f);v.x=Mathf.Round((v.x-FarmFieldArt.Bend(v.z))/.64f)*.64f+FarmFieldArt.Bend(v.z);v.y=grid.GroundHeight(v)+FarmFieldArt.Ridge(v)+.04f;
                    float size=c.crop.Ready?.27f:.07f+c.crop.growth*.04f;
                    var leaf=game.World.Shape("New crop leaves",PrimitiveType.Sphere,v,new Vector3(size,.09f+size,size*.7f),good);leaf.SetParent(transform,true);visuals.Add(leaf.gameObject);var leafTarget=leaf.gameObject.AddComponent<WorldPickTarget>();leafTarget.land=true;leafTarget.cell=c.Cell;
                    if(c.crop.growth>0){var fruit=game.World.Shape("New crop fruit",PrimitiveType.Sphere,v+Vector3.up*.1f,Vector3.one*size*.8f,c.crop.Ready?bad:good);fruit.SetParent(transform,true);visuals.Add(fruit.gameObject);var fruitTarget=fruit.gameObject.AddComponent<WorldPickTarget>();fruitTarget.land=true;fruitTarget.cell=c.Cell;}
                }
            }
        }
        void OnDestroy(){foreach(var go in visuals)if(go)Destroy(go);foreach(var asset in owned)if(asset)Destroy(asset);if(outline)Destroy(outline);if(preview)Destroy(preview);}
    }
}
