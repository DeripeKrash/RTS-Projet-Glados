using UnityEngine;
using UnityEngine.Events;

public class Utility : MonoBehaviour
{
    // ========== Inspector data ==========
    [SerializeField]
    [Tooltip("The logic behind the score evaluation of this utility")]
    public Heuristic heuristic;

    [SerializeField]
    [Tooltip("Condition used to determine whether the score returned by the heuristic is significant")]
    public Comparator scoreSignificance;

    [SerializeField]
    [Tooltip("If another utility returns the same score, the priority level decides which is picked")]
    public int priority = 0;

    [SerializeField]
    [Tooltip("If writeScoreTo is not null, the heuristic score is written in this stat when it is evaluated")]
    public Stat writeScoreTo;

    [Header("Callbacks")]
    [SerializeField]
    public UnityEvent OnEnter;

    [SerializeField]
    public UnityEvent OnUpdate;

    [SerializeField]
    public UnityEvent OnExit;

    public float Evaluate()
    {
        float score = heuristic.Evaluate();
        
        writeScoreTo?.SetValue(score);

        return score;
    }
}