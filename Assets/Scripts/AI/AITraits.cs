using System;
using UnityEngine;

public class AITraits : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Tendency to start an attack")]
    public float aggressive = .5f;

    [SerializeField]
    [Tooltip("Tendency to hold on resources")]
    public float cheap = .5f;

    [SerializeField]
    [Tooltip("Tendency to scout for intel")]
    public float curious = .5f;
}