using UnityEngine;

[CreateAssetMenu(fileName = "CivilianData", menuName = "Game Data/Civilian Data")]
public class CivilianDataSO : NPCDataSO
{
    [Header("Civilian Behavior")]
    [field: SerializeField] public float panicDuration { get; private set; } = 5f;
    [field: SerializeField] public float fleeSpeed { get; private set; } = 3f;
    [field: SerializeField] public float fleeDistance { get; private set; } = 10f;
    [field: SerializeField] public float fearRadius { get; private set; } = 12f;
}