using UnityEngine;

[System.Serializable]
public struct Coordinate
{
    [Range(-180, 180)]
    public float longitude;
    [Range(-90, 90)]
    public float latitude;

    public Coordinate(float longitude, float latitude)
    {
        this.longitude = longitude;
        this.latitude = latitude;
    }

    // Return vector2 containing long/lat remapped to range [0,1]
    public Vector2 ToUV()
    {
        return new Vector2((longitude + 180) / (360), (latitude + 90) / 180);
    }
}
