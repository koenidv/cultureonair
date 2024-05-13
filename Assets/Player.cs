using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using static UnityEngine.ParticleSystem;
using UnityEngine.Rendering;

public class Player : MonoBehaviour
{
    [Header("Positioning")]
    public float maxSpeed;
    public float maxVerticalSpeed;
    public float turnSpeed;
    public float boostFactor;
    public float minElevation;
    public float maxElevation;
    public float currentElevation;

    [Header("Smoothing")]
    public float forwardAcceleration;
    public float turnAcceleration;
    public float verticalAcceleration;
    public float boostAcceleration;

    [Header("Inner Transform")]
    public Transform playerTransform;
    public float maxPitch;
    public float maxRoll;

    [Header("Display")]
    public bool drawPositionLine;
    public Material positionLineMaterial;
    public float positionLineSpeed;

    GameCamera gameCamera;

    float worldRadius;
    float inputForward;
    float inputTurn;
    float inputVertical;
    bool inputBoost;
    float totalTurnAngle;

    float smoothedForward;
    float forwardSmoothV;
    float verticalSmoothV;
    float smoothedTurnSpeed;
    float turnSmoothV;
    float smoothedBoostFactor;
    float boostSmoothV;

    GameObject positionLine;


    // Start is called before the first frame update
    void Start()
    {
        gameCamera = FindObjectOfType<GameCamera>();
        if (drawPositionLine) CreatePositionLine();
    }

    // Update is called once per frame
    void Update()
    {

        // Elevation
        currentElevation = CalculateElevation(inputVertical);

        // Forward Speed
        float adjustedBoost = inputBoost && inputForward > 0 ? boostFactor : 1f;
        smoothedBoostFactor = Mathf.SmoothDamp(smoothedBoostFactor, adjustedBoost, ref boostSmoothV, boostAcceleration);
        smoothedForward = Mathf.SmoothDamp(smoothedForward, inputForward * maxSpeed * smoothedBoostFactor, ref forwardSmoothV, forwardAcceleration);
        UpdatePosition(smoothedForward);

        // Turn
        smoothedTurnSpeed = Mathf.SmoothDamp(smoothedTurnSpeed, inputTurn * turnSpeed, ref turnSmoothV, turnAcceleration);
        float turnAmount = smoothedTurnSpeed * Time.deltaTime;
        totalTurnAngle += turnAmount;
        UpdateRotation(turnAmount);

        UpdateInnerRotation(smoothedForward, smoothedTurnSpeed);

        if (drawPositionLine) DrawPositionLine(transform.position, smoothedForward);
    }

    private float CalculateElevation(float vVertical)
    {
        float target = currentElevation + vVertical * maxVerticalSpeed;
        target = Mathf.Clamp(target, minElevation, maxElevation);
        float elevation = Mathf.SmoothDamp(currentElevation, target, ref verticalSmoothV, verticalAcceleration);
        return elevation;
    }

    private void UpdatePosition(float vForward)
    {
        Vector3 velocity = transform.forward * vForward;
        Vector3 newPos = transform.position + velocity * Time.deltaTime;
        newPos = newPos.normalized * (worldRadius + currentElevation);
        transform.position = newPos;
    }

    void UpdateRotation(float turnAngle)
    {
        Vector3 gravityUp = transform.position.normalized;
        transform.RotateAround(transform.position, gravityUp, turnAngle);
        transform.LookAt((transform.position + transform.forward * 10).normalized * (worldRadius + currentElevation), gravityUp);
        transform.rotation = Quaternion.FromToRotation(transform.up, gravityUp) * transform.rotation;

        playerTransform.rotation = playerTransform.rotation.normalized * Quaternion.Euler(2, 1, 1);
    }

    void UpdateInnerRotation(float vForward, float vTurn)
    {
        float pitch = Util.Mapf(vForward, -maxSpeed, maxSpeed, -maxPitch, maxPitch);
        float roll = Util.Mapf(vTurn * vForward, -turnSpeed * maxSpeed, turnSpeed * maxSpeed, -maxRoll, maxRoll);
        playerTransform.localRotation = Quaternion.Euler(pitch, 0, -roll);
    }

    void CreatePositionLine()
    {
        positionLine = new GameObject();
        positionLine.AddComponent<LineRenderer>();
        LineRenderer lr = positionLine.GetComponent<LineRenderer>();
        lr.material = positionLineMaterial;
        lr.startColor = Color.black;
        lr.endColor = Color.black;
        lr.startWidth = 0.1f;
        lr.endWidth = 0.1f;
        lr.SetPosition(0, Vector3.zero);
    }

    void DrawPositionLine(Vector3 position, float speed)
    {
        if (position == null) return;
        LineRenderer lr = positionLine.GetComponent<LineRenderer>();
        Vector3 target = Mathf.Abs(speed) < 0.1 ? position : Vector3.zero;
        lr.SetPosition(1, Vector3.Lerp(lr.GetPosition(1), target, Time.deltaTime * positionLineSpeed));
    }

    private void OnDestroy()
    {
        if (positionLine != null)
        {
            GameObject.Destroy(positionLine);
        }
    }


    // public

    public void UpdateInput(float forward, float rotate, float vertical, bool boosting)
    {
        this.inputForward = forward;
        this.inputTurn = rotate;
        this.inputVertical = vertical;
        this.inputBoost = boosting;
    }

    public void SetWorldRadius(float radius)
    {
        worldRadius = radius;
    }

    public float currentSpeed
    {
        get
        {
            return smoothedForward;
        }
    }


}
