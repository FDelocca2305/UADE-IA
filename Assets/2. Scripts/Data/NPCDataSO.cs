using Services.MicroServices.BlackboardService;
using UnityEngine;

public enum NPCGender
{
    Male,
    Female
}

[CreateAssetMenu(fileName = "NPCData", menuName = "Game Data/NPC Data")]
public class NPCDataSO : CharacterDataSO
{
    [Header("NPC Info")]
    [field: SerializeField] public NPCGender gender { get; private set; } = NPCGender.Male;

    [Header("AI Detection")]
    [field: SerializeField] public float detectionRange { get; private set; } = 8f;
    [field: SerializeField] public float fieldOfView { get; private set; } = 90f;
    [field: SerializeField] public AIPersonalityType personalityType { get; private set; } = AIPersonalityType.Conservative;
    
    [Header("AI Behavior")]
    [field: SerializeField] public float idleTime { get; private set; } = 3f;
    [field: SerializeField] public float searchTime { get; private set; } = 5f;
    [field: SerializeField] public float patrolSpeed { get; private set; } = 2f;
    [field: SerializeField] public new float rotationSpeed { get; private set; } = 5f;
    
    [Header("Patrol Points")]
    [field: SerializeField] public Transform[] patrolPoints { get; set; }
}