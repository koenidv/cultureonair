using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Util
{
    public static float Mapf(float value, float minIn, float maxIn, float minOut, float maxOut)
    {
        return (value - minIn) / (maxIn - minIn) * (maxOut - minOut) + minOut;
    }
}
