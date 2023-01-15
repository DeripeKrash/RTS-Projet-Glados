using UnityEngine;

namespace AI.Actions
{
    public class DestroyUnits: Action
    {

        protected override bool ValidatePreconditions(WorldState state)
        {
            return true;
        }

        protected override void ProcessEffects(WorldState state)
        {
        }


        public override float GetCost(WorldState state)
        {
            return 999999;
        }


        public override void Execute()
        {
            // _aiController.DestroySelectedUnits();

            // UnselectUnitsAction
            // _unitController.UnselectUnit();
            // _unitController.UnselectUnits();
        }
    }
}