using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class UIController : MonoBehaviour
{
    public Player player;
    public PlanetSettings planetSettings;

    [Header("UI Components")]
    public TextMeshProUGUI locationText;

    public CountryLookup countryLookup;

    private CoordinateCalculator coordinateCalculator;

    private void OnEnable()
    {
        coordinateCalculator = new CoordinateCalculator(planetSettings.radius);
    }


    void Update()
    {
        Coordinate realCoordinates = coordinateCalculator.CalculateRealCoordinates(player.transform.position);

        string countryName = countryLookup.LookupCountry(realCoordinates)?.c_name;
        if (countryName != null)
        {
            locationText.text = countryName;
        }
        else
        {
            locationText.text = $"Location\nlat: {Mathf.Round(realCoordinates.latitude * 1000) / 1000}\nlng: {Mathf.Round(realCoordinates.longitude * 1000) / 1000}\nGo and find Africa ;)";
        }
    }
}
