public struct SongDetails
{
    public string name;
    public string artist;
    public string previewUrl;

    public SongDetails(string name, string artist, string previewUrl)
    {
        this.name = name;
        this.artist = artist;
        this.previewUrl = previewUrl;
    }
}