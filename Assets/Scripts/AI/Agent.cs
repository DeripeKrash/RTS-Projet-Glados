using System;
using System.Collections;
using System.Collections.Generic;
using AI.Actions;
using UnityEngine;
using UnityEngine.Assertions;

namespace AI
{
    public class Agent : MonoBehaviour
    {
        [SerializeField] protected AIController aiController;

        [SerializeField]
        [Min(.25f)]
        [Tooltip("Time elapsed between 2 updates (seconds)")]
        float updateEvery = 1f;

        float lastUpdateTime = 0f;
        
        public List<Action> actions = new List<Action>();

        protected UnitController opponentController;
        protected WorldState     worldState;
        protected WorldState     goal;

        Queue<Action> _actionQueue = new Queue<Action>();
        Action        activeAction = null;
        bool          goalAchieved = false;

        // Unity
        private void Start()
        {
            ETeam opponent = GameServices.GetOpponent(aiController.GetTeam());
            opponentController = GameServices.GetControllerByTeam(opponent);

            worldState = new WorldState();
            goal       = new WorldState();

            int   buildPoints        = aiController.TotalBuildPoints;
            int   lightFactoCount    = aiController.GetLightFactoryCount();
            int   heavyFactoCount    = aiController.GetHeavyFactoryCount();
            float ourMilitaryForce   = aiController.EvaluateMilitaryForce();
            float enemyMilitaryForce = opponentController.EvaluateMilitaryForce();
            int   unitCount          = aiController.GetUnitCount();
            int   capturedTargets    = aiController.CapturedTargets;

            worldState.Add("resourcesAvailable", new Data(Objective.Eq, buildPoints));
            worldState.Add("lightFactory",       new Data(Objective.Eq, lightFactoCount));
            worldState.Add("heavyFactory",       new Data(Objective.Eq, heavyFactoCount));
            worldState.Add("militaryPower",      new Data(Objective.Eq, ourMilitaryForce));
            worldState.Add("enemyMilitaryPower", new Data(Objective.Eq, enemyMilitaryForce));
            worldState.Add("unitCount",          new Data(Objective.Eq, unitCount));
            worldState.Add("targetCaptured",     new Data(Objective.Eq, capturedTargets));

            goal.Add("resourcesAvailable",       new Data(Objective.Ind, buildPoints));
            goal.Add("lightFactory",             new Data(Objective.Ind,  lightFactoCount));
            goal.Add("heavyFactory",             new Data(Objective.Ind,  heavyFactoCount));
            goal.Add("militaryPower",            new Data(Objective.Ind, ourMilitaryForce));
            goal.Add("enemyMilitaryPower",       new Data(Objective.Ind, enemyMilitaryForce));
            goal.Add("unitCount",                new Data(Objective.Ind, unitCount));
            goal.Add("targetCaptured",           new Data(Objective.Ind, capturedTargets));

            lastUpdateTime = Time.time;
        }

        void Update()
        {
            UpdateWorldState();
            
                if (activeAction == null && _actionQueue.Count > 0)
                {
                    activeAction = _actionQueue.Dequeue();
                    activeAction.Execute();

                    goalAchieved = (_actionQueue.Count == 0);
                    if (goalAchieved)
                    {
                        Debug.Log("Goal achieved");
                    }
                }
                
                if (_actionQueue.Count > 0)
                {
                    Action nextAction = _actionQueue.Peek();
                    if (nextAction.IsValid(worldState))
                    {
                        activeAction = null;
                    }
                }
                else if (goalAchieved)
                {
                    PlanActions();
                }
        }


        private List<Action> GetValidActions(WorldState state)
        {
            List<Action> validActions = new List<Action>();
            foreach (Action action in actions)
            {
                if (action.IsValid(state))
                {
                    validActions.Add(action);
                }
            }

            return validActions;
        }
        
        private void ResetGoal()
        {            
            goal["resourcesAvailable"] = new Data(Objective.Ind, 0);
            goal["lightFactory"]       = new Data(Objective.Ind, 0);
            goal["heavyFactory"]       = new Data(Objective.Ind, 0);
            goal["militaryPower"]      = new Data(Objective.Ind, 0f);
            goal["enemyMilitaryPower"] = new Data(Objective.Ind, 0f);
            goal["unitCount"]          = new Data(Objective.Ind, 0);
            goal["targetCaptured"]     = new Data(Objective.Ind, 0);
            
        }

        public void SetGoalValue(string key, Objective objective, object value)
        {
            worldState[key] = new Data(objective, value);
        }

        void OffsetGoalValue(string key, Objective objective, float extraRatio)
        {
            Data worldStateData = worldState[key];
            Data goalData       = goal[key];

            switch (worldStateData.type)
            {
                case DataType.Integer:
                    goalData.value = (int)goalData.value + Mathf.CeilToInt(extraRatio * (int)goalData.value);
                    break;

                case DataType.Float:
                    goalData.value = (float)goalData.value + (float)extraRatio * (float)goalData.value;
                    break;

                case DataType.Boolean:
                    goalData.value = (bool)goalData.value | (bool)(extraRatio > 1f);
                    break;

                default:
                    break;
            }

            goalData.objective = objective;
        }


        // Public
        public void UpdateWorldState()
        {
            if (!aiController)
                return;
            
            worldState["resourcesAvailable"].value = aiController.TotalBuildPoints;
            worldState["lightFactory"].value       = aiController.GetLightFactoryCount();
            worldState["heavyFactory"].value       = aiController.GetHeavyFactoryCount();
            worldState["militaryPower"].value      = aiController.EvaluateMilitaryForce();
            worldState["enemyMilitaryPower"].value = opponentController.EvaluateMilitaryForce();
            worldState["unitCount"].value          = aiController.GetUnitCount();
            worldState["targetCaptured"].value     = aiController.CapturedTargets;
        
        }

        public WorldState GetGoal()
        {
            return goal;
        }

        public void PlanActions()
        {
            float now         = Time.time;
            float timeElapsed = now - lastUpdateTime;
            
            if (timeElapsed <= updateEvery)
                return;

            lastUpdateTime = now;
            _actionQueue = Planner.Plan(actions, worldState, goal);

#if UNITY_EDITOR
            Action[] actionArray = _actionQueue.ToArray();
            string output = "New plan established:";

            for (int i = 0; i < actionArray.Length; i++)
            {
                output += "\n#" + i.ToString() + ": " + actionArray[i].gameObject.name;
            }
            Debug.Log(output);
#endif
        }

        public void AttackUtilityNowActive()
        {
            Debug.LogWarning("AI new goal: attack");

            // Ignore
            goal["resourcesAvailable"].objective = Objective.Ind;
            goal["lightFactory"].objective       = Objective.Ind;
            goal["heavyFactory"].objective       = Objective.Ind;
            goal["unitCount"].objective          = Objective.Ind;

            float ourMilitaryPower   = (float)worldState["militaryPower"].value;
            float enemyMilitaryPower = (float)worldState["enemyMilitaryPower"].value;

            float enemyMilitaryPowerGoal = Mathf.Clamp(enemyMilitaryPower - ourMilitaryPower,
                                                       0f, Mathf.Infinity);
            float ourMilitaryPowerGoal   = Mathf.Clamp(ourMilitaryPower - enemyMilitaryPower,
                                                       0f, Mathf.Infinity);

            // Change all parameters affected by an attack, but also encourage the AI
            // to keep its available resources at the same level to kick in other actions
            Data data      = goal["militaryPower"];
            data.value     = ourMilitaryPowerGoal;
            data.objective = Objective.Inf;
            
            data           = goal["enemyMilitaryPower"];
            data.value     = enemyMilitaryPowerGoal;
            data.objective = Objective.Sup;

            data           = goal["resourcesAvailable"];
            data.value     = worldState["resourcesAvailable"].value;
            data.objective = Objective.Inf;

            goalAchieved = false;
            PlanActions();
        }

        public void AcquireIntelUtilityNowActive()
        {
            // TODO: add global vision and vision on enemy scores
            // Leroy jenkins for now
            Debug.LogWarning("AI new goal: acquire intel");

            goalAchieved = false;
        }

        public void AcquireUnitsUtilityNowActive()
        {
            ResetGoal();

            Debug.LogWarning("AI new goal: acquire units");

            // Ignore
            goal["militaryPower"].objective      = Objective.Ind;
            goal["enemyMilitaryPower"].objective = Objective.Ind;
            goal["targetCaptured"].objective     = Objective.Ind;
            
            Data data      = goal["unitCount"];
            data.value     = aiController.GetUnitCount();
            data.objective = Objective.Sup;

            data           = goal["militaryPower"];
            data.value     = opponentController.EvaluateMilitaryForce() + 500f;
            data.objective = Objective.Sup;
            
            data           = goal["lightFactory"];
            data.value     = aiController.GetLightFactoryCount();
            data.objective = Objective.Eq;
            
            data           = goal["heavyFactory"];
            data.value     = aiController.GetHeavyFactoryCount() + 1;
            data.objective = Objective.Inf;

            goalAchieved = false;
            PlanActions();
        }
        
        public void AcquireResourcesUtilityNowActive()
        {
            Debug.LogWarning("AI new goal: acquire resources");

            // Ignore the following
            goal["militaryPower"].objective      = Objective.Ind;
            goal["enemyMilitaryPower"].objective = Objective.Ind;
            goal["resourcesAvailable"].objective = Objective.Ind;

            Data data      = goal["lightFactory"];
            data.value     = worldState["lightFactory"].value;
            data.objective = Objective.Eq;

            data           = goal["heavyFactory"];
            data.value     = worldState["heavyFactory"].value;
            data.objective = Objective.Eq;
            
            data           = goal["unitCount"];
            data.value     = 2;
            data.objective = Objective.Sup;

            data           = goal["targetCaptured"];
            data.value     = aiController.CapturedTargets + 1;
            data.objective = Objective.Eq;

            goalAchieved = false;
            PlanActions();
        }
    }

}