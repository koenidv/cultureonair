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
        this.longitude = 0;
        this.latitude = 0;
        AddLongitude(longitude);
        AddLatitude(latitude);
    }

    public void Set(Vector2 longAndLat)
    {
        this.longitude = 0;
        this.latitude = 0;
        AddLongitude(longitude);
        AddLatitude(latitude);
    }

    public void Add(Vector2 longAndLat)
    {
        AddLongitude(longAndLat.x);
        AddLatitude(longAndLat.y);
    }

    public void AddLongitude(float longitude)
    {
        this.longitude = (this.longitude + longitude + 180) % 360 - 180;
    }

    public void AddLatitude(float latitude)
    {
        this.latitude = (this.latitude + latitude + 180) % 360 - 180;
    }

    public void AddLatitudeClamped(float latitude)
    {
        this.latitude = Mathf.Clamp(this.latitude + latitude, -90, 90);
    }

    // Return vector2 containing long/lat remapped to range [0,1]
    public Vector2 ToUV()
    {
        return new Vector2((longitude + 180) / (360), (latitude + 90) / 180);
    }

    // Return vector2 containing long/lat remapped to range [-pi/2-pi/2; -pi, pi]
    public Vector2 ToRadians()
    {
        return new Vector2(longitude * Mathf.PI, latitude * Mathf.PI / 2f);
    }

    public Vector2 ToVector2()
    {
        return new Vector2(longitude, latitude);
    }

    public Vector3 ToPoint()
    {
        Vector2 rad = ToRadians();
        return new Vector3(
            Mathf.Cos(rad.x) * Mathf.Cos(rad.y),
            Mathf.Sin(rad.y),
            Mathf.Sin(rad.x) * Mathf.Cos(rad.y)
            );
    }

    public readonly Quaternion GetHorizontalRotation()
    {
        return Quaternion.Euler(0f, longitude, 0f).normalized;
    }

    public readonly Quaternion GetVerticalRotation()
    {
        return Quaternion.Euler(-latitude, 0f, 0f).normalized;
    }

}
