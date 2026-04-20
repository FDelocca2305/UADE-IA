using UnityEngine;

public interface ICharacter
{
    GameObject GameObject { get; }
    Transform Transform { get; }
    void Initialize();
    void Move(Vector3 direction);
    void Shoot(Vector3 direction);
}