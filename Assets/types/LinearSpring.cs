using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LinearSpring
{

    private float _value = 0;
    public float Value { get { return _value; } }

    private float _maxValue;
    public float MaxValue { get { return _maxValue; } }

    private float _minValue;
    public float MinValue { get { return _minValue; } }


    public LinearSpring(float maxValue)
    {
        this._maxValue = maxValue;
        this._minValue = -maxValue;
    }

    public void Add(float value)
    {
        _value += value * CalculateSpringDecreaseForce();
    }

    public void Set(float value) { _value = value; }

    public void Decrease()
    {
        _value *= CalculateSpringDecreaseForce() * Time.deltaTime;
    }

    private float CalculateSpringDecreaseForce()
    {
        return Util.MapfClamped(_value, _minValue, _maxValue, 1, 0);
    }
}
