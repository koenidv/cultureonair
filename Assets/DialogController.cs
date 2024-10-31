using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Runtime.InteropServices;

public class DialogController : MonoBehaviour
{
    public GameController gameController;
    public SpotifyView spotifyView;

    public Image characterImage;
    public TextMeshProUGUI messageText;
    public Toggle autoSwitchToggle;
    public Button actionButton;
    public TextMeshProUGUI actionButtonText;
    public Image progressBar;

    public DialogMessages messages;

    public float durationWithoutAction;
    public float durationWithAction;
    public bool autoSwitchCountries;
    public bool neverAutoSwitchOceans = true;

    public Sprite characterDefault;
    public Sprite characterWaving;

    public float animateAcceleration;
    private Vector2 animateTarget;
    private Vector2 positionV;

    private RectTransform rt;

    private Action pendingAction;
    private Action pendingOnHide;
    private DateTime dialogStartTime;
    private DateTime? hideDialogTime;

    private bool isMobile;
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern bool IsMobileBrowser();
#endif

    void Start()
    {
        rt = gameObject.GetComponent<RectTransform>();

        actionButton.onClick.AddListener(ExecutePendingAction);
        autoSwitchToggle.isOn = autoSwitchCountries;

        isMobile = Application.isMobilePlatform;
#if UNITY_WEBGL && !UNITY_EDITOR
        isMobile = IsMobileBrowser() == true;
#endif
    }


    void Update()
    {
        if (autoSwitchToggle.isOn && !autoSwitchCountries) ExecutePendingAction();
        autoSwitchCountries = autoSwitchToggle.isOn;

        if (rt.anchoredPosition != animateTarget)
        {
            rt.anchoredPosition = Vector2.SmoothDamp(rt.anchoredPosition, animateTarget, ref positionV, animateAcceleration);
        }

        UpdateProgressBar();
        if (hideDialogTime != null && hideDialogTime <= DateTime.Now)
        {
            Hide();
        }
    }

    void UpdateProgressBar()
    {
        if (hideDialogTime == null)
        {
            progressBar.transform.localScale = new Vector2(0, 1);
            return;
        }

        float progress = (float)(DateTime.Now - dialogStartTime).Ticks / (float)(((DateTime)hideDialogTime) - dialogStartTime).Ticks;
        progress = Mathf.Clamp01(progress);
        progressBar.transform.localScale = new Vector2(progress, 1);
    }

    public void DisplayMessage(DialogMessage message)
    {
        messageText.text = message.message;
        characterImage.sprite = message.image == DialogImage.waving ? characterWaving : characterDefault;

        if (message.action != null)
        {
            actionButton.gameObject.SetActive(true);
            actionButtonText.text = ((DialogAction)message.action).action;
            pendingAction = ((DialogAction)message.action).callback;
        }
        else
        {
            actionButton.gameObject.SetActive(false);
            pendingAction = null;
        }
        pendingOnHide = message.onHide;

        hideDialogTime = null;
        if (message.displayTime > 0)
        {
            hideDialogTime = DateTime.Now.AddSeconds(message.displayTime);
        }
        dialogStartTime = DateTime.Now;

        autoSwitchToggle.gameObject.SetActive(message.showAutoSwitchToggle);
        Show();
    }

    private void Show()
    {
        spotifyView.Hide();
        animateTarget = Vector2.zero;
    }

    private void Hide()
    {
        animateTarget = new Vector2(0, -rt.rect.size.y * 2);
        hideDialogTime = null;
        spotifyView.Show();
        pendingOnHide?.Invoke();
        pendingOnHide = null;
    }

    public bool RequestCountrySwitch(CountryInfo newCountry, Action<CountryInfo> onAcceptRequest)
    {
        bool switching = autoSwitchCountries;
        if (neverAutoSwitchOceans && newCountry.ocean) switching = false;
        if (switching)
        {
            DisplayMessage(new DialogMessage(GetSwitchMessage(newCountry.c_name, messages.autoSwitchMessages), durationWithoutAction)
            {
                onHide = () => onAcceptRequest(newCountry)
            });
            return true;
        }
        else
        {
            DisplayMessage(new DialogMessage(
                GetSwitchMessage(newCountry.c_name, messages.manualSwitchMessages),
                new DialogAction("Switch", () => onAcceptRequest(newCountry)),
                durationWithAction
                ));
        }
        return switching;
    }

    public void CancelCountrySwitchRequest()
    {
        pendingAction = null;
        Hide();
    }

    public void DisplayWelcomeMessage()
    {
        DialogMessage message = new DialogMessage(
            message: isMobile ? messages.welcomeMessageMobile : messages.welcomeMessageDesktop,
            DialogImage.waving,
            -1
            )
        {
            showAutoSwitchToggle = false
        };
        DisplayMessage(message);
    }

    private string GetSwitchMessage(string countryName, string[] templates)
    {
        System.Random rand = new System.Random();
        string template = templates[rand.Next(templates.Length)];
        string countryBold = $"<b>{countryName}</b>";
        return string.Format(template, countryBold);
    }

    private void ExecutePendingAction()
    {
        pendingAction?.Invoke();
        pendingAction = null;
        Hide();
    }

}

public struct DialogMessage
{
    public string message;
    public DialogImage image;
    public DialogAction? action;
    public Action onHide;
    public bool showAutoSwitchToggle;
    public float displayTime;

    public DialogMessage(string message, float duration)
    {
        this.message = message;
        this.image = DialogImage.resting;
        this.action = null;
        showAutoSwitchToggle = true;
        displayTime = duration;
        onHide = null;
    }

    public DialogMessage(string message, DialogAction action, float duration)
    {
        this.message = message;
        this.action = action;
        this.image = DialogImage.resting;
        showAutoSwitchToggle = true;
        displayTime = duration;
        onHide = null;
    }

    public DialogMessage(string message, DialogImage image, float duration)
    {
        this.message = message;
        this.image = image;
        this.action = null;
        showAutoSwitchToggle = true;
        displayTime = duration;
        onHide = null;
    }
}

public struct DialogAction
{
    public string action;
    public Action callback;

    public DialogAction(string action, Action callback)
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