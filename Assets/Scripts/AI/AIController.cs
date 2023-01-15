using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class AIController : UnitController
{
    [SerializeField] public FactoryLocationFinder factoryLocationFinder;

    [SerializeField] public CaptureTargetFinder captureTargetFinder;

    // Intermediate storage for AI actions
    [HideInInspector] public Vector3 factoryBuildLocation = new Vector3(float.NaN, float.NaN, float.NaN);

    #region MonoBehaviour methods

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Start()
    {
        base.Start();
        factoryLocationFinder.PreComputeValues();
        captureTargetFinder.Initialize();
    }

    #endregion

    public void SelectMilitaryForces(float forcesRequired)
    {
        int unitCount = GetUnitCount();
        int unitsPickedCount = 0;
        float forcesGathered = 0f;

        while ((unitsPickedCount < unitCount) && (forcesGathered < forcesRequired))
        {
            // NOTE: change as you need, this is just a placeholder to compute
            //       a single unit military power
            int hp = UnitList[unitsPickedCount].GetHP();
            float dps = UnitList[unitsPickedCount].GetUnitData.DPS;
            float cost = UnitList[unitsPickedCount].GetUnitData.Cost;

            forcesGathered += hp * (dps + cost);

            unitsPickedCount++;
        }

        List<Unit> range = UnitList.GetRange(0, unitsPickedCount);
        squadController.Add(range);
    }

    public int PickUnitType(int cost)
    {
        return SelectedFactory.GetMostExpensiveAffordableUnitIndex(cost);
    }

    public int PickFactoryType(int cost)
    {
        return SelectedFactory.GetMostExpensiveAffordableFactoryIndex(cost);
    }

    public Vector3 FindFactoryBuildLocation(int factoryIndex)
    {
        GameObject selectedFactory = SelectedFactory.GetFactoryGO(factoryIndex);

        return factoryLocationFinder.FindLocation(FactoryList.AsReadOnly(), selectedFactory);
    }

    public TargetBuilding GetBuildingToCapture()
    {
        Vector3 referencePos;

        if (squadController.selectedUnits.Count > 0)
        {
            Vector2 referencePosVec2 = squadController.GetAveragePosition();
            referencePos.x = referencePosVec2.x;
            referencePos.y = squadController.selectedUnits[0].transform.position.y;
            referencePos.z = referencePosVec2.y;
        }
        else
        {
            referencePos = FactoryList[0].transform.position;
        }

        return captureTargetFinder.FindCaptureTarget(referencePos, Team);
    }

    // The position returned is meant to be used for an offensive move
    public Vector3 GetTargetLocationToAttack()
    {
        if (GetSelectedUnitCount() == 0)
            return new Vector3(float.NaN, float.NaN, float.NaN);
        
        int opponent = (int) GameServices.GetOpponent(Team);

        float maxInfluence = influenceMap.GetTeamMaxInfluence(opponent);
        List<Map.InfluenceMap.MapTile> tiles = influenceMap.GetTileWithThreshold(maxInfluence,
            opponent);
        Vector3 squadAvgPos = squadController.GetAveragePosition();

        int tileCount = tiles.Count;
        int closestIdx = -1;
        float minDist2 = float.MaxValue;
        
        for (int i = 0; i < tileCount; i++)
        {
            Vector2 toTile = new Vector2(squadAvgPos.x - tiles[i].position.x,
                squadAvgPos.z - tiles[i].position.z);
            float dist2 = toTile.sqrMagnitude;

            if (dist2 < minDist2)
            {
                minDist2 = dist2;
                closestIdx = i;
            }
        }

        if (closestIdx == -1)
        {
            return new Vector3(float.NaN, float.NaN,float.NaN);
        }
        
        return tiles[closestIdx].position;
    }

    public void OffensivelyMoveTo(Vector3 dst)
    {
        UnselectUnits(squadController.selectedUnits.ToArray());
        SelectIdleUnits();
        squadController.OffensivelyMoveTo(dst);
    }

    public void EvaluateFight(Vector3 target, ref float enemyMilitaryPower, ref float ourMilitaryPower)
    {
        float newOurMilitaryPower = ourMilitaryPower - enemyMilitaryPower;
        float newEnemyMilitaryPower = enemyMilitaryPower - ourMilitaryPower;
        
        enemyMilitaryPower = newEnemyMilitaryPower;
        ourMilitaryPower = newOurMilitaryPower;
    }

    public void Capture(TargetBuilding building)
    {
        SelectIdleUnits();
        squadController.Capture(building);
    }

    public void BuildFactory(int value, Vector3 position)
    {
        SelectFactory(FactoryList[0]);

        bool success = RequestFactoryBuild(value, position);
        if (!success)
        {
            Debug.Log("Could not build factory");
        }
    }

    public Factory GetFirstFactoryOfType(int factoryType)
    {
        return FactoryList[factoryType];
    }
}