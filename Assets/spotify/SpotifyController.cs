using System;
using UnityEngine;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using UnityEngine.Networking;
using System.Web;
using System.Net;
using LitJson;

// Classes for deserializing Spotify API response
[System.Serializable]
public class SpotifyPlaylistResponse
{
    public string name;
    public SpotifyPlaylistTracks tracks;
}

[System.Serializable]
public class SpotifyPlaylistTracks
{
    public SpotifyTrack[] items;
}

[System.Serializable]
public class SpotifyTrack
{
    public SpotifyTrackInfo track;
}

[System.Serializable]
public class SpotifyTrackInfo
{
    public string name;
    public SpotifyArtist[] artists;
    public string preview_url;
}

[System.Serializable]
public class SpotifyArtist
{
    public string name;
}

public class SpotifyController : MonoBehaviour
{
    public SpotifyView view;
    [Min(1)]
    public int numberSongs;

    [SerializeField] private CountryInfo preparedCountry;
    [SerializeField] private bool isPreparing;
    [SerializeField] private PlaylistDetails? preparedData;

    public void PrepareCountry(CountryInfo country)
    {
        if (this.preparedCountry.Equals(country)) return;
        this.preparedCountry = country;
        this.isPreparing = true;
        StartCoroutine(PrepareCountryCoroutine(country));
    }

    public void SetCountry(CountryInfo country)
    {
        if (!country.Equals(preparedCountry)) PrepareCountry(country);
        view.SetLoading(true);
        StartCoroutine(SetCountryCoroutine(country));
    }

    private IEnumerator PrepareCountryCoroutine(CountryInfo country)
    {
        preparedData = null;
        
        // Fetch playlist and all track details from the new API endpoint
        var playlistDataTask = new TaskCompletionSource<(string, SongDetails[])>();
        yield return StartCoroutine(FetchPlaylistWithTracksCoroutine(country.p_id, result => playlistDataTask.SetResult(result)));

        var (playlistName, allSongs) = playlistDataTask.Task.Result;

        // Filter songs to only include those with valid preview URLs and limit to numberSongs
        List<SongDetails> songDetails = new List<SongDetails>();
        foreach (var song in allSongs)
        {
            // Check if country changed during async operation
            if (!this.preparedCountry.Equals(country)) yield break;
            
            // Stop if we've already collected enough songs (matching original behavior)
            if (songDetails.Count > numberSongs) break;
            
            if (Uri.IsWellFormedUriString(song.previewUrl, UriKind.Absolute))
            {
                songDetails.Add(song);
            }
        }

        preparedData = new PlaylistDetails(playlistName, songDetails.ToArray());
        isPreparing = false;
    }

    private IEnumerator SetCountryCoroutine(CountryInfo country)
    {
        while (isPreparing && country.Equals(preparedCountry))
        {
            yield return new WaitForSeconds(0.1f);
        }
        if (country.Equals(preparedCountry))
        {
            if (preparedData == null) throw new Exception("Tried to set playlist data but prepared data is null");
            view.SetPlaylist((PlaylistDetails)preparedData);
        }
    }

    private IEnumerator FetchPlaylistDetailsCoroutine(string id, System.Action<(string, string[])> callback)
    {
        string url = $"https://open.spotify.com/playlist/{id}";

        var fetchStringTask = new TaskCompletionSource<string>();
        yield return StartCoroutine(FetchStringCoroutine(url, result => fetchStringTask.SetResult(result)));

        string raw = fetchStringTask.Task.Result;

        Regex nameRg = new Regex("<meta property=\"og:title\" content=\"(?<name>[^\"]+)\"\\/>");
        string name = WebUtility.HtmlDecode(nameRg.Match(raw).Groups["name"].Value);

        Regex songRg = new Regex("<meta name=\"music:song\" content=\"(?<url>[^\"]+)\"\\/>");
        MatchCollection songMatches = songRg.Matches(raw);
        string[] songUrls = songMatches.Cast<Match>().Select(match => match.Groups["url"].Value).ToArray();

        callback((name, songUrls));
    }

    private IEnumerator FetchPlaylistWithTracksCoroutine(string playlistId, System.Action<(string, SongDetails[])> callback)
    {
        // Fetch from the new API endpoint that returns playlist with all track details
        string apiUrl = $"https://with.koeni.dev/spotify/tracks/{playlistId}";
        
        using (UnityWebRequest request = UnityWebRequest.Get(apiUrl))
        {
            request.SetRequestHeader("User-Agent", "culture/on/air");
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.LogError($"Connection error fetching playlist: {request.error}");
                callback(("", new SongDetails[0]));
                yield break;
            }
            else if (request.result == UnityWebRequest.Result.ProtocolError)
            {
                long responseCode = request.responseCode;
                if (responseCode == 404)
                {
                    Debug.LogWarning($"Playlist not found (404): {playlistId}");
                    callback(("", new SongDetails[0]));
                    yield break;
                }
                else if (responseCode == 500)
                {
                    Debug.LogError($"Server error (500) fetching playlist: {playlistId}");
                    callback(("", new SongDetails[0]));
                    yield break;
                }
                else
                {
                    Debug.LogError($"Protocol error fetching playlist: {request.error} (HTTP {responseCode})");
                    callback(("", new SongDetails[0]));
                    yield break;
                }
            }
            else
            {
                // Parse JSON response
                try
                {
                    string jsonResponse = request.downloadHandler.text;
                    SpotifyPlaylistResponse playlistResponse = JsonMapper.ToObject<SpotifyPlaylistResponse>(jsonResponse);
                    
                    string playlistName = playlistResponse.name ?? "";
                    List<SongDetails> songs = new List<SongDetails>();
                    
                    if (playlistResponse.tracks != null && playlistResponse.tracks.items != null)
                    {
                        foreach (var item in playlistResponse.tracks.items)
                        {
                            if (item?.track != null)
                            {
                                string name = item.track.name ?? "";
                                string artist = item.track.artists != null && item.track.artists.Length > 0 
                                    ? item.track.artists[0].name 
                                    : "";
                                string previewUrl = item.track.preview_url ?? "";
                                
                                songs.Add(new SongDetails(name, artist, previewUrl));
                            }
                        }
                    }
                    
                    callback((playlistName, songs.ToArray()));
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error parsing playlist JSON: {e.Message}");
                    callback(("", new SongDetails[0]));
                    yield break;
                }
            }
        }
    }

    private IEnumerator FetchSongDetailsCoroutine(string url, System.Action<SongDetails> callback)
    {
        var fetchStringTask = new TaskCompletionSource<string>();
        yield return StartCoroutine(FetchStringCoroutine(url, result => fetchStringTask.SetResult(result)));

        string raw = fetchStringTask.Task.Result;

        Regex nameRg = new Regex("<meta property=\"og:title\" content=\"(?<name>[^\"]+)\"\\/>");
        string name = WebUtility.HtmlDecode(nameRg.Match(raw).Groups["name"].Value);
        Regex artistRg = new Regex("<meta name=\"music:musician_description\" content=\"(?<artist>[^\"]+)\"\\/>");
        string artist = WebUtility.HtmlDecode(artistRg.Match(raw).Groups["artist"].Value);
        Regex urlRg = new Regex("<meta property=\"og:audio\" content=\"(?<url>[^\"]+)\"\\/>");
        string previewUrl = urlRg.Match(raw).Groups["url"].Value;

        callback(new SongDetails(name, artist, previewUrl));
    }

    public IEnumerator FetchStringCoroutine(string url, Action<string> callback)
    {

        string proxiedUrl = $"https://with.koeni.dev/cors/{HttpUtility.UrlEncode(url)}";
        using (UnityWebRequest request = UnityWebRequest.Get(proxiedUrl))
        {
            request.SetRequestHeader("User-Agent", "culture/on/air");
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"Error fetching string: {request.error}");
                callback(null);
            }
            else
            {
                callback(request.downloadHandler.text);
            }
        }
    }

}

public struct PlaylistDetails
{
    public string name;
    public SongDetails[] songs;

    public PlaylistDetails(string name, SongDetails[] songs)
    {
        this.name = name;
        this.songs = songs;
    }
}
