using UnityEngine;
using UnityEngine.EventSystems;

public class Press : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public bool isPressed { get; private set; }

    public void OnPointerDown(PointerEventData eventData) => isPressed = true;
    public void OnPointerUp(PointerEventData eventData) => isPressed = false;

}
