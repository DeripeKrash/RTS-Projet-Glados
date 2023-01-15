using UnityEngine;

namespace AI.Actions
{
    public class CreateLightFactory : Action
    {
        protected override bool ValidatePreconditions(WorldState state)
        {
            return (int) state["resourcesAvailable"].value >= _aiController.GetFactoryCost(0);
        }

        protected override void ProcessEffects(WorldState state)
        {
            int availableResource = (int) state["resourcesAvailable"].value;
            int finalResources = availableResource - _aiController.GetFactoryCost(0);
            state["resourcesAvailable"].value = finalResources;
            state["lightFactory"].value = (int) state["lightFactory"].value + 1;
        }


        public override float GetCost(WorldState state)
        {
            return _aiController.GetFactoryCost(0);
        }
        
        public override void Execute()
        {
            Vector3 position = _aiController.FindFactoryBuildLocation(0);
            _aiController.BuildFactory(0, position);
        }
    }
}