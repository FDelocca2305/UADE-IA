using UnityEngine;

[CreateAssetMenu(fileName = "CharacterData", menuName = "Game Data/Character Data")]
public class CharacterDataSO : ScriptableObject
{
    [Header("Health")]
    public float maxHealth = 100f;
    
    [Header("Movement")]
    public float moveSpeed = 5f;
    
    [Header("Combat")]
    public float shootCooldown = 0.25f;
    public float rotationSpeed = 10f;
    
    [Header("Visual")]
    public string characterName = "Character";
    public Color characterColor = Color.white;
}