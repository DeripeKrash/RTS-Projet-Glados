using UnityEngine;
using System.Collections.ObjectModel;
using System.Collections.Generic;

public class UtilityInputs : MonoBehaviour
{
    // ========== Inspector data ==========
    [Header("Objects used to prepare inputs")]
    [SerializeField]
    AITraits aiTraits;

    [SerializeField]
    AIController aiController;

    [SerializeField]
    UnitController otherController;

    [SerializeField]
    Map.InfluenceMap influenceMap;
    
    [SerializeField]
    Map.FogOfWar fogOfWar;

    [SerializeField]
    Utility attackUtility;

    [SerializeField]
    Utility intelUtility;

    [SerializeField]
    Utility unitUtility;

    [SerializeField]
    Utility resourceUtility;

    [SerializeField]
    int minAmountOfUnits = 5;


    [Header("\"Attack\" utility inputs")]
    [SerializeField]
    Stat aggressiveTrait;

    [SerializeField]
    Stat enemyRecentOffensives;

    [Header("\"Acquire intel\" utility inputs")]
    [SerializeField]
    Stat visionCoveragePercent;

    [SerializeField]
    Stat visionOnEnemyRatio;

    [SerializeField]
    Stat curiousTrait;

    [Header("\"Acquire units\" utility inputs")]
    [SerializeField]
    Stat unitDeficit;

    [SerializeField]
    Stat influenceMapHotspots;

    [SerializeField]
    [Tooltip("Assigned directly by the utility itself")]
    Stat acquireIntelUtilityScore;
    
    [Header("\"Acquire resources\" utility inputs")]
    [SerializeField]
    Stat resourcesOwned;

    [SerializeField]
    Stat cheapTrait;

    [SerializeField]
    [Tooltip("Assigned directly by the utility itself")]
    Stat acquiredUnitsUtilityScore;

    [Header("Shared")]
    [SerializeField]
    [Tooltip("Assigned directly by the utility itself")]
    Stat attackUtilityScore;

    [SerializeField]
    Stat militaryAdvantageRatio;


    // ========== Internal data ==========
    int offensivesSinceLastUpdate = 0;


    // ========== MonoBehaviour methods ==========
    void Start()
    {
        // Set callback so offensivesSinceLastUpdate is incremented when an allied unit is taking damage
        aiController.OnEntityAdded += (BaseEntity entity) => { SetDamagedCallback(entity); };

        // TODO: remove if aiTraits changes over time
        aggressiveTrait.SetValue(aiTraits.aggressive);
        curiousTrait.SetValue(aiTraits.curious);

        UpdateInputs();
    }

    
    // ========== Other methods ==========
    void SetDamagedCallback(BaseEntity entity)
    {
        entity.OnEntityDamaged += () => { offensivesSinceLastUpdate++; };
    }


    // Intermediate utility methods
    float EvaluateUnitsMilitaryForce(ReadOnlyCollection<Unit> units)
    {
        // HP: the lower it is, the least damage it is likely to deal
        // DPS: the most significant representation of offensive capabilities
        // Cost: other perks of the unit (attack range, capture distance, speed...) are represented in its cost
        float total = 0f;

        foreach (Unit unit in units)
        {
            total += unit.GetHP() * (unit.GetUnitData.DPS + unit.GetUnitData.Cost);
        }
        return total;
    }

    float EvaluateFactoriesMilitaryForce(ReadOnlyCollection<Factory> factories)
    {
        // HP: the lower it is, the less likely it is to deal damage
        // AvailableUnits: strategic diversity
        int total = 0;

        foreach (Factory factory in factories)
        {
            total += factory.GetHP() * factory.GetFactoryData.AvailableUnits.Length;
        }

        return (float)total;
    }

    // Input update methods
    public void UpdateInputs()
    {
        UpdateMilitaryAdvantageRatio();
        UpdateEnemyRecentOffensives();
        UpdateUnitDeficit();
        UpdateInfluenceMapHotspots();

        resourcesOwned.SetValue(aiController.TotalBuildPoints);

        // TODO: uncomment if aiTraits changes over time
        // aggressiveTrait.SetValue(aiTraits.aggressive);
        // curiousTrait.SetValue(aiTraits.curious);
        // cheapTrait.SetValue(aiTraits.cheap);

        UpdateVisionCoverageRatio_VisionOnEnemy();

    }

    void UpdateMilitaryAdvantageRatio()
    {
        ReadOnlyCollection<Unit>    units          = aiController.GetReadOnlyUnits();
        ReadOnlyCollection<Factory> factories      = aiController.GetReadOnlyFactories();
        float                       unitsForce     = EvaluateUnitsMilitaryForce(units);
        float                       factoriesForce = EvaluateFactoriesMilitaryForce(factories);

        float aiMilitaryForce = unitsForce + 1f;

        units          = otherController.GetReadOnlyUnits();
        factories      = otherController.GetReadOnlyFactories();
        unitsForce     = EvaluateUnitsMilitaryForce(units);
        factoriesForce = EvaluateFactoriesMilitaryForce(factories);

        float otherMilitaryForce = unitsForce + 1f;
        
        militaryAdvantageRatio.SetValue(aiMilitaryForce / otherMilitaryForce);
    }

    void UpdateEnemyRecentOffensives()
    {
        enemyRecentOffensives.SetValue(offensivesSinceLastUpdate);

        offensivesSinceLastUpdate = 0;
    }

    void UpdateVisionCoverageRatio_VisionOnEnemy()
    {
        float visionCoverageRatio;
        int aiTeamIdx           = (int)aiController.GetTeam();
        int enemiesInSightCount = 0;

        Map.FogOfWar.FogClearData[] data = fogOfWar.ProcessFogForTeam(aiTeamIdx, out visionCoverageRatio);

        if (data == null)
        {
            return;
        }
        
        for (int i = 0; i < data.Length; i++)
        {
            bool isEnemy = (aiTeamIdx != data[i].teamIndex);
            
            if (isEnemy && (data[i].isVisible > 0))
            {
                enemiesInSightCount++;
            }
        }
        
        int factoCount = aiController.GetFactoryCount();
        int unitCount  = aiController.GetUnitCount();
        
        float enemiesInSightRatio = (float)enemiesInSightCount / (unitCount + factoCount);

        visionOnEnemyRatio.SetValue(enemiesInSightRatio);
        visionCoveragePercent.SetValue(visionCoverageRatio);
    }

    void UpdateUnitDeficit()
    {
        float ratio = minAmountOfUnits / (float)aiController.GetUnitCount();

        unitDeficit.SetValue(ratio);
    }

    void UpdateInfluenceMapHotspots()
    {
        int opponent = (int)GameServices.GetOpponent(aiController.GetTeam());

        float alliesMaxInfluence                = influenceMap.GetTeamMaxInfluence(opponent);
        List<Map.InfluenceMap.MapTile> hotspots = influenceMap.GetTileWithThreshold(alliesMaxInfluence, opponent);

        influenceMapHotspots.SetValue(hotspots.Count);
    }
}