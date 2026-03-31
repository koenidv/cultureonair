using System;
using UnityEngine;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using UnityEngine.Networking;
using System.Web;
using LitJson;

public class DeezerController : MonoBehaviour
{
    public DeezerView view;
    [Min(1)]
    public int numberSongs = 20;

    [SerializeField] private CountryInfo preparedCountry;
    [SerializeField] private bool isPreparing;
    // Removed nullable ? as it might cause issues depending on C# version/settings in Unity
    [SerializeField] private PlaylistDetails preparedData;
    [SerializeField] private bool hasPreparedData;

    private const string WORLDWIDE_PLAYLIST_ID = "3155776842";
    private Dictionary<string, string> countryIdMap = new Dictionary<string, string>
    {
        { "USA", "1313621735" },
        { "GBR", "1116189381" },
        { "DEU", "1116190041" },
        { "FRA", "1109890291" },
        { "BRA", "1116188581" },
        { "MEX", "1116189901" },
        { "CAN", "1116189141" },
        { "IRL", "1116189601" },
        { "NLD", "1116189111" },
        { "ESP", "1116190041" }
    };

    public void PrepareCountry(CountryInfo country)
    {
        if (this.preparedCountry.Equals(country) && (hasPreparedData || isPreparing)) return;
        this.preparedCountry = country;
        this.isPreparing = true;
        this.hasPreparedData = false;
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
        hasPreparedData = false;
        
        string playlistId = null;
        
        // 1. Check hardcoded map
        if (countryIdMap.ContainsKey(country.c_id))
        {
            playlistId = countryIdMap[country.c_id];
        }
        
        // 2. Check if p_id is a Deezer ID (numeric)
        if (string.IsNullOrEmpty(playlistId) && !string.IsNullOrEmpty(country.p_id) && long.TryParse(country.p_id, out _))
        {
            playlistId = country.p_id;
        }

        // 3. Search for playlist if not found
        if (string.IsNullOrEmpty(playlistId))
        {
            var searchTask = new TaskCompletionSource<string>();
            yield return StartCoroutine(SearchPlaylistCoroutine(country.c_name, result => searchTask.SetResult(result)));
            playlistId = searchTask.Task.Result;
        }

        // 4. Fallback to Worldwide
        if (string.IsNullOrEmpty(playlistId))
        {
            playlistId = WORLDWIDE_PLAYLIST_ID;
        }

        // Fetch tracks
        var fetchTask = new TaskCompletionSource<object>();
        yield return StartCoroutine(FetchPlaylistTracksCoroutine(playlistId, result => fetchTask.SetResult(result)));
        
        if (!this.preparedCountry.Equals(country)) yield break;

        var resultObj = fetchTask.Task.Result;
        if (resultObj != null)
        {
            preparedData = (PlaylistDetails)resultObj;
            hasPreparedData = true;
        }
        isPreparing = false;
    }

    private IEnumerator SearchPlaylistCoroutine(string countryName, Action<string> callback)
    {
        string query = $"Top 100 {countryName}";
        string url = $"https://api.deezer.com/search/playlist?q={Uri.EscapeDataString(query)}";

        var fetchTask = new TaskCompletionSource<string>();
        yield return StartCoroutine(FetchStringCoroutine(url, result => fetchTask.SetResult(result)));
        
        string json = fetchTask.Task.Result;
        if (string.IsNullOrEmpty(json))
        {
            callback(null);
            yield break;
        }

        try {
            JsonData data = JsonMapper.ToObject(json);
            if (data["data"].IsArray && data["data"].Count > 0)
            {
                string foundId = null;
                for (int i = 0; i < data["data"].Count; i++)
                {
                    string title = data["data"][i]["title"].ToString().ToLower();
                    if (title.Contains("top") && title.Contains(countryName.ToLower()))
                    {
                        foundId = data["data"][i]["id"].ToString();
                        break;
                    }
                }
                callback(foundId ?? data["data"][0]["id"].ToString());
            }
            else
            {
                callback(null);
            }
        } catch (Exception e) {
            Debug.LogError($"Error parsing search results: {e.Message}");
            callback(null);
        }
    }

    private IEnumerator FetchPlaylistTracksCoroutine(string playlistId, Action<object> callback)
    {
        string url = $"https://api.deezer.com/playlist/{playlistId}";

        var fetchTask = new TaskCompletionSource<string>();
        yield return StartCoroutine(FetchStringCoroutine(url, result => fetchTask.SetResult(result)));
        
        string json = fetchTask.Task.Result;
        if (string.IsNullOrEmpty(json))
        {
            callback(null);
            yield break;
        }

        string playlistName = "";
        List<SongDetails> candidateSongs = new List<SongDetails>();
        bool parseSuccess = false;

        try {
            JsonData data = JsonMapper.ToObject(json);
            
            // Check for Deezer API error
            IDictionary dict = data as IDictionary;
            if (dict != null && dict.Contains("error"))
            {
                string errorMsg = data["error"]["message"].ToString();
                Debug.LogError($"Deezer API Error for playlist {playlistId}: {errorMsg}");
                callback(null);
                yield break;
            }

            if (dict == null || !dict.Contains("title"))
            {
                Debug.LogError($"Deezer API response for playlist {playlistId} missing title.");
                callback(null);
                yield break;
            }

            playlistName = data["title"].ToString();
            
            if (!dict.Contains("tracks") || !(data["tracks"] as IDictionary).Contains("data"))
            {
                callback(new PlaylistDetails(playlistName, new SongDetails[0]));
                yield break;
            }

            JsonData tracks = data["tracks"]["data"];
            int totalTracks = tracks.Count;
            
            // Extract metadata for up to 40 tracks to check later
            int maxToExtract = Math.Min(totalTracks, 40);
            for (int i = 0; i < maxToExtract; i++)
            {
                JsonData track = tracks[i];
                string name = track["title"].ToString();
                string artist = track["artist"]["name"].ToString();
                string previewUrl = track["preview"].ToString();
                if (!string.IsNullOrEmpty(previewUrl))
                {
                    candidateSongs.Add(new SongDetails(name, artist, previewUrl));
                }
            }
            parseSuccess = true;
        } catch (Exception e) {
            Debug.LogError($"Exception parsing playlist tracks: {e.Message}");
            callback(null);
            yield break;
        }

        if (!parseSuccess) yield break;

        // Perform validation outside of try-catch block
        List<SongDetails> validSongs = new List<SongDetails>();
        int candidateIndex = 0;
        // Increase buffer to 40 to provide fallback tracks if some fail during playback
        while (validSongs.Count < 40 && candidateIndex < candidateSongs.Count)
        {
            SongDetails song = candidateSongs[candidateIndex];
            bool isValid = false;
            yield return StartCoroutine(ValidatePreviewUrl(song.previewUrl, result => isValid = result));
            
            if (isValid)
            {
                validSongs.Add(song);
            }
            candidateIndex++;
        }
        
        callback(new PlaylistDetails(playlistName, validSongs.ToArray()));
    }

    private IEnumerator ValidatePreviewUrl(string url, Action<bool> callback)
    {
        using (UnityWebRequest request = UnityWebRequest.Head(url))
        {
            request.timeout = 2;
            yield return request.SendWebRequest();
            callback(request.result == UnityWebRequest.Result.Success);
        }
    }

    private IEnumerator SetCountryCoroutine(CountryInfo country)
    {
        float timeout = 10f;
        while (isPreparing && country.Equals(preparedCountry) && timeout > 0)
        {
            yield return new WaitForSeconds(0.1f);
            timeout -= 0.1f;
        }
        if (country.Equals(preparedCountry))
        {
            if (hasPreparedData)
            {
                view.SetPlaylist(preparedData, numberSongs);
            }
            else
            {
                Debug.LogWarning("Timeout or failed to prepare data for " + country.c_name);
                view.SetLoading(false);
            }
        }
    }

    public IEnumerator FetchStringCoroutine(string url, Action<string> callback)
    {
        string proxiedUrl = $"https://with.koeni.dev/cors/{Uri.EscapeDataString(url)}";
        using (UnityWebRequest request = UnityWebRequest.Get(proxiedUrl))
        {
            request.SetRequestHeader("User-Agent", "culture/on/air");
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                callback(null);
            }
            else
            {
                callback(request.downloadHandler.text);
            }
        }
    }
}

[Serializable]
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
