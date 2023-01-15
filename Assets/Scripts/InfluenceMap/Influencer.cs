using System.Collections;
using System.Collections.Generic;
using Map;
using UnityEngine;

public abstract class Influencer : MonoBehaviour
{
    public virtual InfluenceMap.InfluencerData GetData()
    {
        InfluenceMap.InfluencerData data;

        data.position  = Vector3.zero;
        data.radius    = 0f;
        data.teamIndex = -1;

        return data;
    }
}
