using ScriptableObjects.Bullets;
using UnityEngine;

[CreateAssetMenu(fileName = "MainCharacterData", menuName = "Game Data/Main Character Data")]
public class MainCharacterDataSO : CharacterDataSO
{
    [Header("Player Specific")]
    public BulletData bulletData;
    public float magSize = 10;
    public float reloadTime = 2.5f;
}