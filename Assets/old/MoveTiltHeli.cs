using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MoveTiltHeli : MonoBehaviour
{

    public float _tiltSpeed = 2f;
    public float _turnSpeed = 2f;
    public float _maxTilt = 30f;

    private Vector2 tilt = new Vector2(0f, 0f);

    void Update()
    {
        var horizontal = Input.GetAxisRaw("Horizontal");
        if (horizontal == 0/* || BothPositiveOrNegative(tilt.x, horizontal)*/)
            tilt.x = SubtractTowardsZero(tilt.x, _tiltSpeed);
        if (horizontal != 0)
            tilt.x = Mathf.Clamp(tilt.x + Input.GetAxisRaw("Horizontal"), -_maxTilt, _maxTilt);

        var vertical = Input.GetAxisRaw("Vertical");
        if (vertical == 0/* || BothPositiveOrNegative(tilt.y, -vertical)*/)
            tilt.y = SubtractTowardsZero(tilt.y, _tiltSpeed);
        if (vertical != 0)
            tilt.y = Mathf.Clamp(tilt.y + Input.GetAxisRaw("Vertical"), -_maxTilt, _maxTilt);

        Vector2 targetDirection = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        transform.rotation = Quaternion.Euler(tilt.y, 0f, -tilt.x);
    }

    bool BothPositiveOrNegative(float a, float b)
    {
        return (a < 0 && b < 0) || (a > 0 && b > 0);
    }

    float SubtractTowardsZero(float a, float subtract)
    {
        if (a > -subtract && a < subtract) return 0;
        else if (a < 0) return a + subtract;
        else if (a > 0) return a - subtract;
        else return a;
    }
}
