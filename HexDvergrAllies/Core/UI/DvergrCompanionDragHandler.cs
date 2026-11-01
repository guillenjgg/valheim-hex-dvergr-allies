using UnityEngine;
using UnityEngine.EventSystems;

namespace HexDvergrAllies.Core.UI
{
    internal sealed class DvergrCompanionDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        private RectTransform _target;
        private Vector2 _pointerOffset;

        internal void Initialize(RectTransform target)
        {
            _target = target;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            var parent = _target.parent as RectTransform;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, eventData.position, eventData.pressEventCamera, out var pointerPosition))
            {
                return;
            }

            _pointerOffset = _target.anchoredPosition - pointerPosition;
        }

        public void OnDrag(PointerEventData eventData)
        {
            var parent = _target.parent as RectTransform;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, eventData.position, eventData.pressEventCamera, out var pointerPosition))
            {
                return;
            }

            _target.anchoredPosition = pointerPosition + _pointerOffset;
        }
    }
}
