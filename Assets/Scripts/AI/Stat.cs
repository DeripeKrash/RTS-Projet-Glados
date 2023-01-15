using System;
using UnityEngine;

[Serializable]
public class Stat : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Maximum value this statistic can get")]
    public float maxValue = 100f;

    [SerializeField]
    [Tooltip("Minimum value this statistic can get")]
    public float minValue = 0f;

    [field: SerializeField]
    [field: Tooltip("Current value of this statistic")]
    private float value = 100f;


    public void SetValue(float newValue)
    {
        value = Mathf.Clamp(newValue, minValue, maxValue);
    }

    public float GetValue()
    {
        return value;
    }

    // 0 means the value is min, 1 means the value is max
    public float ValueTo01()
    {
        return (value - minValue) / (maxValue - minValue);
    }
}