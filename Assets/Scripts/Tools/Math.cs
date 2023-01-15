using UnityEngine;

public static class Math
{
    static public Vector2 Flatten(Vector3 v)
    {
        return new Vector2(v.x, v.z);
    }

    public static Vector3 AddVec3Vec2(Vector3 a, Vector2 b)
    {
        return new Vector3(a.x + b.x, a.y, a.z + b.y);
    }

    static public float Sum(in float[] elem)
    {
        float sum = 0f;

        for (int i = 0; i < elem.Length; i++)
        {
            sum += elem[i];
        }

        return sum;
    }

    static public float Average(in float[] elem)
    {
        return Sum(elem) / elem.Length;
    }

    static public float Min(in float[] elem)
    {
        float min = float.MaxValue;

        for (int i = 0; i < elem.Length; i++)
        {
            min = Mathf.Min(min, elem[i]);
        }
        
        return min;
    }

    static public float Max(in float[] elem)
    {
        float max = float.MinValue;

        for (int i = 0; i < elem.Length; i++)
        {
            max = Mathf.Max(max, elem[i]);
        }
        
        return max;
    }
    
    static public float FlatDiagonalLength(Vector3 v)
    {
        return Mathf.Sqrt(v.x * v.x + v.z * v.z);
    }

    // See https://en.wikipedia.org/wiki/Box-Muller_transform
    static public Vector2 RandomNormalVec2()
    {
        float u = Random.value;
        float v = Random.value;

        float x  = Mathf.Log(u);
        float y  = Mathf.Log(u);
              x  = Mathf.Sqrt(-2f * x);
              y  = Mathf.Sqrt(-2f * y);
              x *= Mathf.Cos(2f * Mathf.PI * v);
              y *= Mathf.Sin(2f * Mathf.PI * v);

        return new Vector2(x, y);
    }
}