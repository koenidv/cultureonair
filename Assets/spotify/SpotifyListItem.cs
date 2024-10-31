using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SpotifyListItem : MonoBehaviour
{
    public Sprite playSprite;
    public Sprite pauseSprite;

    private Transform spinner;
    private TextMeshProUGUI songName, artistName;
    private Button playButton;
    private Image playButtonImage;

    private void OnEnable()
    {
        spinner = transform.Find("Spinner");
        songName = transform.Find("vertical/SongNameText").GetComponent<TextMeshProUGUI>();
        artistName = transform.Find("vertical/SongArtistText").GetComponent<TextMeshProUGUI>();
        playButton = transform.GetComponent<Button>();
        playButtonImage = transform.Find("PlayButton").GetComponent<Image>();
    }

    public void Initialize(SongDetails song, Action playButtonCallback)
    {
        songName.text = song.name;
        artistName.text = song.artist;
        playButton.onClick.AddListener(() => playButtonCallback?.Invoke());
        spinner.gameObject.SetActive(false);
    }

    public void SetState(PlayState state)
    {
        switch (state)
        {
            case PlayState.loading: SetLoading(true); break;
            case PlayState.playing: SetPlaying(true); break;
            case PlayState.pausing: SetPlaying(false); break;
        }
    }

    private void SetPlaying(bool playing)
    {
        SetLoading(false);
        playButtonImage.sprite = playing ? pauseSprite : playSprite;
    }

    private void SetLoading(bool loading)
    {
        spinner.gameObject.SetActive(loading);
        playButtonImage.gameObject.SetActive(!loading);
    }

}

public enum PlayState
{
    loading,
    playing,
    pausing
}