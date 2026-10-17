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
        static bool pressed, blocked, clicked, trackingClick, held, released;
        static float dragTravel;
        static readonly List<RaycastResult> hits = new List<RaycastResult>();
        public static bool Available => Mouse.current != null || Touchscreen.current != null;
        public static Vector2 Position { get { Read(); return position; } }
        public static bool Pressed { get { Read(); return pressed; } }
        public static bool Held {get{Read();return held;}}
        public static bool Released {get{Read();return released;}}
        public static bool Clicked { get { Read(); return clicked; } }
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
            pressed = clicked = held = released = false;
            blocked = true;
            if (Touchscreen.current != null)
                foreach (var touch in Touchscreen.current.touches)
                {
                    if (!touch.press.isPressed && !touch.press.wasPressedThisFrame && !touch.press.wasReleasedThisFrame) continue;
                    var point = touch.position.ReadValue();
                    if (OverUI(point)) continue;
                    position = point; clicked = pressed = touch.press.wasPressedThisFrame; held=touch.press.isPressed;released=touch.press.wasReleasedThisFrame; blocked = false;
                    return;
                }
            if (Application.isMobilePlatform) return;
            if (Mouse.current == null) return;
            position = Mouse.current.position.ReadValue();
            pressed = Mouse.current.leftButton.wasPressedThisFrame;held=Mouse.current.leftButton.isPressed;released=Mouse.current.leftButton.wasReleasedThisFrame;
            blocked = OverUI(position);
            if(pressed){trackingClick=!blocked;dragTravel=0;}
            if(trackingClick)dragTravel+=Mouse.current.delta.ReadValue().magnitude;
            if(Mouse.current.leftButton.wasReleasedThisFrame)
            {clicked=trackingClick&&!blocked&&dragTravel<=5;trackingClick=false;}
            else if(!Mouse.current.leftButton.isPressed)trackingClick=false;
        }
    }
}
