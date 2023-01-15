using UnityEngine;
using System;

[Serializable]
public enum OnAttackedUnitReaction
{
    NoReaction = 0,
    Attack,
    Flee
}

[CreateAssetMenu(fileName = "Unit_Data", menuName = "RTS/UnitData", order = 0)]
public class UnitDataScriptable : EntityDataScriptable
{
    [Header("Combat")]
    public int DPS = 10;
    public float AttackFrequency    = 1f;
    public float AttackDistanceMax  = 10f;
    public float CaptureDistanceMax = 10f;
    public float eventSharingRange  = 10f;
    public OnAttackedUnitReaction defaultReactionOnAttacked = OnAttackedUnitReaction.Attack;

    [Header("Repairing")]
    public bool CanRepair = false;
    public int RPS = 10;
    public float RepairFrequency = 1f;
    public float RepairDistanceMax = 10f;

    [Header("Movement")]
    [Tooltip("Overrides NavMeshAgent steering settings")]
    public float Speed = 10f;
    public float AngularSpeed = 200f;
    public float Acceleration = 20f;
    public bool IsFlying = false;

    [Header("FX")]
    public GameObject BulletPrefab = null;
    public GameObject DeathFXPrefab = null;
}
