using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace AI
{
    public abstract class Action : MonoBehaviour
    {
        [SerializeField] protected AIController _aiController;
        [SerializeField] protected bool repeatable = false;
        [SerializeField] public bool Repeatable => repeatable;
        
        public abstract void Execute();

        protected abstract bool ValidatePreconditions(WorldState state); //procedural preconditions

        protected abstract void ProcessEffects(WorldState state); // procedural effects

        public abstract float GetCost(WorldState state); //procedural costs


        public bool IsValid(WorldState state)
        {
            WorldState newState = new WorldState(state);
            return ValidatePreconditions(newState);
        }

        public WorldState GetProcessedState(WorldState state)
        {
            WorldState newState = new WorldState(state);
            ProcessEffects(newState);
            return newState;
        }

        public void ExecuteIfValid(WorldState state)
        {
            if (IsValid(state))
            {
                Execute();
            }
        }
        
    }
}