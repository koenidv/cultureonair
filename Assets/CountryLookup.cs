using UnityEngine;

public class CountryLookup : MonoBehaviour
{
    public ComputeShader lookupShader;
    public Texture2D countryIndices;

    ComputeBuffer resultBuffer;

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

    public float LookupIndex(Coordinate coordinate)
    {
        SetResultBuffer(); // result buffer isn't restored after hot reload
        lookupShader.SetVector("uv", coordinate.ToUV());
        lookupShader.Dispatch(0, 1, 1, 1);
        float[] result = new float[1];
        resultBuffer.GetData(result);
        print(result[0] * 255);
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
