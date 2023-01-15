using System;
using UnityEngine;

[Serializable]
public class CurveHeuristicEvaluator
{
    // ========== Inspector data ==========
    [SerializeField]
    [Tooltip("The stat which will be evaluated through Evaluate()")]
    public Stat evaluatedStat = null;

    [SerializeField]
    [Tooltip("The curve the stat will be plugged in, and from which a score will be computed")]
    public AnimationCurve curve = AnimationCurve.Linear(.0f, .0f, 1f, 1f);

    [SerializeField]
    [Tooltip("The weight to apply on the score evaluated")]
    [Range(0f, 1f)]
    public float weight = 1f;


    // ========== Methods ==========
    public float Evaluate()
    {
        // Curves may not be defined on [evaluatedStat.minValue, evaluatedStat.maxValue] by default
        // Map this stat's value to the curve's bounds
        float range          = curve.keys[curve.keys.Length - 1].time - curve.keys[0].time;
        float valueProjected = curve.keys[0].time + evaluatedStat.ValueTo01() * range;
        float score          = curve.Evaluate(valueProjected);

        return score * weight;
    }
}