using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Map
{
    public class FogElementBaseEntity : FogElement
    {
        [SerializeField] protected BaseEntity entity;
        
        public override FogOfWar.FogClearData GetData()
        {
            FogOfWar.FogClearData data = new FogOfWar.FogClearData();

            data.position     = transform.position;
            data.position.y   = 0.0f;
            data.clearRadius  = entity.GetEntityData().sightRadius;
            data.entityRadius = entity.GetEntityData().entityRadius;
            data.isVisible    = 0;
            data.teamIndex    = (int)entity.GetTeam();

            return data;
        }

        public override void UpdateVisibility(bool isVisible)
        {
            entity.UpdateVisibility(isVisible);
        }

        public override int GetTeamIndex()
        {
            return (int)entity.GetTeam();
        }
    }
}
