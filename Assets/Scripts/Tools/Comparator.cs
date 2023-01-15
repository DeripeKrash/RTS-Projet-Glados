using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Comparator
{
    public enum Comparison
    {
        AlwaysSignificant = 0,
        NeverSignificant,
        Less,
        LessOrEqual,
        Greater,
        GreaterOrEqual,
        Equal,
        NotEqual
    }

    public delegate bool ComparisonFunc(float a, float b);
    ComparisonFunc[] compare =
    {
        (float a, float b) => { return true;   },
        (float a, float b) => { return false;  },
        (float a, float b) => { return a <  b; },
        (float a, float b) => { return a <= b; },
        (float a, float b) => { return a >  b; },
        (float a, float b) => { return a >= b; },
        (float a, float b) => { return a == b; },
        (float a, float b) => { return a != b; }
    };

    [SerializeField]
    [Tooltip("The evaluation operator used to evaluate this predicate")]
    Comparison compares = Comparison.AlwaysSignificant;

    [SerializeField]
    [Tooltip("The value the passed parameter is compared to")]
    float to;

    public bool Evaluate(float value)
    {
        return compare[(int)compares](value, to);
    }
}