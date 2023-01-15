using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Map;
using UnityEngine;

// points system for units creation (Ex : light units = 1 pt, medium = 2pts, heavy = 3 pts)
// max points can be increased by capturing TargetBuilding entities
[RequireComponent(typeof(SquadController))]
public class UnitController : MonoBehaviour
{
    [SerializeField]
    protected Map.FogOfWar fogOfWar;

    [SerializeField]
    protected Map.InfluenceMap influenceMap;
    
    [SerializeField]
    protected ETeam Team;
    
    public ETeam GetTeam() { return Team; }

    [SerializeField]
    protected int StartingBuildPoints = 15;

    protected int _TotalBuildPoints = 0;
    public int TotalBuildPoints
    {
        get { return _TotalBuildPoints; }
        set
        {
            Debug.Log("TotalBuildPoints updated");
            _TotalBuildPoints = value;
            OnBuildPointsUpdated?.Invoke();
        }
    }

    protected int _CapturedTargets = 0;
    public int CapturedTargets
    {
        get { return _CapturedTargets; }
        set
        {
            _CapturedTargets = value;
            OnCaptureTarget?.Invoke();
        }
    }

    protected Transform TeamRoot = null;
    public Transform GetTeamRoot() { return TeamRoot; }

    public SquadController squadController;
    protected List<Unit> UnitList = new List<Unit>();
    protected List<Factory> FactoryList = new List<Factory>();
    public Factory SelectedFactory = null;

    public FogOfWar FogOfWar => fogOfWar;

    // events
    public    Action<BaseEntity> OnEntityAdded;
    protected Action             OnBuildPointsUpdated;
    protected Action             OnCaptureTarget;

    #region Unit methods
    public void UnselectAllUnits()
    {
        foreach (Unit unit in squadController.selectedUnits)
            unit.SetSelected(false);
        squadController.selectedUnits.Clear();
    }

    public void SelectAllUnits()
    {
        foreach (Unit unit in UnitList)
            unit.SetSelected(true);

        squadController.selectedUnits.Clear();
        squadController.selectedUnits.AddRange(UnitList);
    }

    public void SelectIdleUnits()
    {
        int unitCount = UnitList.Count;
        List<Unit> idleUnits = new List<Unit>(unitCount);

        for (int i = 0; i < unitCount; i++)
        {
            if (UnitList[i].GetState() == UnitState.Idle)
            {
                idleUnits.Add(UnitList[i]);
            }
        }

        SelectUnitList(idleUnits);
    }

    public void SelectAllUnitsByTypeId(int typeId)
    {
        UnselectCurrentFactory();
        UnselectAllUnits();
        
        squadController.selectedUnits = UnitList.FindAll(delegate (Unit unit)
            {
                return unit.GetTypeId == typeId;
            }
        );

        foreach (Unit unit in squadController.selectedUnits)
        {
            unit.SetSelected(true);
        }
    }

    public void SelectUnitList(List<Unit> units)
    {
        squadController.Add(units);
    }

    public void SelectUnitList(Unit [] units)
    {
        squadController.Add(units);
    }

    public void SelectUnit(Unit unit)
    {
       squadController.Add(unit);
    }

    public void UnselectUnit(Unit unit)
    {
       squadController.Remove(unit);
    }

    public void UnselectUnits(Unit[] units)
    {
        int len = units.Length;
        for (int i = 0; i < len; i++)
        {
            squadController.Remove(units[i]);
        }
    }

    public void FindUnitsAround(Vector3 pos, float radius, out Unit[] units)
    {
        int        sphereLayer = LayerMask.GetMask("Unit");
        Collider[] nearbyUnits = Physics.OverlapSphere(pos, radius, sphereLayer);

        int len = nearbyUnits.Length;
        units = new Unit[len];

        for (int i = 0; i < len; i++)
        {
            units[i] = nearbyUnits[i].gameObject.GetComponent<Unit>();
        }
    }

    public void SelectUnitsAround(Vector3 pos, float radius)
    {
        Unit[] units;
        FindUnitsAround(pos, radius, out units);

        SelectUnitList(units);
    }

    virtual public void AddUnit(Unit unit)
    {
        Map.FogElementBaseEntity fogElem    = unit.GetComponent<Map.FogElementBaseEntity>();
        InfluencerBaseEntity     influencerBaseEntity = unit.GetComponent<InfluencerBaseEntity>();

        unit.OnDeadEvent += () =>
        {
            TotalBuildPoints += unit.Cost;

            if (unit.IsSelected)
            {
                squadController.selectedUnits.Remove(unit);
            }

            fogOfWar.clearers.Remove(fogElem);
            influenceMap.RemoveInfluencer(influencerBaseEntity);
            UnitList.Remove(unit);
        };

        OnEntityAdded?.Invoke(unit);
        fogOfWar.clearers.Add(fogElem);
        influenceMap.AddInfluencer(influencerBaseEntity);
        UnitList.Add(unit);
    }

    void DestroySelected()
    {
        foreach (Unit unit in squadController.selectedUnits)
        {
            unit.Destroy();
        }

        if (SelectedFactory != null)
        {
            SelectedFactory.SetSelected(false);
            SelectedFactory.Destroy();
            SelectedFactory = null;
        }
    }

    public void TargetCaptured(int points)
    {
        Debug.Log("CaptureTarget");
        TotalBuildPoints += points;
        CapturedTargets++;
    }

    public void TargetLost(int points)
    {
        TotalBuildPoints -= points;
        CapturedTargets--;
    }
    #endregion

    #region Factory methods
    void AddFactory(Factory factory)
    {
        Map.FogElementBaseEntity fogElem    = factory.GetComponent<Map.FogElementBaseEntity>();
        InfluencerBaseEntity     influencerBaseEntity = factory.GetComponent<InfluencerBaseEntity>();

        if (factory == null)
        {
            Debug.LogWarning("Trying to add null factory");
            return;
        }

        factory.OnDeadEvent += () =>
        {
            TotalBuildPoints += factory.Cost;
            if (factory.IsSelected)
                SelectedFactory = null;
            FactoryList.Remove(factory);

            fogOfWar.clearers.Remove(fogElem);
            influenceMap.RemoveInfluencer(influencerBaseEntity);
        };

        OnEntityAdded?.Invoke(factory);
        fogOfWar.clearers.Add(fogElem);
        influenceMap.AddInfluencer(influencerBaseEntity);
        FactoryList.Add(factory);
    }

    virtual protected void SelectFactory(Factory factory)
    {
        if (factory == null || factory.IsUnderConstruction)
            return;

        SelectedFactory = factory;
        SelectedFactory.SetSelected(true);
        UnselectAllUnits();
    }

    public virtual void UnselectCurrentFactory()
    {
        if (SelectedFactory != null)
            SelectedFactory.SetSelected(false);
        SelectedFactory = null;
    }

    protected bool RequestUnitBuild(int unitMenuIndex)
    {
        if (SelectedFactory == null)
            return false;

        return SelectedFactory.RequestUnitBuild(unitMenuIndex);
    }

    protected bool RequestFactoryBuild(int factoryIndex, Vector3 buildPos)
    {
        if (SelectedFactory == null)
            return false;

        int cost = SelectedFactory.GetFactoryCost(factoryIndex);
        if (TotalBuildPoints < cost)
            return false;

        // Check if positon is valid
        if (SelectedFactory.CanPositionFactory(factoryIndex, buildPos) == false)
            return false;

        Factory newFactory = SelectedFactory.StartBuildFactory(factoryIndex, buildPos);
        if (newFactory != null)
        {
            AddFactory(newFactory);
            TotalBuildPoints -= cost;

            return true;
        }
        return false;
    }
    #endregion


    // ========== Getters & setters ==========
    public int GetSelectedUnitCount()
    {
        return squadController.selectedUnits.Count;
    }

    public FormationType GetSquadFormation()
    {
        return squadController.formation.current;
    }

    public void SetSquadFormation(FormationType formation)
    {
        squadController.formation.current = formation;
    }

    public int GetUnitCount()
    {
        return UnitList.Count;
    }

    public int GetFactoryCount()
    {
        return FactoryList.Count;
    }

    public int GetEntityWithTypeIdCount(int typeId)
    {
        int entityCount = 0;

        int factoryCount = FactoryList.Count;

        for (int i = 0; i < factoryCount; i++)
        {
            if (FactoryList[i].GetFactoryData.TypeId == typeId)
            {
                entityCount++;
            }
        }

        return entityCount;
    }

    public int GetLightFactoryCount()
    {
        return GetEntityWithTypeIdCount(8);
    }

    public int GetHeavyFactoryCount()
    {
        return GetEntityWithTypeIdCount(9);
    }

    public ReadOnlyCollection<Unit> GetReadOnlyUnits()
    {
        return UnitList.AsReadOnly();
    }

    public ReadOnlyCollection<Factory> GetReadOnlyFactories()
    {
        return FactoryList.AsReadOnly();
    }

    public int GetFactoryCost(int factoryIndex)
    {
        return FactoryList[0].GetFactoryCost(factoryIndex);
    }

    public float EvaluateMilitaryForce()
    {
        int   unitCount     = UnitList.Count;
        float militaryForce = 0f;

        for (int i = 0; i < unitCount; i++)
        {
            // NOTE: change as you need, this is just a placeholder to compute
            //       a single unit military power
            int   hp   = UnitList[i].GetHP();
            float dps  = UnitList[i].GetUnitData.DPS;
            float cost = UnitList[i].GetUnitData.Cost;

            militaryForce += hp * (dps + cost);
        }

        return militaryForce;
    }

    #region MonoBehaviour methods
    virtual protected void Awake()
    {
        string rootName = Team.ToString() + "Team";
        TeamRoot = GameObject.Find(rootName)?.transform;

        if (TeamRoot)
            Debug.LogFormat("TeamRoot {0} found !", rootName);

        squadController = GetComponent<SquadController>();
    }

    virtual protected void Start ()
    {
        CapturedTargets = 0;
        TotalBuildPoints = StartingBuildPoints;

        // get all team factory already in scene
        Factory [] allFactories = FindObjectsOfType<Factory>();
        foreach(Factory factory in allFactories)
        {
            if (factory.GetTeam() == GetTeam())
            {
                AddFactory(factory);
            }
        }

        Debug.Log("found " + FactoryList.Count + " factory for team " + GetTeam().ToString());
    }
    
    virtual protected void Update ()
    {
		
	}
    #endregion
}
