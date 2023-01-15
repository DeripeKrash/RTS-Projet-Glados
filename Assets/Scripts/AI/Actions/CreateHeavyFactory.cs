using UnityEngine;

namespace AI.Actions
{
    public class CreateHeavyFactory: Action
    {
        protected override bool ValidatePreconditions(WorldState state)
        {
            return (int)state["resourcesAvailable"].value >= _aiController.GetFactoryCost(1);
        }

        protected override void ProcessEffects(WorldState state)
        {
            int availableResource = (int) state["resourcesAvailable"].value;
            int finalResources = availableResource - _aiController.GetFactoryCost(1);
            state["resourcesAvailable"].value = finalResources;
            state["heavyFactory"].value = (int) state["heavyFactory"].value + 1;
        }


        public override float GetCost(WorldState state)
        {
            return _aiController.GetFactoryCost(1);
        }


        public override void Execute()
        {
            Vector3 position = _aiController.FindFactoryBuildLocation(1);
            _aiController.BuildFactory(1, position);
        }
    }
}