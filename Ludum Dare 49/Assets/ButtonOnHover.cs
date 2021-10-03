using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonOnHover : MonoBehaviour, IPointer​Enter​Handler
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        SoundDesigner.Instance.PlayMenuHoveredEffect();
    }
}
