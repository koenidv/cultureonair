using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class CoordinateCalculator
{
    private float worldRadius;
    public CoordinateCalculator(float worldRadius)
    {
        this.worldRadius = worldRadius;
    }

    public Coordinate CalculateRealCoordinates(Vector3 position)
    {
        Vector3 projectedPosition = position.normalized * worldRadius;

        // Calculate spherical coordinates
        float latitude = Mathf.Asin(projectedPosition.y / worldRadius) * Mathf.Rad2Deg;
        float longitude = Mathf.Atan2(projectedPosition.x, -projectedPosition.z) * Mathf.Rad2Deg;

        // Convert longitude to be in the range [-180, 180]
        longitude = (longitude + 360) % 360;
        if (longitude > 180) longitude -= 360;

        return new Coordinate(longitude, latitude);
    }

    public float CalculateHeading(Vector3 position, Vector3 forward)
    {
        Vector3 projectedNorth = CalculateProjectedNorth(position);
        return Vector3.SignedAngle(-projectedNorth, forward, position);
    }

    private Vector3 CalculateProjectedNorth(Vector3 position)
    {
        Vector3 simplifiedNorth = Vector3.up * worldRadius;
        Vector3 greatCircleNormal = Vector3.Cross(position, simplifiedNorth);
        Vector3 projectedNorth = Vector3.Cross(position, greatCircleNormal).normalized;
        return projectedNorth;
        
    }
}
