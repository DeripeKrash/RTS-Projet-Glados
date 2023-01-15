using UnityEngine;
using UnityEngine.UI;

public class SquadReactionUpdater : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Player's squad controller")]
    SquadController playerSquadController;

    [SerializeField]
    [Tooltip("Dropdown component where reactions are picked by the player")]
    Dropdown        reactionChoices;

    void Start()
    {
        if (playerSquadController == null)
        {
            GameObject playerController = GameObject.Find("PlayerController");
            playerSquadController = playerController.GetComponent<SquadController>();
        }

        if (reactionChoices == null)
        {
            reactionChoices = GetComponent<Dropdown>();
        }

        reactionChoices.onValueChanged.AddListener(UpdateSquadReaction);
    }

    public void UpdateSquadReaction(int newValue)
    {
        OnAttackedSquadReaction newReaction = (OnAttackedSquadReaction)newValue;

        playerSquadController.SetControllerReactionOnAttacked(newReaction);
    }
}
