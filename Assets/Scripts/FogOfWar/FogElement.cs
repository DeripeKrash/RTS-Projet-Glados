using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Map
{
    public abstract class FogElement : MonoBehaviour
    {
        public virtual FogOfWar.FogClearData GetData()
        {
            FogOfWar.FogClearData data = new FogOfWar.FogClearData();

            data.position = transform.localPosition;
            data.position.y = 0.0f;
            data.clearRadius = 0.0f;
            data.entityRadius = 0.0f;
            data.isVisible = 0;
            data.teamIndex = 0;

            return data;
        }

        public virtual void UpdateVisibility(bool isVisible)
        {
            return;
        }

        public virtual int GetTeamIndex()
        {
            return 0;
        }
    }
}
