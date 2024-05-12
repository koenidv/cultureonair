using LitJson;
using UnityEngine;

[System.Serializable]
public class CountriesLoader : MonoBehaviour
{
    public TextAsset data;
    private CountryInfo[] _Countries;

    public void OnEnable()
    {
        _Countries = JsonMapper.ToObject<CountryInfo[]>(data.text);
    }

    public CountryInfo[] GetCountries()
    {
        return _Countries;
    }


}

public struct CountryInfo
{
    public string c_id;
    public string c_name;
    public string p_id;
    public string p_name;
}