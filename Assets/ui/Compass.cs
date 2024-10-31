using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.UI;

public class Compass : MonoBehaviour
{
    public RawImage image;
    public Transform target;
    public PlanetSettings planetSettings;

    [SerializeField] private CoordinateCalculator coordinateCalculator;

    private void Start()
    {
        coordinateCalculator = new CoordinateCalculator(planetSettings.radius);
    }

    void LateUpdate()
    {
        float heading = coordinateCalculator.CalculateHeading(target.position, target.forward);
        image.uvRect = new Rect(heading / 360, 0f, 1f, 1f);
    }

}
