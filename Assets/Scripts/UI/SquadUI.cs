using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SquadUI : MonoBehaviour
{
    // ========== Inspector ==========
    [SerializeField]
    PlayerController playerController;

    [SerializeField]
    Text unitCountDisplay;


    // ========== Internal ==========
    Canvas squadUICanvas;


    // ========== Unity's methods ==========
    void Awake()
    {
        playerController = GameObject.Find("PlayerController").GetComponent<PlayerController>();
    }

    void Start()
    {
        squadUICanvas = GetComponent<Canvas>();
        
        Awake();
        Debug.Assert(playerController != null);
    }

    void Update()
    {
        int unitCount = playerController.GetSelectedUnitCount();

        squadUICanvas.enabled = unitCount > 0;
        unitCountDisplay.text = unitCount.ToString();
    }


    // ========== Public setters ==========
    // Button's OnClick() doesn't accept enums as parameters
    // Although dirty, this is a work-around
    public void SetRegimentSquadFormation()
    {
        playerController.SetSquadFormation(FormationType.Regiment);
    }

    public void SetDiscSquadFormation()
    {
        playerController.SetSquadFormation(FormationType.Disc);
    }

    public void SetRandomSquadFormation()
    {
        playerController.SetSquadFormation(FormationType.Random);
    }
}
