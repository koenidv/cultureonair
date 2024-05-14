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

    private ViewTypes currentView;

    private float playerMaxSpeed;
    private float playerCurrentSpeed;

    private Transform playerTransform;
    private Vector3 positionV;
    private float fovV;

    // Start is called before the first frame update
    void Start()
    {
        playerTransform = player.transform;
    }

    void LateUpdate()
    {
        UpdateView();
        UpdateFOV();
    }

    void UpdateView()
    {
        ViewOptions options = GetCurrentViewOptions();

        // Calculate new position
        Vector3 newPos = playerTransform.position + playerTransform.forward * options.cameraOffset.z + playerTransform.position.normalized * options.cameraOffset.y;
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
        return Util.Mapf(Mathf.Abs(speed), 0, playerMaxSpeed, defaultFOV, limit);
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