using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Map
{
    public class FogElementScene : FogElement
    {
        [SerializeField] public float entityRadius = 10f;
        [SerializeField] public float sightRadius = 20f;
        [SerializeField] public int teamIndex = -1;

        [SerializeField] public bool isDiscovered = true;

        [SerializeField] public List<Renderer> renderers = new List<Renderer>();

        public override FogOfWar.FogClearData GetData()
        {
            FogOfWar.FogClearData data = new FogOfWar.FogClearData();

            data.position     = transform.position;
            data.position.y   = 0.0f;
            data.clearRadius  = sightRadius;
            data.entityRadius = entityRadius;
            data.isVisible    = Convert.ToInt32(isDiscovered);
            data.teamIndex    = teamIndex;

            return data;
        }

        public override void UpdateVisibility(bool isVisible)
        {
            for (int i = 0 ; i < renderers.Count; i++)
                renderers[i].enabled = isVisible;
        }
        
    }
}