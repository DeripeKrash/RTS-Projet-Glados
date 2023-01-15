using static Math;
using UnityEngine;
using System.Collections.Generic;
using System;
using Random = UnityEngine.Random;

[Serializable]
public enum FormationType
{
    Regiment = 0,
    Disc,
    Random
}


[Serializable]
public class FormationGenerator
{
    // ========== Inspector ==========
    [SerializeField]
    public FormationType current = FormationType.Regiment;

    [Header("Regiment parameters")]
    [SerializeField]
    public int unitsPerRegimentRow = 4;

    [Header("Disc parameters")]
    [SerializeField]
    public int maxAttemptsPerPoint = 60;

    [SerializeField]
    [Range(0, 1)]
    [Tooltip("The ratio of the search radius added as margin")]
    public float radiusMarginRatio = 0.75f;

    // ========== Internal ==========
    [HideInInspector]
    public List<Vector2> relPos = new List<Vector2>();
    
    delegate void GenerationFunc(List<Unit> units);
    GenerationFunc[] Generate;


    // ========== Private utility methods ==========
    static void GetUnitsMaxSideAndDiagonal(List<Unit> units, out float maxSide, out float maxDiag)
    {
        maxSide = 0f;
        maxDiag = 0f;

        foreach (Unit unit in units)
        {
            Collider unitCol = unit.GetComponent<Collider>();

            Vector2 maxB = Math.Flatten(unitCol.bounds.max);
            Vector2 minB = Math.Flatten(unitCol.bounds.min);
            Vector2 diff = maxB - minB;

            maxSide = Mathf.Max(maxSide, Mathf.Max(diff.x, diff.y));
            maxDiag = Mathf.Max(maxDiag, diff.magnitude);
        }
    }

    static float GetUnitsMaxSide(List<Unit> units)
    {
        float maxSide = 0f;
        foreach (Unit unit in units)
        {
            Collider unitCol = unit.GetComponent<Collider>();

            float diffX = unitCol.bounds.max.x - unitCol.bounds.min.x;
            float diffY = unitCol.bounds.max.z - unitCol.bounds.min.z;

            maxSide = Mathf.Max(maxSide, Mathf.Max(diffX, diffY));
        }

        return maxSide;
    }

    static float GetUnitsMaxDiagonal(List<Unit> units)
    {
        float maxDiag = 0f;
        foreach (Unit unit in units)
        {
            Collider unitCol = unit.GetComponent<Collider>();

            Vector2 maxB = Math.Flatten(unitCol.bounds.max);
            Vector2 minB = Math.Flatten(unitCol.bounds.min);

            maxDiag = Mathf.Max(maxDiag, Vector2.Distance(maxB, minB));
        }

        return maxDiag;
    }

    static float GetUnitsTotalSurface(List<Unit> units)
    {
        float surface = 0f;

        foreach (Unit unit in units)
        {
            Vector3 dim = unit.GetComponent<Collider>().bounds.size;
            
            surface += dim.x * dim.z;
        }

        return surface;
    }

    // Helper methods for disc formation
    static float EstimateRadiusForSurface(float surface)
    {
        return Mathf.Sqrt(surface / Mathf.PI);
    }


    float GetDist2ToClosestRelPos(Vector2 pos)
    {
        float min = float.MaxValue;

        int unitCount = relPos.Count;
        for (int i = 0; i < unitCount; i++)
        {
            Vector2 toPos = relPos[i] - pos;
            min = Mathf.Min(min, toPos.sqrMagnitude);
        }

        return min;
    }


    // ========== Public methods ==========
    public FormationGenerator()
    {
        Generate = new GenerationFunc[]
        {
            GenerateRegiment,
            GenerateDisc,
            GenerateRandom
        };
    }


    public void GenerateFor(List<Unit> units)
    {
        relPos.Clear();
        relPos.Capacity = units.Count;

        Generate[(int)current](units);
    }


    public void GenerateRegiment(List<Unit> units)
    {
        Vector2Int slotCoord        = Vector2Int.zero;
        int        unitCount        = units.Count;
        int        unitsPerRow      = Mathf.Min(unitCount, unitsPerRegimentRow);
        float      distBetweenSlots = GetUnitsMaxDiagonal(units);

        for (int i = 0; i < unitCount; i++)
        {
            slotCoord.x = (i % unitsPerRow) - (unitsPerRow / 2);
            slotCoord.y = i / unitsPerRow;

            Vector2 slotPos = new Vector2(slotCoord.x * distBetweenSlots,
                                          slotCoord.y * distBetweenSlots);
            
            relPos.Add(slotPos);
        }
    }


    public void GenerateDisc(List<Unit> units)
    {
        const float one_third = 0.3333333f;

        int unitCount = units.Count;

        float maxSide, maxDiag;
        GetUnitsMaxSideAndDiagonal(units, out maxSide, out maxDiag);

        // Pick a wide radius to accelerate how fast new positions are found
        // while keeping a minimal gap between positions
        float radius   = EstimateRadiusForSurface(maxDiag * maxDiag * unitCount);
              radius  += radiusMarginRatio * radius;
        float minDist2 = maxSide * maxSide;

        for (int unitIdx = 0; unitIdx < unitCount; unitIdx++)
        {
            for (int attempIdx = 0; attempIdx < maxAttemptsPerPoint; attempIdx++)
            {
                Vector2 newPos = Math.RandomNormalVec2();
                float   cbrt   = Mathf.Pow(Random.value, one_third);

                newPos = Vector2.ClampMagnitude(newPos, radius * cbrt);

                float dist2 = GetDist2ToClosestRelPos(newPos);
                if (dist2 >= minDist2)
                {
                    relPos.Add(newPos);
                    break;
                }
            }
        }

        // Compute missing positions using random
        int posFoundCount = relPos.Count;
        if (posFoundCount < unitCount)
        {
            int        diff  = unitCount - posFoundCount;
            List<Unit> range = units.GetRange(posFoundCount, diff);

            GenerateRandom(range);
        }
    }


    public void GenerateRandom(List<Unit> units)
    {
        int   unitCount      = units.Count;
        float maxDim         = GetUnitsMaxDiagonal(units);
        float offset         = maxDim / unitCount;
        float distFromCenter = offset;

        for (int i = 0; i < unitCount; i++)
        {
            relPos.Add(Random.insideUnitCircle * offset);
            distFromCenter += offset;
        }
    }
}

/* Poisson-disc sampling approach. Abandoned

// Helper structure for disc formation generation
public struct PoissonGrid
{
    public int[] cells;
    public int   side;
    public float cellSize;

    public PoissonGrid(int _side, float _cellSize)
    {
        int size = _side * _side;
        cells    = new int[size];
        side     = _side;
        cellSize = _cellSize;

        for (int i = 0; i < size; i++)
        {
            cells[i] = -1;
        }
    }
}

int GetPosGridIndex(PoissonGrid grid, Vector2 pos)
{
    int xIdx = Mathf.FloorToInt(pos.x / grid.cellSize);
    int yIdx = Mathf.FloorToInt(pos.y / grid.cellSize);
    
    return yIdx * grid.side + xIdx;
}

void RegisterDiscPos(PoissonGrid grid, Queue<int> activeList, Vector2 pos)
{
    int posGridIdx = GetPosGridIndex(grid, pos);
    int posIdx     = relPos.Count;

    Debug.Log("pos = " + pos.ToString());
    Debug.Log("posGridIdx = " + posGridIdx.ToString());
    Debug.Log("grid.cells.Length = " + grid.cells.Length.ToString());

    grid.cells[posGridIdx] = posIdx;
    activeList.Enqueue(posIdx);
    relPos.Add(pos);
}

static Vector2 RandOffset(float minDist)
{
    return Random.insideUnitCircle * Random.Range(minDist, minDist * 2f);
}

float SqrMagToNearbyPoint(PoissonGrid grid, Vector2 pos)
{
    int maxIdx     = grid.cells.Length - 1;
    int posGridIdx = GetPosGridIndex(grid, pos);
    int posIdx     = grid.cells[posGridIdx];

    // There is already a registered position near pos
    if (posIdx != -1)
    {
        return 0f;
    }

    float closest = float.MaxValue;

    int[] adjacentIndices =
    {
        Mathf.Max(posGridIdx - grid.side - 1, 0),
        Mathf.Max(posGridIdx - grid.side,     0),
        Mathf.Max(posGridIdx - grid.side + 1, 0),
        Mathf.Max(posGridIdx - 1,             0),
        Mathf.Min(posGridIdx + 1,             maxIdx),
        Mathf.Min(posGridIdx + grid.side - 1, maxIdx),
        Mathf.Min(posGridIdx + grid.side,     maxIdx),
        Mathf.Min(posGridIdx + grid.side + 1, maxIdx),
    };

    for (int i = 0; i < 8; i++)
    {
        int idx = grid.cells[adjacentIndices[i]];

        if (idx != -1)
        {
            closest = Mathf.Min(closest, (relPos[idx] - pos).sqrMagnitude);
        }
    }

    return closest;
}

public void GenerateDisc(List<Unit> units)
{
    // Get relevant data before doing anything
    int   unitCount   = units.Count;
    float maxDim      = GetUnitsMaxDimension(units);
    float discRadius  = EstimateRadiusForSurface(maxDim * maxDim * unitCount);
    float discRadius2 = discRadius * discRadius;

    // Initialize grid and active list
    // 0.7071067811865475244 is 1 / sqrt(2)
    float       cellSize          = maxDim * 0.7071067811865475244f;
    int         gridSideCellCount = Mathf.FloorToInt(2f * discRadius / cellSize);
    PoissonGrid grid              = new PoissonGrid(gridSideCellCount, cellSize);
    Queue<int>  activeList        = new Queue<int>(grid.cells.Length);

    Debug.DrawLine(units[0].transform.position - Vector3.right * grid.side * grid.cellSize,
                   units[0].transform.position + Vector3.right * grid.side * grid.cellSize,
                   Color.red,
                   60f,
                   true);
    Debug.DrawLine(units[0].transform.position - Vector3.forward * grid.side * grid.cellSize,
                   units[0].transform.position + Vector3.forward * grid.side * grid.cellSize,
                   Color.red,
                   60f,
                   true);

    relPos.Clear();
    relPos.Capacity = unitCount;

    // Pick first (random) position
    Vector2 gridCenter = new Vector2(grid.side * grid.cellSize * .5f,
                                     grid.side * grid.cellSize * .5f);
    Vector2 newPos     = gridCenter + RandOffset(maxDim);

    RegisterDiscPos(grid, activeList, newPos);

    while (relPos.Count < unitCount)
    {
        // Get a random position around the first active one
        int     posIdx = activeList.Peek();
        Vector2 center = relPos[posIdx];

        bool newPosFound = false;

        for (int i = 0; i < maxAttemptsPerPoint; i++)
        {
            newPos      = center + RandOffset(maxDim);
            newPosFound = (newPos - gridCenter).sqrMagnitude < discRadius2;
            newPosFound = newPosFound && (SqrMagToNearbyPoint(grid, newPos) >= maxDim);

            if (newPosFound)
            {
                break;
            }
        }

        if (newPosFound)
        {
            RegisterDiscPos(grid, activeList, newPos);
        }
        else
        {
            activeList.Dequeue();
        }
    }
}
*/