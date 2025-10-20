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
public class SpotifyTrackResponse
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

    // Static regex for extracting track ID from Spotify URLs
    private static readonly Regex TrackIdRegex = new Regex(@"spotify\.com/track/([a-zA-Z0-9]+)", RegexOptions.Compiled);

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
        var playlistDetails = new TaskCompletionSource<(string, string[])>();
        yield return StartCoroutine(FetchPlaylistDetailsCoroutine(country.p_id, result => playlistDetails.SetResult(result)));

        var (playlistName, songUrls) = playlistDetails.Task.Result;

        List<SongDetails> songDetails = new List<SongDetails>();
        int urlIndex = 0;

        while (urlIndex++ < songUrls.Length && songDetails.Count <= numberSongs)
        {
            string url = songUrls[urlIndex];
            // as this is async, check if country changed
            if (!this.preparedCountry.Equals(country)) yield break;

            var songDetailsTask = new TaskCompletionSource<SongDetails>();
            yield return StartCoroutine(FetchSongDetailsCoroutine(url, result => songDetailsTask.SetResult(result)));
            if (Uri.IsWellFormedUriString(songDetailsTask.Task.Result.previewUrl, UriKind.Absolute))
            {
                songDetails.Add(songDetailsTask.Task.Result);
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

    private IEnumerator FetchSongDetailsCoroutine(string url, System.Action<SongDetails> callback)
    {
        // Extract track ID from Spotify URL
        // URL format: https://open.spotify.com/track/{trackId} or similar
        string trackId = null;
        Match trackIdMatch = TrackIdRegex.Match(url);
        if (trackIdMatch.Success)
        {
            trackId = trackIdMatch.Groups[1].Value;
        }
        else
        {
            Debug.LogError($"Could not extract track ID from URL: {url}");
            callback(new SongDetails("", "", ""));
            yield break;
        }

        // Fetch from the new API endpoint
        string apiUrl = $"https://with.koeni.dev/spotify/tracks/{trackId}";
        
        using (UnityWebRequest request = UnityWebRequest.Get(apiUrl))
        {
            request.SetRequestHeader("User-Agent", "culture/on/air");
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.LogError($"Connection error fetching track details: {request.error}");
                callback(new SongDetails("", "", ""));
                yield break;
            }
            else if (request.result == UnityWebRequest.Result.ProtocolError)
            {
                long responseCode = request.responseCode;
                if (responseCode == 404)
                {
                    Debug.LogWarning($"Track not found (404): {trackId}");
                    callback(new SongDetails("", "", ""));
                }
                else if (responseCode == 500)
                {
                    Debug.LogError($"Server error (500) fetching track details: {trackId}");
                    callback(new SongDetails("", "", ""));
                }
                else
                {
                    Debug.LogError($"Protocol error fetching track details: {request.error} (HTTP {responseCode})");
                    callback(new SongDetails("", "", ""));
                }
                yield break;
            }
            else
            {
                // Parse JSON response
                try
                {
                    string jsonResponse = request.downloadHandler.text;
                    SpotifyTrackResponse trackResponse = JsonMapper.ToObject<SpotifyTrackResponse>(jsonResponse);
                    
                    string name = trackResponse.name ?? "";
                    string artist = trackResponse.artists != null && trackResponse.artists.Length > 0 
                        ? trackResponse.artists[0].name 
                        : "";
                    string previewUrl = trackResponse.preview_url ?? "";
                    
                    callback(new SongDetails(name, artist, previewUrl));
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error parsing track details JSON: {e.Message}");
                    callback(new SongDetails("", "", ""));
                    yield break;
                }
            }
        }
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
