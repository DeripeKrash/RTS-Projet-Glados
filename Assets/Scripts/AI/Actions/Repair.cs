using UnityEngine;

namespace AI.Actions
{

    public class Repair : Action
    {

        public override void Execute()
        {
        }

        protected override bool ValidatePreconditions(WorldState state)
        {
            return true;
        }

        protected override void ProcessEffects(WorldState state)
        {
        }

        public override float GetCost(WorldState state)
        {
            return 99999;
        }

    }
}