using UnityEngine;

namespace AI.Actions
{
    public class Attack : Action
    {
        
        private Vector3? target = null;
        
        public override void Execute()
        {
            if (target == null)
            {
                target = _aiController.GetTargetLocationToAttack();
            }
            
            _aiController.OffensivelyMoveTo(target.Value); //add military power argument
        }

        protected override bool ValidatePreconditions(WorldState state)
        {
            return (int) state["unitCount"].value > 0;
        }

        protected override void ProcessEffects(WorldState state)
        {
            float enemyMilitaryPower = 0f;
            float ourMilitaryPower = 0f;
            target = _aiController.GetTargetLocationToAttack();
            _aiController.EvaluateFight(target.Value, ref enemyMilitaryPower, ref ourMilitaryPower);
            state["militaryPower"].value = ourMilitaryPower;
            state["enemyMilitaryPower"].value = enemyMilitaryPower;
        }


        public override float GetCost(WorldState state)
        {
            return 1;
        }
        
    }
}