using UnityEngine;

namespace AI.Actions
{
    public class DestroyHeavyFactory : Action
    {
        private int _factoryType = 1;
        

        protected override bool ValidatePreconditions(WorldState state)
        {
            int nbHeavy = (int) state["heavyFactory"].value;

            return nbHeavy > 1;
        }

        protected override void ProcessEffects(WorldState state)
        {
            state["heavyFactory"].value = (int) state["heavyFactory"].value - 1;
        }


        public override float GetCost(WorldState state)
        {
            return -_aiController.GetFactoryCost(_factoryType) + 30;
        }

        public override void Execute()
        {
            _aiController.GetFirstFactoryOfType(_factoryType).OnDeadEvent.Invoke();
        }
    }
}