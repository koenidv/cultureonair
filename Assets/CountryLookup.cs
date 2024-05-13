using UnityEngine;

public class CountryLookup : MonoBehaviour
{
    public ComputeShader lookupShader;
    public Texture2D countryIndices;
    public CountriesLoader countriesLoader;

    ComputeBuffer resultBuffer;
    int lastIndex;

    private void Start()
    {
        SetResultBuffer();
        lookupShader.SetTexture(0, "_CountryIndicesTex", countryIndices);
    }

    private void SetResultBuffer()
    {
        if (resultBuffer != null) return;
        resultBuffer = new ComputeBuffer(1, sizeof(int));
        lookupShader.SetBuffer(0, "_Result", resultBuffer);
    }

    public string LookupCountryName(Coordinate coordinate)
    {
        int index = LookupIndex(coordinate) - 1;
        if (index < 0) return null;
        return countriesLoader.GetCountries()[index].c_name;
    }

    public int LookupIndex(Coordinate coordinate)
    {
        SetResultBuffer(); // result buffer isn't restored after hot reload
        lookupShader.SetVector("uv", coordinate.ToUV());
        lookupShader.Dispatch(0, 1, 1, 1);
        int[] result = new int[1];
        resultBuffer.GetData(result);
        if (result[0] == 0) return lastIndex;
        lastIndex = result[0];
        return result[0];
    }

    private void OnDestroy()
    {
        if (resultBuffer != null)
        {
            resultBuffer.Release();
            resultBuffer = null;
        }
    }
}
