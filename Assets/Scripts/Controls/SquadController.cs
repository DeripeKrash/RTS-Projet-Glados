using static Math;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public enum OnAttackedSquadReaction
{
    NoReaction = 0,
    Attack,
    Flee,
    PerUnitDefault
}

[Serializable]
public class SquadController : MonoBehaviour
{
    // ========== Inspector ==========
    [SerializeField]
    public FormationGenerator formation = new FormationGenerator();

    [SerializeField]
    OnAttackedSquadReaction squadReactionOnAttacked = OnAttackedSquadReaction.PerUnitDefault;


    // ========== Internal ==========
    [HideInInspector]
    public List<Unit> selectedUnits = new List<Unit>();


    // ========== Public methods ==========
    // Helper methods
    public Vector2 GetAveragePosition()
    {
        Vector2 avg = Vector2.zero;

        foreach (Unit unit in selectedUnits)
        {
            avg.x += unit.transform.position.x;
            avg.y += unit.transform.position.z;
        }

        return avg / selectedUnits.Count;
    }

    public float GetLowestMovespeed()
    {
        float min = float.MaxValue;

        foreach (Unit unit in selectedUnits)
        {
            min = Mathf.Min(unit.GetUnitData.Speed, min);
        }

        return min;
    }

    public void SetControllerReactionOnAttacked(OnAttackedSquadReaction newReaction)
    {
        squadReactionOnAttacked = newReaction;

        SetUnitsReactionOnAttacked(newReaction);
    }

    public void SetUnitsReactionOnAttacked(OnAttackedSquadReaction newReaction)
    {
        if (newReaction != OnAttackedSquadReaction.PerUnitDefault)
        {
            foreach (Unit unit in selectedUnits)
            {
                unit.onAttackedIdleReaction = (OnAttackedUnitReaction)newReaction;
            }
        }
        else
        {
            foreach (Unit unit in selectedUnits)
            {
                unit.onAttackedIdleReaction = unit.GetUnitData.defaultReactionOnAttacked;
            }
        }
    }

    // Squad member handling
    public void Add(Unit unit)
    {
        if (squadReactionOnAttacked != OnAttackedSquadReaction.PerUnitDefault)
        {
            unit.onAttackedIdleReaction = (OnAttackedUnitReaction)squadReactionOnAttacked;
        }
        else
        {
            unit.onAttackedIdleReaction = unit.GetUnitData.defaultReactionOnAttacked;
        }

        unit.SetSelected(true);
        unit.OnDeadEvent += () => { Remove(unit); };
        selectedUnits.Add(unit);
    }

    public void Add(IEnumerable<Unit> units)
    {
        if (squadReactionOnAttacked != OnAttackedSquadReaction.PerUnitDefault)
        {
            foreach (Unit unit in units)
            {
                unit.SetSelected(true);
                unit.OnDeadEvent += () => { Remove(unit); };
                unit.onAttackedIdleReaction = (OnAttackedUnitReaction)squadReactionOnAttacked;
            }
        }
        else
        {
            foreach (Unit unit in units)
            {
                unit.SetSelected(true);
                unit.OnDeadEvent += () => { Remove(unit); };
                unit.onAttackedIdleReaction = unit.GetUnitData.defaultReactionOnAttacked;
            }
        }

        selectedUnits.AddRange(units);
    }

    public void Remove(Unit unit)
    {
        unit.SetSelected(false);
        unit.OnDeadEvent -= () => { Remove(unit); };
        selectedUnits.Remove(unit);
    }

    // Actions
    public void MoveTo(Vector3 dst)
    {
        int     unitCount = selectedUnits.Count;
        float   movespeed = GetLowestMovespeed();

        formation.GenerateFor(selectedUnits);

        for (int i = 0; i < unitCount; i++)
        {
            Vector3 newDst = AddVec3Vec2(dst, formation.relPos[i]);

            selectedUnits[i].SetMovespeed(movespeed);
            selectedUnits[i].StartMovingTo(newDst);
        }
    }

    public void OffensivelyMoveTo(Vector3 dst)
    {
        int     unitCount = selectedUnits.Count;
        float   movespeed = GetLowestMovespeed();

        formation.GenerateFor(selectedUnits);

        for (int i = 0; i < unitCount; i++)
        {
            Vector3 newDst = AddVec3Vec2(dst, formation.relPos[i]);

            selectedUnits[i].SetMovespeed(movespeed);
            selectedUnits[i].StartMovingOffensivelyTo(newDst);
        }
    }

    public void Attack(BaseEntity entity)
    {
        foreach (Unit unit in selectedUnits)
        {
            unit.StartAttacking(entity);
        }
    }

    public void Capture(TargetBuilding target)
    {
        foreach (Unit unit in selectedUnits)
        {
            unit.StartCapturing(target);
        }
    }

    public void Repair(BaseEntity entity)
    {
        foreach (Unit unit in selectedUnits)
        {
            unit.StartRepairing(entity);
        }
    }

    public void Stop()
    {
        foreach (Unit unit in selectedUnits)
        {
            unit.StopAllActions();
        }
    }
}