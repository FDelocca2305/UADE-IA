using System.Collections.Generic;
using Scripts.FSM.Base.StateMachine;
using UnityEngine;

[CreateAssetMenu(fileName = "AllyLeaderData", menuName = "Game Data/Ally Leader Data")]
public class AllyLeaderDataSO : ScriptableObject
{
    [Header("Defensive Tactics")]
    [Tooltip("Player health % para activar proteccion")]
    [Range(0f, 1f)]
    [field: SerializeField] public float PlayerProtectThreshold { get; private set; } = 0.3f;

    [Tooltip("Radio de formacion defensiva alrededor del player")]
    [field: SerializeField] public float DefensiveRadius { get; private set; } = 3f;

    [Tooltip("Duracion del override defensivo")]
    [field: SerializeField] public float DefensiveCommandDuration { get; private set; } = 10f;

    [Header("Coordinacion")]
    [Tooltip("Intervalo para reevaluar tacticas")]
    [field: SerializeField] public float TacticsUpdateInterval { get; private set; } = 1f;

    [Tooltip("Radio para descubrir aliados cercanos automaticamente")]
    [field: SerializeField] public float AllyDiscoveryRadius { get; private set; } = 15f;
}
