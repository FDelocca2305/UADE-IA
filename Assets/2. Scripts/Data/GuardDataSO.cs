using UnityEngine;
using Scripts.FSM.Models;
using System.Collections.Generic;
using ScriptableObjects.Bullets;
using Scripts.FSM.Base.StateMachine;

[CreateAssetMenu(fileName = "GuardData", menuName = "Game Data/Guard Data")]
public class GuardDataSO : NPCDataSO
{
    [Header("Guard Combat")]
    [field: SerializeField] public float attackRange { get; private set; } = 2f;
    [field: SerializeField] public float chaseSpeed { get; private set; } = 4f;
    [field: SerializeField] public BulletData bulletData { get; private set; }

    [Header("FSM Patrol Settings")]
    [field: SerializeField] public int loopsToIdle { get; private set; } = 3;
    [field: SerializeField] public float idleSeconds { get; private set; } = 5f;

    [Header("State Machine Configuration")]
    [field: SerializeField] public List<StateData> stateDataList { get; private set; } = new List<StateData>();
    [field: SerializeField] public bool useFSM { get; private set; } = true;

    [Header("AI Configuration")]
    [field: SerializeField] public bool enableNewAISystem { get; private set; } = true;

    [Header("Steering Physics")]
    [field: SerializeField] public float mass { get; set; } = 1f;
    [field: SerializeField] public float maxForce { get; set; } = 25f;
    [field: SerializeField] public float maxSpeed { get; set; } = 8f;
    [field: SerializeField] public float slowingDistance { get; set; } = 2f;

    [Header("Obstacle Avoidance")]
    [field: SerializeField] public LayerMask obstaclesMask { get; set; } = -1;
    [field: SerializeField] public float avoidRadius { get; set; } = 2f;
    [field: SerializeField] public float avoidAngle { get; set; } = 90f;
    [field: SerializeField] public float personalArea { get; set; } = 0.5f;

    [Header("Advanced Detection")]
    [field: SerializeField] public float baseRotationSpeed { get; private set; } = 2f;
}