using System.Runtime.InteropServices;
using UnityEngine;

public class PlatformSpecificObject : MonoBehaviour
{
    public PlatformSelection platform;
    public bool debugAlwaysShow = false;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern bool IsMobileBrowser();
#endif

    void Start()
    {
        bool mobileOnly = platform == PlatformSelection.mobileOnly;
        bool isMobile = Application.isMobilePlatform;
#if UNITY_WEBGL && !UNITY_EDITOR
        isMobile = IsMobileBrowser() == true;
#endif
        gameObject.SetActive((mobileOnly && isMobile) || debugAlwaysShow);
        print(isMobile ? "Detected mobile platform" : "Detected desktop platform");
    }
}

public enum PlatformSelection
{
    desktopOnly,
    mobileOnly
}
