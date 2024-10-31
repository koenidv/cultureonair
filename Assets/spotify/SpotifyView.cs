using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class SpotifyView : MonoBehaviour
{
    public TextMeshProUGUI playlistNameText;
    public GameObject listItemTemplate;
    public Transform content;
    public Transform spinner;
    public AudioSource audioSource;
    public GameObject minimizeButton;
    public Sprite minimizeSprite, maximizeSprite;
    public ScrollRect scrollRect;
    public ParticleSystem particles;

    public bool autoPlayFirstSong = true;
    public float initialEaseDuration;
    private float initialEaseV;

    private List<(SongDetails, SpotifyListItem)> songsAndListItems;
    private int? currentPlayingIndex = null;
    private bool playedFirstSong;

    public float animateAcceleration;
    private Vector2 animateTarget, positionV, sizeTarget, sizeV;
    public bool isMinimized = false;

    private RectTransform rt;
    private float rtDefaultHeight;

    private void OnEnable()
    {
        rt = transform.Find("SpotifyBox").GetComponent<RectTransform>();
    }

    void Start()
    {
        songsAndListItems = new List<(SongDetails, SpotifyListItem)>();
        listItemTemplate.SetActive(false);

        spinner = spinner != null ? spinner : transform.Find("SpotifyBox/Spinner");
        audioSource = audioSource != null ? audioSource : gameObject.AddComponent<AudioSource>();
        audioSource.volume = 0f;
        scrollRect = scrollRect != null ? scrollRect : transform.Find("SpotifyBox/Scroll View").GetComponent<ScrollRect>();

        minimizeButton = minimizeButton != null ? minimizeButton : transform.Find("MinimizeButton").gameObject;
        minimizeButton.GetComponent<Button>().onClick.AddListener(ToggleMinimized);

        rtDefaultHeight = rt.rect.size.y;
        animateTarget = new Vector2(0, -rt.rect.size.y * 2);
        sizeTarget = rt.sizeDelta;
    }

    private void Update()
    {
        if (rt.anchoredPosition != animateTarget)
        {
            rt.anchoredPosition = Vector2.SmoothDamp(rt.anchoredPosition, animateTarget, ref positionV, animateAcceleration);
        }
        if (rt.sizeDelta != sizeTarget)
        {
            rt.sizeDelta = Vector2.SmoothDamp(rt.sizeDelta, sizeTarget, ref sizeV, animateAcceleration);
            if (Math.Abs(sizeV.y) > 0.2) UpdateScrollrectPosition();
        }

        if (playedFirstSong && audioSource.volume != 1f)
        {
            audioSource.volume = Mathf.SmoothDamp(audioSource.volume, 1f, ref initialEaseV, initialEaseDuration);
        }
    }

    public void Show()
    {
        animateTarget = Vector2.zero;
    }

    public void Hide()
    {
        animateTarget = new Vector2(0, -rt.rect.size.y * 2);
    }

    public void ToggleMinimized()
    {
        isMinimized = !isMinimized;
        minimizeButton.GetComponent<Image>().sprite = isMinimized ? maximizeSprite : minimizeSprite;
        sizeTarget = new Vector2(rt.sizeDelta.x, rtDefaultHeight * (isMinimized ? 0.5f : 1f));
        UpdateScrollrectPosition();
    }

    private void UpdateScrollrectPosition(int? songIndex = null)
    {
        float position = Util.MapfClamped((float)(songIndex ?? currentPlayingIndex), 0, songsAndListItems.Count - 1, 0, 1);
        scrollRect.verticalNormalizedPosition = 1 - position;
    }

    public void SetLoading(bool loading)
    {
        spinner.gameObject.SetActive(loading);
    }

    public void SetPlaylist(PlaylistDetails playlist)
    {
        SetLoading(false);
        playlistNameText.text = playlist.name;
        PopulateList(playlist.songs);
        if (autoPlayFirstSong && songsAndListItems.Count > 0) PlayPreview(0);
    }

    private void PopulateList(SongDetails[] songs)
    {
        ClearList();
        int i = 0;
        foreach (var song in songs)
        {
            songsAndListItems.Add((song, CreateSongItem(song, i++)));
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(content.GetComponent<RectTransform>());
        UpdateScrollrectPosition(0);
    }

    private SpotifyListItem CreateSongItem(SongDetails song, int index)
    {
        GameObject listObject = Instantiate(listItemTemplate, content);
        listObject.SetActive(true);
        SpotifyListItem item = listObject.GetComponent<SpotifyListItem>();
        item.Initialize(song, () => { if (currentPlayingIndex == index) PausePreview(index); else PlayPreview(index); });
        return item;
    }

    private void PlayPreview(int index)
    {
        if (index >= songsAndListItems.Count || index < 0) return;
        (SongDetails song, SpotifyListItem item) = songsAndListItems[index];

        item.SetState(PlayState.loading);

        if (currentPlayingIndex != null)
        {
            songsAndListItems[(int)currentPlayingIndex].Item2.SetState(PlayState.pausing);
        }
        currentPlayingIndex = index;
        playedFirstSong = true;

        Action finishedLoading = () =>
        {
            item.SetState(PlayState.playing);
        };

        Action audioCompleted = () =>
        {
            if (currentPlayingIndex != index) return;
            item.SetState(PlayState.pausing);
            currentPlayingIndex = null;
            if (index + 1 < songsAndListItems.Count)
            {
                PlayPreview(index + 1);
                if (isMinimized) UpdateScrollrectPosition(index + 1);
            }
        };

        StartCoroutine(StreamAudioFromUrl(song.previewUrl, finishedLoading, audioCompleted));
    }

    private void PausePreview(int index)
    {
        currentPlayingIndex = null;
        audioSource.Pause();
        particles.Stop();
        if (index >= songsAndListItems.Count || index < 0) return;
        songsAndListItems[index].Item2.SetState(PlayState.pausing);
    }

    private IEnumerator StreamAudioFromUrl(string url, Action onFinishedLoading, Action onAudioCompleted)
    {
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError(www.error);
            }
            else
            {
                onFinishedLoading?.Invoke();
                AudioClip audioClip = DownloadHandlerAudioClip.GetContent(www);
                audioSource.clip = audioClip;
                audioSource.Play();
                particles.Play();
                StartCoroutine(CheckAudioCompleted(onAudioCompleted));
            }
        }
    }

    private IEnumerator CheckAudioCompleted(Action onAudioCompleted)
    {
        while (audioSource.isPlaying)
        {
            yield return null;
        }
        onAudioCompleted?.Invoke();
    }

    private void ClearList()
    {
        foreach (Transform child in content)
        {
            Destroy(child.gameObject);
        }
        songsAndListItems.Clear();
    }

    private void OnDestroy()
    {
        ClearList();
    }
}
