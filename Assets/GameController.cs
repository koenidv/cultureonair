using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    public Player player;
    public PlanetSettings planetSettings;
    public DialogController dialogController;
    public CountryLookup countryLookup;
    private CoordinateCalculator coordinateCalculator;

    [Header("Computed values")]
    public Coordinate coordinates;
    public CountryInfo hoveredCountry;
    public CountryInfo selectedCountry;

    private void OnEnable()
    {
        coordinateCalculator = new CoordinateCalculator(planetSettings.radius);
    }


    void Update()
    {
        coordinates = coordinateCalculator.CalculateRealCoordinates(player.transform.position);
        CountryInfo newHovered = countryLookup.LookupCountry(coordinates) ?? hoveredCountry;

        if (!newHovered.Equals(hoveredCountry) && !newHovered.Equals(selectedCountry))
        {
            hoveredCountry = newHovered;
            bool accepted = dialogController.RequestCountrySwitch(hoveredCountry);
            if (accepted) SelectCountry(hoveredCountry);
        }
        else if (newHovered.Equals(selectedCountry) && !newHovered.Equals(hoveredCountry))
        {
            dialogController.CancelCountrySwitchRequest();
        }

    }

    public void SelectCountry(CountryInfo country)
    {
        selectedCountry = country;
    }
}
