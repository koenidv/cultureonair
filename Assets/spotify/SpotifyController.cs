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
