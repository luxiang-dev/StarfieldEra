using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StarfieldEra
{
    public sealed class FarmGame : MonoBehaviour
    {
        public TMP_FontAsset interfaceFont;
        public Material worldMaterial;
        public FarmState State { get; private set; }
        public FarmWorld World { get; private set; }
        public AdventureGame Adventure { get; private set; }
        public HexConstruction Construction { get; private set; }
        public PastureFences Fences { get; private set; }
        public FarmAnimals Animals { get; private set; }
        public HomesteadGame Homestead {get;private set;}
        public NightDefense Night {get;private set;}
        public ExpeditionGame Expedition { get; private set; }
        public LandStewardship Land {get;private set;}
        public TreeChopping Forestry {get;private set;}
        public RockMining Mining {get;private set;}
        public WorldInspection Inspection {get;private set;}
        public bool SprintRequested {get;set;}
        public bool IsSprinting {get;private set;}
        bool jumpRequested;
        public bool IsGrounded=>World&&World.Controller&&(World.Controller.isGrounded||(gravity<=0&&World.Player.position.y-World.HexGrid.GroundHeight(World.Player.position)<.12f));
        public void RequestJump(){if(Management&&Management.IsOpen||Adventure.InBattle||Expedition.Fishing)return;jumpRequested=true;}
        public WeatherSystem Weather { get; private set; }
        public WeatherManager WeatherManager {get;private set;}
        public ManagementHud Management { get; private set; }
        public int SelectedPlot { get; private set; } = -1;
        public bool NearHouse => World != null && (Vector3.Distance(Flat(World.Player.position), World.Door) < 2.5f || State.adventure.buildings.Exists(b=>(b.kind==4||b.kind==11)&&WorldRelief.PlanarDistance(World.Player.position,HexWorldGrid.Center(b.column,b.row))<4));
        public string Message { get; private set; } = "欢迎来到星野纪元！走近农田，种下第一颗种子。";
        public bool SaveHealthy { get; private set; } = true;
        public string SavePath => Path.Combine(Application.persistentDataPath, "farm-v1.json");
        FarmHud hud;
        float gravity;

        void Awake()
        {
            State = FarmSave.TryLoad(SavePath, out var loaded) ? loaded : new FarmState();
            State.Upgrade();
            WeatherManager=gameObject.AddComponent<WeatherManager>();WeatherManager.Initialize(this);
            World = new GameObject("Farm island").AddComponent<FarmWorld>();
            World.transform.SetParent(transform);
            World.materialTemplate = worldMaterial;
            World.labelFont = interfaceFont;
            World.Generate();
            World.HexGrid.CompleteTerrain(World,State);
            Physics.SyncTransforms();
            World.Player.position = World.HexGrid.SafeLand(new Vector3(Mathf.Clamp(State.playerX, -HexWorldGrid.HalfWidth+1, HexWorldGrid.HalfWidth-1), .25f, Mathf.Clamp(State.playerZ, -HexWorldGrid.HalfDepth+1, HexWorldGrid.HalfDepth-1)),World.Controller.radius);
            if(State.adventure==null) State.adventure=new AdventureState();
            Adventure=gameObject.AddComponent<AdventureGame>();
            Adventure.Initialize(this);
            Construction=gameObject.AddComponent<HexConstruction>();Construction.Initialize(this);
            Fences=gameObject.AddComponent<PastureFences>();Fences.Initialize(this);
            Animals=gameObject.AddComponent<FarmAnimals>();Animals.Initialize(this);
            Expedition=gameObject.AddComponent<ExpeditionGame>();Expedition.Initialize(this);
            Land=gameObject.AddComponent<LandStewardship>();Land.Initialize(this);
            World.gameObject.AddComponent<ExpandedWilderness>().Build(this);
            Forestry=gameObject.AddComponent<TreeChopping>();Forestry.Initialize(this);
            Mining=gameObject.AddComponent<RockMining>();Mining.Initialize(this);
            Homestead=gameObject.AddComponent<HomesteadGame>();Homestead.Initialize(this);
            hud = new GameObject("Farm HUD").AddComponent<FarmHud>();
            hud.transform.SetParent(transform);
            hud.Create(this, interfaceFont);
            Management=hud.GetComponent<ManagementHud>();
            Construction.CreateMenu(hud.transform,interfaceFont);
            Inspection=hud.gameObject.AddComponent<WorldInspection>();Inspection.Initialize(this,interfaceFont);
            World.GetComponent<FantasyPresentation>()?.Stylize(transform);
            Weather = gameObject.AddComponent<WeatherSystem>();
            Weather.Initialize(this);
            Night=gameObject.AddComponent<NightDefense>();Night.Initialize(this);
            World.Controller.slopeLimit=50;
            World.RefreshCrops(State);
            UpdateSelection();
            hud.Refresh();
        }

        // Terrain edits and old saves can leave the capsule in a trench. Recover before UI/input gates.
        void RecoverFromUnsafeGround()
        {
            var grid=World.HexGrid;var p=World.Player.position;
            if(grid.CanStand(p,World.Controller.radius)&&p.y>=grid.GroundHeight(p)-.35f)return;
            var safe=grid.SafeLand(p,World.Controller.radius);
            World.Controller.enabled=false;World.Player.position=safe;World.Controller.enabled=true;gravity=-2;
        }

        void Update()
        {
            if(State.RecoverEnergy(Time.deltaTime))Persist();
            RecoverFromUnsafeGround();IsSprinting=false;
            var keyboard = Keyboard.current;
            if(Construction.Menu&&Construction.Menu.IsOpen)
            {
                jumpRequested=false;
                if(keyboard!=null&&keyboard.jKey.wasPressedThisFrame){Construction.Menu.Close();Management.Open("land");}
                if(keyboard!=null&&(keyboard.escapeKey.wasPressedThisFrame||keyboard.hKey.wasPressedThisFrame||keyboard.gKey.wasPressedThisFrame))Construction.Menu.Close();
                hud.Refresh();return;
            }
            if(Management.IsOpen)
            {
                jumpRequested=false;
                if(keyboard!=null)
                {
                    if(keyboard.escapeKey.wasPressedThisFrame||keyboard.tabKey.wasPressedThisFrame)Management.Close();
                    else if(keyboard.iKey.wasPressedThisFrame)Management.Toggle("inventory");
                    else if(keyboard.qKey.wasPressedThisFrame)Management.Toggle("tasks");
                    else if(keyboard.lKey.wasPressedThisFrame)Management.Toggle("craft");
                    else if(keyboard.mKey.wasPressedThisFrame)Management.Toggle("map");
                    else if(keyboard.jKey.wasPressedThisFrame)Management.Toggle("land");
                    else if(keyboard.hKey.wasPressedThisFrame)Construction.Open();
                    else if(keyboard.yKey.wasPressedThisFrame)Management.Toggle("weather");
                }
                hud.Refresh();return;
            }
            if(Expedition.Fishing)
            {
                jumpRequested=false;
                if(keyboard!=null){if(keyboard.eKey.wasPressedThisFrame)Expedition.Reel();if(keyboard.escapeKey.wasPressedThisFrame)Expedition.CancelFishing();}
                hud.Refresh();return;
            }
            Vector2 input = Vector2.zero;
            if (keyboard != null && !Adventure.InBattle)
            {
                input.x = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1 : 0);
                input.y = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1 : 0);
            }
            if (!Adventure.InBattle) input = Vector2.ClampMagnitude(input + MobileControls.Movement, 1);
            Vector3 forward = Flat(World.View.transform.forward).normalized;
            Vector3 right = Flat(World.View.transform.right).normalized;
            Vector3 move = (forward * input.y + right * input.x).normalized;
            if (!World.Adventure.CameraRig.IsFirstPerson&&move.sqrMagnitude > .01f)
                World.Player.rotation = Quaternion.Slerp(World.Player.rotation, Quaternion.LookRotation(move), Time.deltaTime * 14);
            if(IsGrounded&&gravity<0)gravity=-2;
            bool jump=jumpRequested||keyboard!=null&&keyboard.spaceKey.wasPressedThisFrame;jumpRequested=false;
            if(jump&&IsGrounded&&!Adventure.InBattle)gravity=Mathf.Sqrt(2*20*1.25f);
            float frameDelta=Mathf.Min(Time.deltaTime,.1f);
            float verticalMove=gravity*frameDelta-10*frameDelta*frameDelta;
            gravity=Mathf.Max(-20,gravity-20*frameDelta);
            var grid=World.HexGrid;var before=World.Player.position;
            float speed=Expedition.MovementSpeed(grid.TerrainAt(before));
            IsSprinting=move.sqrMagnitude>.01f&&!Adventure.InBattle&&(SprintRequested||keyboard!=null&&(keyboard.leftShiftKey.isPressed||keyboard.rightShiftKey.isPressed));
            var horizontal=grid.ConstrainMovement(before,move*(IsSprinting?6.6f:4)*speed*Mathf.Min(Time.deltaTime,.1f),World.Controller.radius);
            World.Controller.Move(horizontal+Vector3.up*verticalMove);
            // Collision sliding must also respect the shoreline, including the character radius.
            if(!grid.CanStand(World.Player.position,World.Controller.radius))
            {var safe=grid.SafeLand(before,World.Controller.radius,false);World.Controller.enabled=false;World.Player.position=safe;World.Controller.enabled=true;}
            Vector3 position = World.Player.position;
            // Physical island edge is complemented by bounds so no input can walk off it.
            position.x = Mathf.Clamp(position.x, -HexWorldGrid.HalfWidth+1, HexWorldGrid.HalfWidth-1);
            position.z = Mathf.Clamp(position.z, -HexWorldGrid.HalfDepth+1, HexWorldGrid.HalfDepth-1);
            World.Player.position = position;
            UpdateSelection();
            if (keyboard != null)
            {
                if (keyboard.yKey.wasPressedThisFrame) Management.Toggle("weather");
                if (keyboard.tabKey.wasPressedThisFrame) Management.Toggle("inventory");
                if (keyboard.lKey.wasPressedThisFrame) Management.Toggle("craft");
                if (keyboard.gKey.wasPressedThisFrame) Construction.Open();
                if (keyboard.hKey.wasPressedThisFrame) Construction.Open();
                if (keyboard.jKey.wasPressedThisFrame) Management.Toggle("land");
                if (keyboard.qKey.wasPressedThisFrame) Adventure.Hud.Show("journal");
                if (keyboard.mKey.wasPressedThisFrame) Adventure.Hud.Show("map");
                if (keyboard.tKey.wasPressedThisFrame) CycleCrop();
                if (keyboard.iKey.wasPressedThisFrame) Adventure.Hud.Show("supplies");
                if (keyboard.cKey.wasPressedThisFrame) Adventure.CycleCompanion();
                if(Management.IsOpen){hud.Refresh();return;}
                if (Adventure.InBattle)
                {
                    if(keyboard.digit1Key.wasPressedThisFrame)Adventure.BattleAction(0);
                    if(keyboard.digit2Key.wasPressedThisFrame)Adventure.BattleAction(1);
                    if(keyboard.digit3Key.wasPressedThisFrame)Adventure.BattleAction(2);
                    if(keyboard.digit4Key.wasPressedThisFrame)Adventure.BattleAction(3);
                }
                if (keyboard.eKey.wasPressedThisFrame) Interact();
                if (keyboard.xKey.wasPressedThisFrame) Forestry.Chop();
                if (keyboard.rKey.wasPressedThisFrame) {if(Construction.Active)Construction.Rotate();else Water();}
                if (keyboard.fKey.wasPressedThisFrame) Sleep();
                if (keyboard.bKey.wasPressedThisFrame) BuySeed();
                if (keyboard.escapeKey.wasPressedThisFrame) { Inspection.Clear(); Night.CancelTrap(); Construction.Cancel(); Fences.Cancel(); Land.Cancel(); if(!Adventure.InBattle) Adventure.Hud.Hide(); Persist(); }
            }
            hud.Refresh();
        }

        static Vector3 Flat(Vector3 value) => new Vector3(value.x, 0, value.z);

        public void UpdateSelection()
        {
            int selected = -1;
            float nearest = 1.65f;
            for (int i = 0; i < FarmState.PlotCount; i++)
            {
                float distance = Vector3.Distance(Flat(World.Player.position), Flat(World.PlotPositions[i]));
                if (distance < nearest) { nearest = distance; selected = i; }
            }
            SelectedPlot = selected;
            World.Select(selected);
            Adventure?.UpdateTarget();
            Forestry?.UpdateTarget();
            Mining?.UpdateTarget();
        }

        public void Interact()
        {
            if(Night&&Night.Active){if(Inspection&&Inspection.HasSelection)Inspection.InteractSelected();else Night.Attack();return;}
            if(Night&&Night.TrapPlacing)return;
            if(Expedition.Fishing){Expedition.Reel();return;}
            if(Adventure.InBattle||Construction.Active||Fences.Active||Land.Active)return;
            if(Inspection&&Inspection.HasSelection){Inspection.InteractSelected();return;}
            if(Homestead.InteractNearby())return;
            if(Land.InteractNearby(false))return;
            if(Animals.InteractNearby())return;
            if(Adventure.Target!=null){Adventure.Interact();return;}
            if(Mining.HasNearby){Mining.Mine();return;}
            if(SelectedPlot<0&&Forestry.HasNearby){Forestry.Chop();return;}
            InteractPlot(SelectedPlot);
        }
        public void InteractPlot(int index)
        {
            if(Adventure.InBattle||Expedition.Fishing||Management.IsOpen||Construction.Active||Fences.Active||Land.Active)return;
            if(index>=FarmState.PlotCount)return;
            if(index>=0&&WorldRelief.PlanarDistance(World.Player.position,World.PlotPositions[index])>=1.7f){Notify("请靠近选中的农田。");return;}
            if (index < 0) { Notify("先走近一块农田。金色边框表示当前目标。"); return; }
            if(index>=12 && !State.adventure.expandedFarm){Notify("南侧农田尚未开垦，在村庄议事牌扩建农场后启用。");return;}
            if(State.energy<1){Notify("体力不足，回小屋休息或在营地使用料理。");return;}
            CropState crop = State.plots[index];
            if (crop.Ready)
            {
                if(!EquipmentCatalog.Require(this,"sickle"))return;
                int kind=crop.kind;State.Harvest(index);
                Changed("收获"+FarmState.CropNames[kind]+"并售出！浆果田会继续生长。可以在小屋购买新种子。");
            }
            else if (!crop.planted)
            {
                if(!EquipmentCatalog.Require(this,"hoe"))return;
                if (!State.Plant(index)) { Notify("种子不足，请在小屋或商人旁购买。"); return; }
                Changed("已种下"+FarmState.CropNames[State.selectedCrop]+(State.Rainy?"。雨水已浇灌。":"。作物需要浇水。"));
            }
            else Notify(crop.watered ? "今天已浇水。回小屋休息，等待作物成长。" : "作物需要浇水。");
        }

        public void Water()
        {
            if(Night&&Night.Active){Night.Repair();return;}
            if(Adventure.InBattle||Expedition.Fishing)return;
            if(Inspection&&Inspection.HasSelection&&Inspection.WaterSelected())return;
            if(!EquipmentCatalog.Require(this,"can"))return;
            if(Land.InteractNearby(true))return;
            if (SelectedPlot < 0) { Notify("走近已播种的农田才能浇水。"); return; }
            if (!State.Water(SelectedPlot)) { Notify("体力不足，或农田今天无需浇水。成熟作物可以收获。"); return; }
            Changed("浇水完成！湿润的农田会在夜间成长。");
        }

        public void Sleep()
        {
            if(Adventure.InBattle||Expedition.Fishing)return;
            if(Night&&Night.Active){Notify("先守住资源点，再迎接新一天。");return;}
            if (!NearHouse) { Notify("请在小屋或居民房屋旁休息以结束一天。"); return; }
            if(Night&&Night.TryBegin())return;
            CompleteSleep();
        }

        public void CompleteSleep()
        {
            Land.BeforeSleep();
            State.Sleep();
            Land.NewDay();
            Adventure.Commit("新的一天！设施完成了生产，伙伴和灌溉设施会照料农田。"+(Night&&Night.State.Target(State)>=0?Night.Forecast:""));
            World.RefreshCrops(State);
        }

        public void BuySeed()
        {
            if(Adventure.InBattle||Expedition.Fishing)return;
            if (!NearHouse && !(Adventure.Target!=null && Adventure.Target.kind==EncounterKind.Market)) { Notify("种子商店在小屋门前，也可以与远行商人交易。"); return; }
            if (!State.BuySeed()) { Notify("金币不足：当前种子需要 "+State.SeedCost+" 金币。"); return; }
            Changed("购买"+FarmState.CropNames[State.selectedCrop]+"种子 +1。");
        }

        public void SelectCrop(int kind)
        {
            if(Adventure.InBattle||Expedition.Fishing||kind<0||kind>2)return;
            State.selectedCrop=kind;Changed("当前种子："+FarmState.CropNames[kind]+"。");
        }

        public void CycleCrop()
        {
            if(Adventure.InBattle||Expedition.Fishing)return;
            State.selectedCrop=(State.selectedCrop+1)%3;
            Changed("当前种子："+FarmState.CropNames[State.selectedCrop]+"。余量 "+State.SelectedSeeds+"，购买价 "+State.SeedCost+" 金币。");
        }

        void Changed(string message)
        {
            World.RefreshCrops(State);
            Adventure.Commit(message);
            hud.Refresh();
        }

        void Notify(string message) { Message = message; hud?.Refresh(); }
        public void ShowMessage(string message) => Notify(message);

        public void Persist()
        {
            if (State == null || World == null) return;
            Animals?.SavePositions();
            State.playerX = World.Player.position.x;
            State.playerZ = World.Player.position.z;
            try { FarmSave.Save(SavePath, State); SaveHealthy = true; }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                SaveHealthy = false;
                Debug.LogWarning("Farm progress could not be saved: " + e.Message);
            }
        }

        void OnApplicationPause(bool paused) { if (paused) Persist(); }
        void OnApplicationQuit() => Persist();
        void OnDisable() { if (Application.isPlaying) Persist(); }
    }
}
