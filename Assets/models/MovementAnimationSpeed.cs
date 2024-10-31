using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementAnimationSpeed : MonoBehaviour
{
    public Player player;
    public Animator animator;

    public float horizontalMinModifier = 1, horizontalMaxModifier = 2;
    public float verticalMinModifier = 0, verticalMaxModifier = 2;

    public float verticalSpeedAdjustment = 2;

    private float maximumHorizontalSpeed, maximumVerticalSpeed;

    void Start()
    {
        maximumHorizontalSpeed = player.maxSpeed * player.boostFactor;
        maximumVerticalSpeed = player.maxVerticalSpeed / verticalSpeedAdjustment;
    }

    void Update()
    {
        animator.SetFloat("speed", CalculateSpeedModifier());
    }

    private float CalculateSpeedModifier()
    {
        float horizontalModifier = Util.MapfClamped(Mathf.Abs(player.currentSpeed), 0, maximumHorizontalSpeed, horizontalMinModifier, horizontalMaxModifier);
        float verticalModifier = Util.MapfClamped(player.currentElevationSpeed, -maximumVerticalSpeed, maximumVerticalSpeed, verticalMinModifier, verticalMaxModifier);
        return horizontalModifier * verticalModifier;
    }
}
