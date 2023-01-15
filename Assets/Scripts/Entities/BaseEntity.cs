using System;
using UnityEngine;
using UnityEngine.UI;

public abstract class BaseEntity : MonoBehaviour, ISelectable, IDamageable, IRepairable
{
    // ========== Inspector data ==========
    [SerializeField]
    protected ETeam Team;

    [SerializeField]
    protected Renderer[] renderers;

    protected int        HP = 0;
    protected Action     OnHpUpdated;
    protected GameObject SelectedSprite = null;
    protected Text       HPText         = null;
    protected bool       IsInitialized  = false;

    public Action        OnEntityDamaged;
    public Action        OnDeadEvent;
    public bool          IsSelected { get; protected set; }
    public bool          IsAlive    { get; protected set; }
    
    virtual public void Init(ETeam _team)
    {
        if (IsInitialized)
            return;

        Team = _team;

        IsInitialized = true;

        UpdateVisibility(false);
    }

    public Color GetColor()
    {
        return GameServices.GetTeamColor(GetTeam());
    }

    public int GetHP()
    {
        return HP;
    }

    void UpdateHpUI()
    {
        if (HPText != null)
            HPText.text = "HP : " + HP.ToString();
    }

    public virtual void UpdateVisibility(bool isVisible)
    {
        int max = renderers.Length;

        for (int i = 0; i < max; i++)
        {
            renderers[i].enabled = isVisible;
        }
       
        HPText.enabled       = isVisible;
    }

    public virtual EntityDataScriptable GetEntityData()
    {
        return null;
    }

    #region ISelectable
    public void SetSelected(bool selected)
    {
        IsSelected = selected;
        SelectedSprite?.SetActive(IsSelected);
    }
    public ETeam GetTeam()
    {
        return Team;
    }
    #endregion

    #region IDamageable
    public void AddDamage(int damageAmount)
    {
        if (IsAlive == false)
            return;

        HP -= damageAmount;
        OnEntityDamaged?.Invoke();

        OnHpUpdated?.Invoke();

        if (HP <= 0)
        {
            IsAlive = false;
            OnDeadEvent?.Invoke();
            Debug.Log("Entity " + gameObject.name + " died");
        }

    }

    public void Destroy()
    {
        HP -= HP;
        IsAlive = false;
        OnHpUpdated?.Invoke();
        OnDeadEvent?.Invoke();
    }
    #endregion

    #region IRepairable
    virtual public bool NeedsRepairing()
    {
        return true;
    }
    virtual public void Repair(int amount)
    {
        OnHpUpdated?.Invoke();
    }
    virtual public void FullRepair()
    {
    }
    #endregion

    #region MonoBehaviour methods
    virtual protected void Awake()
    {
        IsAlive = true;

        SelectedSprite = transform.Find("SelectedSprite")?.gameObject;
        SelectedSprite?.SetActive(false);

        Transform hpTransform = transform.Find("Canvas/HPText");
        if (hpTransform)
            HPText = hpTransform.GetComponent<Text>();

        OnHpUpdated += UpdateHpUI;
    }
    virtual protected void Start()
    {
        UpdateHpUI();
    }
    virtual protected void Update()
    {
    }
    #endregion
}
