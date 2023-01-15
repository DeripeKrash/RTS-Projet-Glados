using UnityEngine;
using UnityEngine.AI;

// Since a Unit can transition between any state freely, an enum is enough
// to track its state, rather than using an FSM
public enum UnitState
{
    Idle = 0,
    Moving,
    MovingToAttack,
    MovingToRepair,
    MovingToCapture,
    MovingOffensively,
    Attacking,
    Repairing,
    Capturing
}

public class Unit : BaseEntity
{
    // ========== Inspector data ==========
    [SerializeField]
    UnitDataScriptable UnitData = null;


    // ========== Internal ==========
    delegate void StateUpdateFunc();
    delegate void AttackReactionFunc(Unit unit);
    StateUpdateFunc[]    StateUpdate;
    AttackReactionFunc[] AttackIdleReaction;

    
    Transform      BulletSlot;
    NavMeshAgent   NavMeshAgent;
    BaseEntity     EntityTarget   = null;
    TargetBuilding CaptureTarget  = null;
    float          LastActionDate = 0f;
    UnitState      currentState   = UnitState.Idle;


    // ========== Properties / public data ==========
    public UnitDataScriptable GetUnitData { get { return UnitData; } }
    public int                Cost        { get { return UnitData.Cost; } }
    public int                GetTypeId   { get { return UnitData.TypeId; } }
    
    [HideInInspector]
    public OnAttackedUnitReaction onAttackedIdleReaction;


    // ========== Methods ==========
    override public void Init(ETeam _team)
    {
        if (IsInitialized)
            return;

        base.Init(_team);

        HP = UnitData.MaxHP;
        OnDeadEvent += Unit_OnDead;

        // Initialize method look-up arrays
        StateUpdate = new StateUpdateFunc[]
        {
            () => {},
            MovingUpdate,
            MovingToAttackUpdate,
            MovingToRepairUpdate,
            MovingToCaptureUpdate,
            MovingOffensivelyUpdate,
            AttackingUpdate,
            RepairingUpdate,
            CapturingUpdate
        };

        AttackIdleReaction = new AttackReactionFunc[]
        {
            (Unit unit) => {},
            OnAttackedReaction_Attack,
            OnAttackedReaction_Flee
        };

        onAttackedIdleReaction = UnitData.defaultReactionOnAttacked;
    }

    void Unit_OnDead()
    {
        StopCapture();

        if (UnitData.DeathFXPrefab)
        {
            GameObject fx = Instantiate(UnitData.DeathFXPrefab, transform);
            fx.transform.parent = null;
        }

        Destroy(gameObject);
    }


    public UnitState GetState()
    {
        return currentState;
    }

    // ========== Unity methods ==========
    override protected void Awake()
    {
        base.Awake();

        NavMeshAgent = GetComponent<NavMeshAgent>();
        BulletSlot   = transform.Find("BulletSlot");

        // fill NavMeshAgent parameters
        NavMeshAgent.speed        = UnitData.Speed;
        NavMeshAgent.angularSpeed = UnitData.AngularSpeed;
        NavMeshAgent.acceleration = UnitData.Acceleration;
    }

    override protected void Start()
    {
        // Needed for non factory spawned units (debug)
        if (!IsInitialized)
            Init(Team);

        Debug.Assert(NavMeshAgent != null);

        base.Start();
    }

    override protected void Update()
    {
        StateUpdate[(int)currentState]();
	}
    

    // ========== Getters ==========
    public override EntityDataScriptable GetEntityData()
    {
        return UnitData;
    }


    // ========== Checks ==========
    override public bool NeedsRepairing()
    {
        return HP < UnitData.MaxHP;
    }

    public bool IsAnAlly(BaseEntity target)
    {
        return target.GetTeam() == GetTeam();
    }

    public bool IsInControlOf(TargetBuilding target)
    {
        return target.GetTeam() == Team;
    }

    public bool CanRepair(BaseEntity target)
    {
        if (UnitData.CanRepair == false || !IsAnAlly(target))
            return false;

        // distance check
        Vector3 toTarget    = target.transform.position - transform.position;
        float   repairDist2 = UnitData.RepairDistanceMax * UnitData.RepairDistanceMax;

        return toTarget.sqrMagnitude <= repairDist2;
    }

    public bool AttacksCanReach(BaseEntity target)
    {
        Vector3 toTarget         = target.transform.position - transform.position;
        float   unitAttackRange2 = UnitData.AttackDistanceMax * UnitData.AttackDistanceMax;

        return toTarget.sqrMagnitude <= unitAttackRange2;
    }

    public bool CanCapture(TargetBuilding target)
    {
        Vector3 toTarget      = target.transform.position - transform.position;
        float   captureRange2 = UnitData.CaptureDistanceMax * UnitData.CaptureDistanceMax;

        return toTarget.sqrMagnitude <= captureRange2;
    }

    public bool HasLineOfSightOn(Transform target)
    {
        // NavMeshAgent.Raycast is unreliable, somestimes doesn't work when it should
        RaycastHit hit;

        // Ignore self
        int oldLayer = gameObject.layer;
        gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");

        // Cast the ray
        Vector3 dir = target.position - transform.position;
        Physics.Raycast(transform.position, dir, out hit, Mathf.Infinity);

        gameObject.layer = oldLayer;

        return hit.collider.transform == target;
    }


    // ========== On attacked while idle reactions ==========
    public void OnAttackedReaction_Flee(Unit enemy)
    {
        if (currentState == UnitState.Idle)
        {
            float   enemyRange = enemy.UnitData.AttackDistanceMax;
            Vector3 fleeVec    = transform.position - enemy.transform.position;
            Vector3 fleeOffset = Vector3.ClampMagnitude(fleeVec, enemyRange);

            NavMeshAgent.speed = UnitData.Speed;
            StartMovingTo(transform.position + fleeOffset);
        }
    }

    public void OnAttackedReaction_Attack(Unit enemy)
    {
        if (currentState == UnitState.Idle)
        {
            StartAttacking(enemy);
        }
    }


    // ========== Actions on self ==========
    // IRepairable interface implementation
    override public void Repair(int amount)
    {
        HP = Mathf.Min(HP + amount, UnitData.MaxHP);
        base.Repair(amount);
    }

    override public void FullRepair()
    {
        Repair(UnitData.MaxHP);
    }

    public void StopCapture()
    {
        if (CaptureTarget == null)
            return;

        CaptureTarget.StopCapture(this);
        CaptureTarget = null;
        currentState = UnitState.Idle;
    }

    public void UpdatePathToTarget()
    {
        bool success = NavMeshAgent.SetDestination(EntityTarget.transform.position);
        
        // Cannot be reached
        if (!success)
        {
            currentState = UnitState.Idle;
            NavMeshAgent.isStopped = true;
            EntityTarget = null;
        }
    }

    public void SetMovespeed(float movespeed)
    {
        NavMeshAgent.speed = movespeed;
    }

    public void SetDefaultMovespeed()
    {
        NavMeshAgent.speed = UnitData.Speed;
    }

    public void AnswerToAttackFrom(Unit enemy)
    {
        if (currentState == UnitState.Idle)
        {
            AttackIdleReaction[(int)onAttackedIdleReaction](enemy);
        }
    }

    protected void WarnNearbyAlliesOfAttackFrom(Unit origin)
    {
        int        sphereLayer = LayerMask.GetMask("Unit");
        Collider[] nearbyUnits = Physics.OverlapSphere(transform.position,
                                                       UnitData.eventSharingRange,
                                                       sphereLayer);

        foreach (Collider unitCollider in nearbyUnits)
        {
            Unit unit = unitCollider.gameObject.GetComponent<Unit>();

            if (unit.GetTeam() == Team)
            {
                unit.AnswerToAttackFrom(origin);
            }
        }
    }

    public void OnAttackedBy(Unit enemy)
    {
        AnswerToAttackFrom(enemy);
        WarnNearbyAlliesOfAttackFrom(enemy);
    }


    // ========== Tasks start ==========
    public bool StartMovingTo(Vector3 pos)
    {
        bool success = NavMeshAgent.SetDestination(pos);

        if (success)
        {
            StopCapture();
            NavMeshAgent.isStopped = false;
            currentState = UnitState.Moving;
        }

        return success;
    }

    public bool StartMovingToAttack()
    {
        StopCapture();

        NavMeshAgent.isStopped = false;
        currentState           = UnitState.MovingToAttack;

        return true;
    }

    public bool StartMovingOffensivelyTo(Vector3 position)
    {
        StopCapture();

        bool success = NavMeshAgent.SetDestination(position);

        if (success)
        {
            NavMeshAgent.isStopped = false;
            currentState           = UnitState.MovingOffensively;
        }

        return success;
    }

    public bool StartAttacking(BaseEntity target)
    {
        if (target == null || IsAnAlly(target))
        {
            return false;
        }
        
        bool success = AttacksCanReach(target);
        
        // NavMeshAgent.isOnNavMesh is tested because unit nav mesh agents
        // seem to not be unregistered immediately
        if (success == false && NavMeshAgent.isOnNavMesh)
        {
            success = NavMeshAgent.SetDestination(target.transform.position);

            if (success)
            {
                EntityTarget = target;
                StartMovingToAttack();
            }

            return success;
        }

        StopCapture();
        currentState = UnitState.Attacking;
        EntityTarget = target;

        return success;
    }

    public bool StartCapturing(TargetBuilding target)
    {
        if (CaptureTarget == target)
        {
            return true;
        }
        else if (IsInControlOf(target))
        {
            return false;
        }

        if (CanCapture(target) == false)
        {
            bool success = NavMeshAgent.SetDestination(target.transform.position);

            if (success)
            {
                StopCapture();
                NavMeshAgent.isStopped = false;
                currentState           = UnitState.MovingToCapture;
                CaptureTarget          = target;
            }

            return success;
        }

        if (CaptureTarget != null)
        {
            CaptureTarget.StopCapture(this);
        }

        NavMeshAgent.isStopped = true;
        target.StartCapture(this);
        currentState  = UnitState.Capturing;
        CaptureTarget = target;

        return true;
    }

    public bool StartRepairing(BaseEntity entity)
    {
        if (CanRepair(entity) == false)
            return false;

        StopCapture();

        NavMeshAgent.isStopped = true;
        EntityTarget = entity;
        currentState = UnitState.Repairing;

        return true;
    }

    // Updates
    protected void MovingUpdate()
    {
        float dstDist2  = (NavMeshAgent.destination - transform.position).sqrMagnitude;
        float stopDist2 = NavMeshAgent.stoppingDistance * NavMeshAgent.stoppingDistance;

        if (dstDist2 <= stopDist2)
        {
            currentState = UnitState.Idle;
        }
    }

    protected void MovingToAttackUpdate()
    {
        if (EntityTarget == null)
        {
            currentState = UnitState.Idle;
        }
        else if (AttacksCanReach(EntityTarget) && HasLineOfSightOn(EntityTarget.transform))
        {
            NavMeshAgent.isStopped = true;
            currentState           = UnitState.Attacking;
        }
        else
        {
            UpdatePathToTarget();
        }
    }

    protected void MovingToRepairUpdate()
    {
        if (CanRepair(EntityTarget))
        {
            currentState = UnitState.Repairing;
        }
        else
        {
            UpdatePathToTarget();
        }
    }

    protected void MovingToCaptureUpdate()
    {
        if (IsInControlOf(CaptureTarget))
        {
            currentState = UnitState.Idle;
        }
        else if (CanCapture(CaptureTarget))
        {
            NavMeshAgent.isStopped = true;
            CaptureTarget.StartCapture(this);
            currentState = UnitState.Capturing;
        }
    }

    public void MovingOffensivelyUpdate()
    {
        int        unitMask    = 1 << LayerMask.NameToLayer("Unit");
        int        factoryMask = 1 << LayerMask.NameToLayer("Factory");
        int        overlapMask = unitMask | factoryMask;

        Collider[] nearbyUnits = Physics.OverlapSphere(transform.position,
            UnitData.AttackDistanceMax,
            overlapMask);

        ETeam opponentTeam = GameServices.GetOpponent(Team);

        foreach (Collider unitCollider in nearbyUnits)
        {
            BaseEntity entity = unitCollider.gameObject.GetComponent<BaseEntity>();

            if (entity.GetTeam() == opponentTeam)
            {
                StartAttacking(entity);
                return;
            }
        }

        MovingUpdate();
    }

    public void AttackingUpdate()
    {
        // No more target to attack
        if (EntityTarget == null)
        {
            currentState = UnitState.Idle;
            return;
        }
        
        if (!AttacksCanReach(EntityTarget) ||
            !HasLineOfSightOn(EntityTarget.transform))
        {
            StartMovingToAttack();
            return;
        }

        transform.LookAt(EntityTarget.transform);

        // only keep Y axis
        Vector3 eulerRotation = transform.eulerAngles;
        eulerRotation.x = 0f;
        eulerRotation.z = 0f;
        transform.eulerAngles = eulerRotation;

        if ((Time.time - LastActionDate) > UnitData.AttackFrequency)
        {
            LastActionDate = Time.time;
            // visual only ?
            if (UnitData.BulletPrefab)
            {
                GameObject newBullet = Instantiate(UnitData.BulletPrefab, BulletSlot);
                newBullet.transform.parent = null;
                newBullet.GetComponent<Bullet>().ShootToward(EntityTarget.transform.position - transform.position, this);
            }
            // apply damages
            int damages = Mathf.FloorToInt(UnitData.DPS * UnitData.AttackFrequency);
            
            EntityTarget.AddDamage(damages);
            
            // TODO: that's dirty, find another way
            Unit enemy = EntityTarget as Unit;

            if (enemy != null)
            {
                enemy.OnAttackedBy(this);
            }
        }
    }

    public void RepairingUpdate()
    {
        if (CanRepair(EntityTarget) == false)
        {
            currentState = UnitState.MovingToRepair;
            return;
        }

        transform.LookAt(EntityTarget.transform);

        // only keep Y axis
        Vector3 eulerRotation = transform.eulerAngles;
        eulerRotation.x = 0f;
        eulerRotation.z = 0f;
        transform.eulerAngles = eulerRotation;

        if ((Time.time - LastActionDate) > UnitData.RepairFrequency)
        {
            LastActionDate = Time.time;

            // apply reparing
            int amount = Mathf.FloorToInt(UnitData.RPS * UnitData.RepairFrequency);
            EntityTarget.Repair(amount);
        }
    }

    public void CapturingUpdate()
    {
        if (CaptureTarget.GetTeam() == Team)
        {
            currentState = UnitState.Idle;
        }
    }

    public void StopAllActions()
    {
        StopCapture();
        EntityTarget           = null;
        NavMeshAgent.isStopped = true;
    }
}
