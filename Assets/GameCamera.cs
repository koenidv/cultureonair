using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class GameCamera : MonoBehaviour
{
    [Header("Target")]
    public Player player;
    public Vector3 cameraOffset;
    public Vector3 targetOffset;

    [Header("Camera")]
    public Camera cam;
    public float acceleration;
    public float defaultFOV;
    public float forwardFOV;
    public float backwardFOV;
    public float fovAcceleration;

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
        // Calculate new position
        Vector3 newPos = playerTransform.position + playerTransform.forward * cameraOffset.z + playerTransform.position.normalized * cameraOffset.y;
        newPos = Vector3.SmoothDamp(transform.position, newPos, ref positionV, acceleration);

        //Calculate look playerTransform
        Vector3 lookTarget = playerTransform.position;
        lookTarget += playerTransform.right * targetOffset.x;
        lookTarget += playerTransform.up * targetOffset.y;
        lookTarget += playerTransform.forward * targetOffset.z;

        transform.position = newPos;
        transform.LookAt(lookTarget, playerTransform.position.normalized);
    }

    void UpdateFOV()
    {
        float fov = defaultFOV;
        if (playerCurrentSpeed > 0) {
            cam.fieldOfView = MapFOV(playerCurrentSpeed, forwardFOV);
        }
        else if (playerCurrentSpeed < 0) {
            cam.fieldOfView = MapFOV(playerCurrentSpeed, backwardFOV);
        }

        fov = Mathf.SmoothDamp(cam.fieldOfView, fov, ref fovV, fovAcceleration);
        cam.fieldOfView = fov;
    }

    private float MapFOV(float speed, float limit)
    {
        return Util.Mapf(Mathf.Abs(speed), 0, playerMaxSpeed, defaultFOV, limit);
    }

    public void SetPlayerMaxSpeed(float value) { playerMaxSpeed = value; }
    public void SetPlayerCurrentSpeed(float value) { playerCurrentSpeed = value; }
}
