using UnityEngine;

namespace AI.Actions
{
    public class CreateLightUnit : Action
    {
        [SerializeField] private UnitDataScriptable unitData;

        protected override bool ValidatePreconditions(WorldState state)
        {
            return (int) state["lightFactory"].value > 0 && (int) state["resourcesAvailable"].value >= unitData.Cost;
        }

        protected override void ProcessEffects(WorldState state)
        {
            state["resourcesAvailable"].value = (int) state["resourcesAvailable"].value - unitData.Cost;
            state["unitCount"].value = (int) state["unitCount"].value + 1;
            state["militaryPower"].value = (float) state["militaryPower"].value + unitData.MaxHP * (unitData.DPS + unitData.Cost);
        }

        public override float GetCost(WorldState state)
        {
            return unitData.Cost;
        }

        public override void Execute()
        {
            _aiController.GetFirstFactoryOfType(0).BuildUnitByLabel(unitData.Caption);
        }
    }
}