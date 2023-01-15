using UnityEngine;

namespace AI.Actions
{
    public class DestroyLightFactory : Action
    {
        private int _factoryType = 0;
        

        protected override bool ValidatePreconditions(WorldState state)
        {
            int nbLight = (int) state["lightFactory"].value;

            return nbLight > 1;
        }

        protected override void ProcessEffects(WorldState state)
        {
            state["lightFactory"].value = (int) state["lightFactory"].value - 1;
        }


        public override float GetCost(WorldState state)
        {
            return -_aiController.GetFactoryCost(_factoryType) + 20;
        }
        
        public override void Execute()
        {
            _aiController.GetFirstFactoryOfType(_factoryType).OnDeadEvent.Invoke();
        }
    }
}