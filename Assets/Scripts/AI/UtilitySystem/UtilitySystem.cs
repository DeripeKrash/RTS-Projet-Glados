using UnityEngine;
using UnityEngine.Events;

public class UtilitySystem : MonoBehaviour
{
    // ========== Inspector data ==========
    [HideInInspector]
    public int activeUtilityIdx = -1;

    [SerializeField]
    [Tooltip("Rate at which utilities are updated (seconds)")]
    [Min(0f)]
    float updateEvery = .1f;

    [SerializeField]
    UnityEvent BeforeUtilitiesUpdate;

    [SerializeField]
    UnityEvent OnActiveUtilityChanged;

    [SerializeField]
    UnityEvent AfterUtilitiesUpdate;

    // ========== Internal data ==========
    [HideInInspector]
    public Utility[] utilities;
    float            lastUpdateTime;


    // ========== Methods ==========
    void Start()
    {
        utilities      = GetComponentsInChildren<Utility>();
        lastUpdateTime = 0f;
    }

    void Update()
    {
        float currentTime = Time.time;
        if (currentTime - lastUpdateTime >= updateEvery)
        {
            lastUpdateTime = currentTime;
            UpdateUtilities();
        }

        if (activeUtilityIdx != -1)
        {
            utilities[activeUtilityIdx].OnUpdate?.Invoke();
        }
    }

    void UpdateUtilities()
    {
        BeforeUtilitiesUpdate?.Invoke();

        // Find highest scoring utility
        int candidateIdx  = -1;
        int candidatePrio = -1;

        float topScore  = float.MinValue;

        // Evaluate each utility, keep the best score (if any)
        int utilityCount = utilities.Length;
        for (int i = 0; i < utilityCount; i++)
        {
            float score = utilities[i].Evaluate();
            
            // Test score significance, compare to top score, and compare priority
            if (utilities[i].scoreSignificance.Evaluate(score) &&
                (score > topScore                              ||
                (score == topScore && utilities[i].priority > candidatePrio)))
            {
                topScore     = score;
                candidateIdx = i;
                candidatePrio = utilities[i].priority;
            }
        }

        // Check for change of utility
        if (candidateIdx != activeUtilityIdx)
        {
            if (activeUtilityIdx != -1)
            {
                utilities[activeUtilityIdx].OnExit?.Invoke();
            }

            if (candidateIdx != -1)
            {
                utilities[candidateIdx]?.OnEnter?.Invoke();
            }

            activeUtilityIdx = candidateIdx;
            OnActiveUtilityChanged?.Invoke();
        }

        AfterUtilitiesUpdate?.Invoke();
    }
}