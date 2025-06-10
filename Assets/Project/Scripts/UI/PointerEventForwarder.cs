using UnityEngine;
using UnityEngine.EventSystems;

public class PointerEventForwarder : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private MonoBehaviour _pointerEventTarget;

    private IPointerDownHandler _pointerDownHandler;
    private IPointerUpHandler _pointerUpHandler;

    public void Initialize(MonoBehaviour target)
    {
        _pointerEventTarget = target;

        // Cache the handlers
        if (_pointerEventTarget != null)
        {
            _pointerDownHandler = _pointerEventTarget as IPointerDownHandler;
            _pointerUpHandler = _pointerEventTarget as IPointerUpHandler;
        }

        if (target != null)
        {
            _pointerDownHandler = _pointerEventTarget as IPointerDownHandler;
            _pointerUpHandler = _pointerEventTarget as IPointerUpHandler;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Filter this, unity is spamming it for some reason
        if(!Input.GetMouseButtonDown(0))
        {
            return;
        }

        /*Debug.Log("[PointerEventForwarder] OnPointerDown: " 
                    + gameObject.name //eventData.pointerCurrentRaycast.gameObject?.name 
                    + " --> " 
                    + (pointerDownHandler != null ? ((MonoBehaviour)pointerDownHandler).gameObject.name : "NULL"), 
                    gameObject);*/
        _pointerDownHandler?.OnPointerDown(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // Filter this, unity is spamming it for some reason
        if(!Input.GetMouseButtonUp(0))
        {
            return;
        }

        /*Debug.Log("[PointerEventForwarder] OnPointerUp: " 
                    + gameObject.name //eventData.pointerCurrentRaycast.gameObject?.name 
                    + " --> " 
                    + (pointerUpHandler != null ? ((MonoBehaviour)pointerUpHandler).gameObject.name : "NULL"), 
                    gameObject);*/
        _pointerUpHandler?.OnPointerUp(eventData);
    }
}
