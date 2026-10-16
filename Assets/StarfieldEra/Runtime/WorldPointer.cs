using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace StarfieldEra
{
    // Share world targeting between desktop mouse and Android touches. UI touches never place buildings.
    public static class WorldPointer
    {
        static int frame = -1;
        static Vector2 position;
        static bool pressed, blocked;
        static readonly List<RaycastResult> hits = new List<RaycastResult>();
        public static bool Available => Mouse.current != null || Touchscreen.current != null;
        public static Vector2 Position { get { Read(); return position; } }
        public static bool Pressed { get { Read(); return pressed; } }
        public static bool Blocked { get { Read(); return blocked; } }
        public static bool Cancel => Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
        static bool OverUI(Vector2 point)
        {
            if (!EventSystem.current) return false;
            hits.Clear();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = point }, hits);
            return hits.Count > 0;
        }
        static void Read()
        {
            if (frame == Time.frameCount) return;
            frame = Time.frameCount;
            pressed = false;
            blocked = true;
            if (Touchscreen.current != null)
                foreach (var touch in Touchscreen.current.touches)
                {
                    if (!touch.press.isPressed && !touch.press.wasPressedThisFrame) continue;
                    var point = touch.position.ReadValue();
                    if (OverUI(point)) continue;
                    position = point; pressed = touch.press.wasPressedThisFrame; blocked = false;
                    return;
                }
            if (Application.isMobilePlatform) return;
            if (Mouse.current == null) return;
            position = Mouse.current.position.ReadValue();
            pressed = Mouse.current.leftButton.wasPressedThisFrame;
            blocked = OverUI(position);
        }
    }
}
