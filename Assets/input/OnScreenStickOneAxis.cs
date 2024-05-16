using System;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.UI;

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.OnScreen;

public class OnScreenStickOneAxis : OnScreenControl, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    public float movementRange = 50;
    [InputControl(layout = "Vector2")]
    [SerializeField]
    private string m_ControlPath;
    public Axis axis;

    private Vector3 startPos;
    private Vector2 pointerDownPos;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData == null)
            throw new System.ArgumentNullException(nameof(eventData));

        BeginInteraction(eventData.position, eventData.pressEventCamera);
    }

    /// <summary>
    /// Callback to handle OnDrag UI events.
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (eventData == null)
            throw new System.ArgumentNullException(nameof(eventData));

        MoveStick(eventData.position, eventData.pressEventCamera);
    }

    /// <summary>
    /// Callback to handle OnPointerUp UI events.
    /// </summary>
    public void OnPointerUp(PointerEventData eventData)
    {
        EndInteraction();
    }

    private void Start()
    {
        startPos = ((RectTransform)transform).anchoredPosition;
    }

    private void BeginInteraction(Vector2 pointerPosition, Camera uiCamera)
    {
        var canvasRect = transform.parent?.GetComponentInParent<RectTransform>();
        if (canvasRect == null)
        {
            Debug.LogError("OnScreenStick needs to be attached as a child to a UI Canvas to function properly.");
            return;
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, pointerPosition, uiCamera, out pointerDownPos);
    }

    private void MoveStick(Vector2 pointerPosition, Camera uiCamera)
    {
        var canvasRect = transform.parent?.GetComponentInParent<RectTransform>();
        if (canvasRect == null)
        {
            Debug.LogError("OnScreenStick needs to be attached as a child to a UI Canvas to function properly.");
            return;
        }
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, pointerPosition, uiCamera, out var position);
        var delta = position - pointerDownPos;

        delta = Vector2.ClampMagnitude(delta, movementRange);
        if (axis == Axis.x) delta.y = 0;
        else delta.x = 0;

        ((RectTransform)transform).anchoredPosition = (Vector2)startPos + delta;

        var newPos = new Vector2(delta.x / movementRange, delta.y / movementRange);
        SendValueToControl(newPos);
    }

    private void EndInteraction()
    {
        ((RectTransform)transform).anchoredPosition = pointerDownPos = startPos;
        SendValueToControl(Vector2.zero);
    }

    protected override string controlPathInternal
    {
        get => m_ControlPath;
        set => m_ControlPath = value;
    }
}

public enum Axis
{
    x,
    y
}