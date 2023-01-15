using System;
using System.Collections;
using System.Collections.Generic;
using Map;
using UnityEditor;
using UnityEngine;

public class InfluencerBaseEntity : Influencer
{
    [SerializeField] protected BaseEntity attachedEntity;
    [SerializeField] private float        radius = 5f;

    public override InfluenceMap.InfluencerData GetData()
    {
        InfluenceMap.InfluencerData data;

        data.position  = transform.position;
        data.radius    = radius;
        data.teamIndex = (int)attachedEntity.GetTeam();

        return data;
    }
}
