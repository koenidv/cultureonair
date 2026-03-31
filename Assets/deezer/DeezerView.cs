using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class DeezerView : MonoBehaviour
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

    private List<(SongDetails, DeezerListItem)> songsAndListItems;
    private List<SongDetails> songBuffer;
    private int? currentPlayingIndex = null;
    private bool playedFirstSong;
    private int targetListSize;

    public float animateAcceleration;
    private Vector2 animateTarget, positionV, sizeTarget, sizeV;
    public bool isMinimized = false;

    private RectTransform rt;
    private float rtDefaultHeight;

    private void OnEnable()
    {
        rt = transform.Find("DeezerBox").GetComponent<RectTransform>();
    }

    void Start()
    {
        songsAndListItems = new List<(SongDetails, DeezerListItem)>();
        songBuffer = new List<SongDetails>();
        listItemTemplate.SetActive(false);

        spinner = spinner != null ? spinner : transform.Find("DeezerBox/Spinner");
        audioSource = audioSource != null ? audioSource : gameObject.AddComponent<AudioSource>();
        audioSource.volume = 0f;
        scrollRect = scrollRect != null ? scrollRect : transform.Find("DeezerBox/Scroll View").GetComponent<ScrollRect>();

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

    public void SetPlaylist(PlaylistDetails playlist, int targetSize)
    {
        SetLoading(false);
        if (playlistNameText != null) playlistNameText.text = playlist.name;
        PopulateList(playlist.songs, targetSize);
        if (autoPlayFirstSong && songsAndListItems.Count > 0) PlayPreview(0);
    }

    private void PopulateList(SongDetails[] songs, int targetSize)
    {
        ClearList();
        targetListSize = targetSize;
        
        // Take the first targetSize songs for the UI
        int initialCount = Math.Min(songs.Length, targetSize);
        for (int i = 0; i < initialCount; i++)
        {
            songsAndListItems.Add((songs[i], CreateSongItem(songs[i])));
        }

        // Store the rest in a buffer
        songBuffer = new List<SongDetails>();
        if (songs.Length > initialCount)
        {
            for (int i = initialCount; i < songs.Length; i++)
            {
                songBuffer.Add(songs[i]);
            }
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(content.GetComponent<RectTransform>());
        UpdateScrollrectPosition(0);
    }

    private DeezerListItem CreateSongItem(SongDetails song)
    {
        GameObject listObject = Instantiate(listItemTemplate, content);
        listObject.SetActive(true);
        DeezerListItem item = listObject.GetComponent<DeezerListItem>();
        item.Initialize(song, () => OnSongItemClicked(item));
        return item;
    }

    private void OnSongItemClicked(DeezerListItem item)
    {
        int index = songsAndListItems.FindIndex(x => x.Item2 == item);
        if (index == -1) return;

        if (currentPlayingIndex == index) PausePreview(index);
        else PlayPreview(index);
    }

    private void PlayPreview(int index)
    {
        if (index >= songsAndListItems.Count || index < 0) return;
        (SongDetails song, DeezerListItem item) = songsAndListItems[index];

        item.SetState(PlayState.loading);

        if (currentPlayingIndex != null && currentPlayingIndex < songsAndListItems.Count)
        {
            songsAndListItems[(int)currentPlayingIndex].Item2.SetState(PlayState.pausing);
        }
        currentPlayingIndex = index;
        playedFirstSong = true;

        Action finishedLoading = () =>
        {
            if (currentPlayingIndex == index)
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

        StartCoroutine(StreamAudioFromUrl(song.previewUrl, index, finishedLoading, audioCompleted));
    }

    private void PausePreview(int index)
    {
        currentPlayingIndex = null;
        audioSource.Pause();
        particles.Stop();
        if (index >= songsAndListItems.Count || index < 0) return;
        songsAndListItems[index].Item2.SetState(PlayState.pausing);
    }

    private IEnumerator StreamAudioFromUrl(string url, int index, Action onFinishedLoading, Action onAudioCompleted)
    {
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG))
        {
            yield return www.SendWebRequest();

            if (index >= songsAndListItems.Count || currentPlayingIndex != index) yield break;

            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"Error streaming audio from {url}: {www.error} (Response Code: {www.responseCode})");
                RemoveBrokenSong(index);
                if (index < songsAndListItems.Count) PlayPreview(index);
                else onAudioCompleted?.Invoke();
            }
            else
            {
                string contentType = www.GetResponseHeader("Content-Type");
                if (string.IsNullOrEmpty(contentType) || !contentType.ToLower().Contains("audio"))
                {
                    Debug.LogError($"Invalid content type for audio stream from {url}: {contentType}. Expected audio/mpeg.");
                    RemoveBrokenSong(index);
                    if (index < songsAndListItems.Count) PlayPreview(index);
                    else onAudioCompleted?.Invoke();
                    yield break;
                }

                onFinishedLoading?.Invoke();
                AudioClip audioClip = DownloadHandlerAudioClip.GetContent(www);
                if (audioClip == null)
                {
                    Debug.LogError($"Failed to create AudioClip from {url}");
                    RemoveBrokenSong(index);
                    if (index < songsAndListItems.Count) PlayPreview(index);
                    else onAudioCompleted?.Invoke();
                    yield break;
                }
                audioSource.clip = audioClip;
                audioSource.Play();
                particles.Play();
                StartCoroutine(CheckAudioCompleted(onAudioCompleted));
            }
        }
    }

    private void RemoveBrokenSong(int index)
    {
        if (index < 0 || index >= songsAndListItems.Count) return;

        (SongDetails song, DeezerListItem item) = songsAndListItems[index];
        Debug.LogWarning($"Filtering out broken song: {song.name} by {song.artist}");

        // Destroy the UI element
        Destroy(item.gameObject);
        
        // Remove from our list
        songsAndListItems.RemoveAt(index);

        // Adjust currentPlayingIndex if needed
        if (currentPlayingIndex == index) currentPlayingIndex = null;
        else if (currentPlayingIndex > index) currentPlayingIndex--;

        // Append from buffer if available
        if (songBuffer != null && songBuffer.Count > 0)
        {
            SongDetails nextCandidate = songBuffer[0];
            songBuffer.RemoveAt(0);
            songsAndListItems.Add((nextCandidate, CreateSongItem(nextCandidate)));
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(content.GetComponent<RectTransform>());
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
