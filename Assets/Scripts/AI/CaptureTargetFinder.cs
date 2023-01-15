using System;
using UnityEngine;

public enum CapturePriority
{
    Closest = 0,
    ClosestNeutral,
    ClosestEnemy
}


// CaptureTargetFinder is in charge of finding a TargetBuilding to capture, according
// to the capture priority specified. It proceeds as follow:
// 1. Retrieve all neutral points in an array and their distance from a passed position
// 2. Same for enemy-owned points and append to the same array, and keep the index to the start
//    of enemy-owned points
// 3. Depending on the capture priority specified, either:
//    - find the index of the all-around closest capture target in the array, or
//    - find the index of the closest neutral capture target in the array, or
//    - find the index of the closest enemy-owned capture target in the array
// 4. With the index, return the matching target building
[Serializable]
public class CaptureTargetFinder
{
    // ========== Inspector fields ==========
    [SerializeField]
    public CapturePriority capturePriority = CapturePriority.Closest;


    // ========== Internal helper structures ==========
    delegate int FindCaptureTargetIdxFunc(BuildingsSqrDist pairs);
    FindCaptureTargetIdxFunc[] FindCaptureTargetIdxTable;

    struct BuildingsSqrDist
    {
        public float[] dist2ToSquad;
        public int     size;
        public int     enemyBuildingStartIdx;

        public BuildingsSqrDist(int capacity)
        {
            dist2ToSquad          = new float[capacity];
            size                  = 0;
            enemyBuildingStartIdx = 0;
        }

        public void Add(float dist2)
        {
            dist2ToSquad[size] = dist2;
            size++;
        }
    }


    // ========== Internal helper methods ==========
    static void GetTargetBuildingsDist(ref BuildingsSqrDist dists,
                                       TargetBuilding[]    targets,
                                       Vector3             referencePos,
                                       ETeam               targetBuildingOwner)
    {
        int targetLen = targets.Length;
        for (int i = 0; i < targetLen; i++)
        {
            if (targets[i].GetTeam() == targetBuildingOwner)
            {
                Vector2 toBuilding = new Vector2(targets[i].transform.position.x - referencePos.x,
                                                 targets[i].transform.position.z - referencePos.z);
                toBuilding.x = toBuilding.sqrMagnitude;

                dists.Add(toBuilding.x);
            }
        }
    }

    static BuildingsSqrDist GetNotAlliedTargetBuildingsDist(TargetBuilding[] targets,
                                                            Vector3          referencePos,
                                                            ETeam            callerTeam)
    {
        int              targetLen = targets.Length;
        BuildingsSqrDist dists     = new BuildingsSqrDist(targetLen);
        
        // Neutral first
        GetTargetBuildingsDist(ref dists, targets, referencePos, ETeam.Neutral);

        // Enemy second
        ETeam enemyTeam = GameServices.GetOpponent(callerTeam);
        dists.enemyBuildingStartIdx = dists.size;
        GetTargetBuildingsDist(ref dists, targets, referencePos, enemyTeam);

        return dists;
    }

    static int FindMinDistIdx(float[] dist2, int startInc, int endExc)
    {
        int minIdx = startInc;

        for (int i = minIdx + 1; i < endExc; i++)
        {
            if (dist2[i] < dist2[minIdx])
            {
                minIdx = i;
            }
        }

        return minIdx;
    }

    static int FindClosestCaptureTargetIdx(BuildingsSqrDist pairs)
    {
        return FindMinDistIdx(pairs.dist2ToSquad, 0, pairs.size);
    }

    static int FindClosestNeutralCaptureTargetIdx(BuildingsSqrDist pairs)
    {
        return FindMinDistIdx(pairs.dist2ToSquad, 0, pairs.enemyBuildingStartIdx);
    }

    static int FindClosestEnemyCaptureTargetIdx(BuildingsSqrDist pairs)
    {
        return FindMinDistIdx(pairs.dist2ToSquad, pairs.enemyBuildingStartIdx, pairs.size);
    }


    // ========== Public method ==========
    public void Initialize()
    {
        FindCaptureTargetIdxTable = new FindCaptureTargetIdxFunc[]
        {
            FindClosestCaptureTargetIdx,
            FindClosestNeutralCaptureTargetIdx,
            FindClosestEnemyCaptureTargetIdx
        };
    }

    public TargetBuilding FindCaptureTarget(Vector3 referencePos, ETeam callerTeam)
    {
        TargetBuilding[] targets = GameServices.GetTargetBuildings();
        BuildingsSqrDist dists   = GetNotAlliedTargetBuildingsDist(targets, referencePos, callerTeam);

        if (dists.size == 0)
        {
            return null;
        }

        int idx = FindCaptureTargetIdxTable[(int)capturePriority](dists);

        return targets[idx];
    }
}