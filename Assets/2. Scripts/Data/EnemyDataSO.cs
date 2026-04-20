using ScriptableObjects.Bullets;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Game Data/Enemy Data")]
public class EnemyDataSO : CharacterDataSO
{
    [Header("Enemy Specific")]
    public BulletData bulletData;
    
    [Header("AI")]
    public float detectionRange = 10f;
    public float attackRange = 8f;
    public float patrolSpeed = 2f;
}