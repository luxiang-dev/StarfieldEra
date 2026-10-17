using UnityEngine;
using UnityEngine.EventSystems;

namespace StarfieldEra
{
    public sealed class MobileStick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public Vector2 Value { get; private set; }
        public RectTransform knob;
        int pointer = int.MinValue;
        public void OnPointerDown(PointerEventData e)
        { if (pointer != int.MinValue) return; pointer = e.pointerId; OnDrag(e); }
        public void OnDrag(PointerEventData e)
        {
            if (pointer != e.pointerId) return;
            var rect = (RectTransform)transform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, e.position, e.pressEventCamera, out var point);
            Value = Vector2.ClampMagnitude(point / (rect.rect.width * .4f), 1);
            knob.anchoredPosition = Value * rect.rect.width * .3f;
        }
        public void OnPointerUp(PointerEventData e) { if (pointer == e.pointerId) ResetStick(); }
        void OnDisable() { ResetStick(); }
        void OnApplicationFocus(bool focus) { if (!focus) ResetStick(); }
        void ResetStick() { pointer = int.MinValue; Value = Vector2.zero; if (knob) knob.anchoredPosition = Vector2.zero; }
    }
}
