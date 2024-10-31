using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Util
{
    public static float MapfClamped(float value, float minIn, float maxIn, float minOut, float maxOut)
    {
        value = Mathf.Clamp(value, minIn, maxIn);
        return (value - minIn) / (maxIn - minIn) * (maxOut - minOut) + minOut;
    }
}
