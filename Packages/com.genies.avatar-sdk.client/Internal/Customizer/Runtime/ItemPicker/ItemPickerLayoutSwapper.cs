using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Genies.Customization.Framework.ItemPicker
{
    public class ItemPickerLayoutSwapper : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public ScrollingItemPicker horizontalItemPicker;
        public ExpandableGalleryItemPicker galleryItemPicker;
        public float swipePixelThreshold;
        public float swipeTimeThreshold;

        private Vector2 _dragStartPos;
        private Vector2 _dragEndPos;
        private float _dragStartTime;
        private bool _dragStarted;
        private bool _didSwipe = false;
        protected void Awake()
        {
            horizontalItemPicker.Hidden += Hide;
            galleryItemPicker.SelectionChanged += Refresh;
        }

        private void Refresh()
        {
            horizontalItemPicker.RefreshData().Forget();
        }

        protected void OnDestroy()
        {
            horizontalItemPicker.Hidden -= Hide;
            galleryItemPicker.SelectionChanged -= Refresh;

        }

        private void Hide()
        {
            galleryItemPicker.Hide();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _didSwipe = false;
            _dragStarted = true;
            _dragStartPos = eventData.pressPosition;
            _dragStartTime = Time.unscaledTime;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_didSwipe && _dragStarted && !galleryItemPicker.IsTransitioning && horizontalItemPicker.Source != null && !horizontalItemPicker.IsInitializingCount)
            {
                galleryItemPicker.Show(horizontalItemPicker.Source);
            }

            //reset the variables
            _dragStartPos = Vector2.zero;
            _dragEndPos = Vector2.zero;
            _dragStartTime = -1;
            _dragStarted = false;
            _didSwipe = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_dragStarted)
            {
                _dragEndPos = eventData.position;

                var delta        = _dragEndPos - _dragStartPos;
                var deltaSeconds = Time.unscaledTime - _dragStartTime;
                var isVertical   = Mathf.Abs(delta.y) >= Mathf.Abs(delta.x);
                var isUp         = delta.y > 0;


                if (delta.magnitude > swipePixelThreshold && isVertical && isUp && deltaSeconds <= swipeTimeThreshold)
                {
                    _didSwipe = true;
                }
                else
                {
                    _didSwipe = false;
                }
            }
        }
    }
}
