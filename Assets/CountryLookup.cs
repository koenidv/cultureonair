using System.Data;
using UnityEditor.PackageManager.UI;
using UnityEngine;

public class CountryLookup : MonoBehaviour
{
    public ComputeShader lookupShader;
    public Texture2D countryIndices;
    public CountriesLoader countriesLoader;
    public SamplingStrategies samplingStrategy;

    private bool useGpuSampling;
    ComputeBuffer resultBuffer;
    int lastIndex;

    private void Start()
    {
        SetSamplingStrategy(samplingStrategy);
        if (useGpuSampling)
        {
            SetResultBuffer();
            lookupShader.SetTexture(0, "_CountryIndicesTex", countryIndices);
        }
    }

    private void SetSamplingStrategy(SamplingStrategies sampleStrategy)
    {
        if (sampleStrategy != SamplingStrategies.Auto)
        {
            useGpuSampling = sampleStrategy == SamplingStrategies.GPU;
            return;
        }
        useGpuSampling = SystemInfo.supportsComputeShaders;
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
        if (useGpuSampling) return LookupIndexGPU(coordinate);
        else return LookupIndexCPU(coordinate);
    }

    private int LookupIndexCPU(Coordinate coordinate)
    {
        print("using cpu lookup");
        Vector2 uv = coordinate.ToUV();
        Color sample = countryIndices.GetPixel(Mathf.RoundToInt(uv.x * countryIndices.width), Mathf.RoundToInt(uv.y * countryIndices.height));
        return Mathf.RoundToInt(sample.r * 255);
    }

    private int LookupIndexGPU(Coordinate coordinate)
    {
        print("using graphic lookup");
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

public enum SamplingStrategies
{
    Auto,
    GPU,
    CPU
}
