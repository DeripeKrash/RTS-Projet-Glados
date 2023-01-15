using UnityEngine;

namespace AI.Actions
{
    public class Capture : Action
    {
        private TargetBuilding building = null;
        
        protected override bool ValidatePreconditions(WorldState state)
        {
            return (int) state["unitCount"].value > 0;
        }

        protected override void ProcessEffects(WorldState state)
        {
            building = _aiController.GetBuildingToCapture();

            state["targetCaptured"].value = (int) state["targetCaptured"].value + 1;
            state["resourcesAvailable"].value = (int) state["resourcesAvailable"].value + building.BuildPoints;
        }


        public override float GetCost(WorldState state)
        {
            return 5f;
        }

        public override void Execute()
        {
            if (building == null)
            {
                building = _aiController.GetBuildingToCapture();
            }

            _aiController.Capture(building);
        }
    }
}