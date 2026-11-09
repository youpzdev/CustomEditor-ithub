using System;
using UnityEngine;

[Serializable]
public class TweenSequenceEntry
{
    [SerializeField] private Transform target;
    [SerializeField] private TweenPreset preset;
    [SerializeField] private bool joinPrevious;
    [SerializeField] [Min(0)] private float delay;

    public Transform Target {get => target; set => target = value;}
    public TweenPreset Preset {get => preset; set => preset = value;}
    public bool JoinPrevious {get => joinPrevious; set => joinPrevious = value;}
    public float Delay {get => delay; set => delay = value;}
}
