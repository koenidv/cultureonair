using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    public Player player;
    public PlanetSettings planetSettings;
    public DialogController dialogController;
    public CountryLookup countryLookup;
    public SpotifyController spotifyController;

    [Header("Computed values")]
    public Coordinate coordinates;
    public CountryInfo hoveredCountry;
    public CountryInfo selectedCountry;
    
    
    private CoordinateCalculator coordinateCalculator;

    private void OnEnable()
    {
        coordinateCalculator = new CoordinateCalculator(planetSettings.radius);
    }

    private void Start()
    {
        coordinates = coordinateCalculator.CalculateRealCoordinates(player.transform.position);
        CountryInfo? startCountry = countryLookup.LookupCountry(coordinates);
        if (startCountry != null) hoveredCountry = (CountryInfo)startCountry;
        selectedCountry = hoveredCountry;
        dialogController.DisplayWelcomeMessage();

        SelectCountry(selectedCountry);
    }

    void Update()
    {
        coordinates = coordinateCalculator.CalculateRealCoordinates(player.transform.position);
        CountryInfo newHovered = countryLookup.LookupCountry(coordinates) ?? hoveredCountry;

        if (!newHovered.Equals(hoveredCountry) && !newHovered.Equals(selectedCountry))
        {
            hoveredCountry = newHovered;
            bool accepted = dialogController.RequestCountrySwitch(hoveredCountry, OnAcceptCountry);
            if (accepted) spotifyController.PrepareCountry(hoveredCountry);
        }
        else if (newHovered.Equals(selectedCountry) && !newHovered.Equals(hoveredCountry))
        {
            hoveredCountry = newHovered;
            dialogController.CancelCountrySwitchRequest();
        }
    }

    private void OnAcceptCountry(CountryInfo country)
    {
        if (country.Equals(hoveredCountry))
        {
            SelectCountry(country);
        }
    }

    public void SelectCountry(CountryInfo country)
    {
        selectedCountry = country;
        spotifyController.SetCountry(country);
    }
}
