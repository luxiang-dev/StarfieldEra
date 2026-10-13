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
        public int SelectedPlot { get; private set; } = -1;
        public bool NearHouse => World != null && Vector3.Distance(Flat(World.Player.position), World.Door) < 2.5f;
        public string Message { get; private set; } = "欢迎来到星野纪元！走近农田，种下第一颗种子。";
        public bool SaveHealthy { get; private set; } = true;
        public string SavePath => Path.Combine(Application.persistentDataPath, "farm-v1.json");
        FarmHud hud;
        float gravity;

        void Awake()
        {
            State = FarmSave.TryLoad(SavePath, out var loaded) ? loaded : new FarmState();
            World = new GameObject("Farm island").AddComponent<FarmWorld>();
            World.transform.SetParent(transform);
            World.materialTemplate = worldMaterial;
            World.Generate();
            World.Player.position = new Vector3(Mathf.Clamp(State.playerX, -10.5f, 10.5f), .25f, Mathf.Clamp(State.playerZ, -9.5f, 9.5f));
            hud = new GameObject("Farm HUD").AddComponent<FarmHud>();
            hud.transform.SetParent(transform);
            hud.Create(this, interfaceFont);
            World.RefreshCrops(State);
            UpdateSelection();
            hud.Refresh();
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            Vector2 input = Vector2.zero;
            if (keyboard != null)
            {
                input.x = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1 : 0);
                input.y = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1 : 0);
            }
            Vector3 forward = Flat(World.View.transform.forward).normalized;
            Vector3 right = Flat(World.View.transform.right).normalized;
            Vector3 move = (forward * input.y + right * input.x).normalized;
            if (move.sqrMagnitude > .01f)
                World.Player.rotation = Quaternion.Slerp(World.Player.rotation, Quaternion.LookRotation(move), Time.deltaTime * 14);
            gravity = World.Controller.isGrounded ? -2 : Mathf.Max(-20, gravity - 20 * Time.deltaTime);
            World.Controller.Move((move * 4 + Vector3.up * gravity) * Time.deltaTime);
            Vector3 position = World.Player.position;
            // Physical island edge is complemented by bounds so no input can walk off it.
            position.x = Mathf.Clamp(position.x, -10.5f, 10.5f);
            position.z = Mathf.Clamp(position.z, -9.5f, 9.5f);
            World.Player.position = position;
            UpdateSelection();
            if (keyboard != null)
            {
                if (keyboard.eKey.wasPressedThisFrame) Interact();
                if (keyboard.rKey.wasPressedThisFrame) Water();
                if (keyboard.fKey.wasPressedThisFrame) Sleep();
                if (keyboard.bKey.wasPressedThisFrame) BuySeed();
                if (keyboard.escapeKey.wasPressedThisFrame) Persist();
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
        }

        public void Interact()
        {
            if (SelectedPlot < 0) { Notify("先走近一块农田。金色边框表示当前目标。"); return; }
            CropState crop = State.plots[SelectedPlot];
            if (crop.Ready)
            {
                State.Harvest(SelectedPlot);
                Changed("收获金萝卜并售出！金币 +12。可以在小屋购买新种子。");
            }
            else if (!crop.planted)
            {
                if (!State.Plant(SelectedPlot)) { Notify("种子不足，走到小屋旁按 B 购买。"); return; }
                Changed("已种下金萝卜。按 R 浇水，再回小屋休息。");
            }
            else Notify(crop.watered ? "今天已浇水。回小屋休息，等待作物成长。" : "作物需要水，按 R 浇水。");
        }

        public void Water()
        {
            if (SelectedPlot < 0) { Notify("走近已播种的农田才能浇水。"); return; }
            if (!State.Water(SelectedPlot)) { Notify("空田无需浇水；成熟作物可以收获，每天只需浇水一次。"); return; }
            Changed("浇水完成！湿润的农田会在夜间成长。");
        }

        public void Sleep()
        {
            if (!NearHouse) { Notify("走到小屋门前，再按 F 结束一天。"); return; }
            State.Sleep();
            Changed("新的一天！记得再次浇水。金萝卜需要两个浇水夜晚成熟。");
        }

        public void BuySeed()
        {
            if (!NearHouse) { Notify("种子商店在小屋门前，走近后按 B。"); return; }
            if (!State.BuySeed()) { Notify("金币不足：一颗种子需要 5 金币。"); return; }
            Changed("购买种子 +1，金币 -5。");
        }

        void Changed(string message)
        {
            Message = message;
            World.RefreshCrops(State);
            Persist();
            hud.Refresh();
        }

        void Notify(string message) { Message = message; hud.Refresh(); }

        public void Persist()
        {
            if (State == null || World == null) return;
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
