using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.UI;

public class Compass : MonoBehaviour
{
    public RawImage image;

    private float heading;

    void LateUpdate()
    {
        image.uvRect = new Rect(heading / 360, 0f, 1f, 1f);
    }

    public void UpdateHeading(float angle)
    {
        this.heading = angle;
    }

}
