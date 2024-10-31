using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class GameCamera : MonoBehaviour
{
    [Header("Target")]
    public Player player;
    public ViewOptions forwardView;
    public ViewOptions topdownView;

    [Header("Camera")]
    public Camera cam;
    public float acceleration;
    public float defaultFOV;
    public float forwardFOV;
    public float backwardFOV;
    public float fovAcceleration;

    [Header("Touch Rotate")]
    public float modifierSpeed;
    public float modifierReturnSpeed;
    public float modifierSpringMaxValue;

    private ViewTypes currentView;

    private float playerMaxSpeed;
    private float playerCurrentSpeed;

    private Transform playerTransform;
    private Vector3 positionV;
    private float fovV;

    private LinearSpring modifierX;
    private bool modifierInChange;

    private void OnEnable()
    {
        modifierX = new LinearSpring(modifierSpringMaxValue);
    }
    void Start()
    {
        playerTransform = player.transform;
    }

    void LateUpdate()
    {
        UpdateView();
        UpdateFOV();
        modifierX.Decrease();
        if (playerCurrentSpeed > 10 || currentView == ViewTypes.topdown) modifierInChange = false;
        if (!modifierInChange) modifierX.Set(0);
    }

    public void UpdateModifierInput(Vector2 delta)
    {
        if (playerCurrentSpeed > 10) return;
        if (currentView == ViewTypes.topdown) return;
        modifierX.Add(delta.x * modifierSpeed);
    }

    public void UpdateModifierChanging(bool changing)
    {
        modifierInChange = changing;
    }

    void UpdateView()
    {
        ViewOptions options = GetCurrentViewOptions();

        // Calculate new position
        Vector3 newPos = playerTransform.position + playerTransform.position.normalized * options.cameraOffset.y + playerTransform.forward * options.cameraOffset.z;
        newPos = playerTransform.position + (Quaternion.Euler(0f, modifierX.Value, 0f) * (newPos - playerTransform.position));
        newPos = Vector3.SmoothDamp(transform.position, newPos, ref positionV, acceleration);

        //Calculate look playerTransform
        Vector3 lookTarget = playerTransform.position;
        lookTarget += playerTransform.right * options.targetOffset.x;
        lookTarget += playerTransform.up * options.targetOffset.y;
        lookTarget += playerTransform.forward * options.targetOffset.z;

        transform.position = newPos;
        transform.LookAt(lookTarget, playerTransform.position.normalized);
    }

    void UpdateFOV()
    {
        float fov = defaultFOV;
        if (playerCurrentSpeed > 0)
        {
            cam.fieldOfView = MapFOV(playerCurrentSpeed, forwardFOV);
        }
        else if (playerCurrentSpeed < 0)
        {
            cam.fieldOfView = MapFOV(playerCurrentSpeed, backwardFOV);
        }

        fov = Mathf.SmoothDamp(cam.fieldOfView, fov, ref fovV, fovAcceleration);
        cam.fieldOfView = fov;
    }

    ViewOptions GetCurrentViewOptions()
    {
        if (currentView == ViewTypes.forward) return forwardView;
        if (currentView == ViewTypes.topdown) return topdownView;
        return forwardView;
    }

    private float MapFOV(float speed, float limit)
    {
        return Util.MapfClamped(Mathf.Abs(speed), 0, playerMaxSpeed, defaultFOV, limit);
    }

    public void GotoNextViewType()
    {
        if (currentView == ViewTypes.forward)
            currentView = ViewTypes.topdown;
        else currentView = ViewTypes.forward;
    }

    public void SetPlayerMaxSpeed(float value) { playerMaxSpeed = value; }
    public void SetPlayerCurrentSpeed(float value) { playerCurrentSpeed = value; }

}

[System.Serializable]
public struct ViewOptions
{
    public Vector3 cameraOffset;
    public Vector3 targetOffset;

    public ViewOptions(Vector3 cameraOffset, Vector3 targetOffset)
    {
        this.cameraOffset = cameraOffset;
        this.targetOffset = targetOffset;
    }
}

enum ViewTypes
{
    forward,
    topdown
}