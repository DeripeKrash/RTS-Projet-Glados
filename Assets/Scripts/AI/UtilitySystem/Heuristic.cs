using System;
using UnityEngine;

[Serializable]
public class Heuristic
{
    // ========== Static ==========
    public enum ScoreCombination
    {
        SingleValue = 0,
        Average,
        Sum,
        Minimum,
        Maximum
    }

    delegate float CombinationFunc(in float[] scores);
    static CombinationFunc[] combine =
    {
        (in float[] scores) => { return scores[0]; },
        Math.Average,
        Math.Sum,
        Math.Min,
        Math.Max
    };


    // ========== Inspector data ==========
    [SerializeField]
    [Tooltip("The single-stat evaluators from which scores are computed, before being combined")]
    CurveHeuristicEvaluator[] evaluators;

    [SerializeField]
    [Tooltip("The way scores are combined together to return a single score")]
    ScoreCombination combineMode;


    // ========== Methods ==========
    public float Evaluate()
    {
        float[] scores = new float[evaluators.Length];

        for (int i = 0; i < evaluators.Length; i++)
        {
            scores[i] = evaluators[i].Evaluate();
        }

        return combine[(int)combineMode](scores);
    }
}