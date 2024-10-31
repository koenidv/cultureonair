using UnityEngine;

[CreateAssetMenu(fileName = "DialogMessages", menuName = "ScriptableObject/Dialog Messages")]
public class DialogMessages : ScriptableObject
{
    public string welcomeMessageDesktop;
    public string welcomeMessageMobile;

    public string errorMessage;

    public string[] autoSwitchMessages;
    public string[] manualSwitchMessages;
}