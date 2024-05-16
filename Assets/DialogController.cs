using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogController : MonoBehaviour
{
    public GameController gameController;

    public Image characterImage;
    public TextMeshProUGUI messageText;
    public Toggle autoSwitchToggle;
    public Button actionButton;
    public Text actionButtonText;

    public int durationWithoutAction;
    public bool autoSwitchCountries;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        autoSwitchCountries = autoSwitchToggle.isOn;
    }

    public void DisplayMessage(DialogMessage message)
    {
        messageText.text = message.message;
    }

    public bool RequestCountrySwitch(CountryInfo newCountry)
    {
        if (autoSwitchCountries)
        {
            DisplayMessage(new DialogMessage("Auto-changing to " + newCountry.c_name));
            return true;
        }
        else
        {
            DisplayMessage(new DialogMessage("Switch to " + newCountry.c_name, new DialogAction("Switch", gameController.SelectCountry)));
        }
        return autoSwitchCountries;
    }

    public void CancelCountrySwitchRequest()
    {

    }
}

public struct DialogMessage
{
    public string message;
    public DialogImage? image;
    public DialogAction? action;

    public DialogMessage(string message)
    {
        this.message = message;
        this.image = null;
        this.action = null;
    }

    public DialogMessage(string message, DialogAction action)
    {
        this.message = message;
        this.action = action;
        this.image = null;
    }

    public DialogMessage(string message, DialogImage image)
    {
        this.message = message;
        this.image = image;
        this.action = null;
    }
}

public struct DialogAction
{
    public string action;
    public Action<CountryInfo> callback;

    public DialogAction(string action, Action<CountryInfo> callback)
    {
        this.action = action;
        this.callback = callback;
    }
}

public enum DialogImage
{
    resting,
    waving
}