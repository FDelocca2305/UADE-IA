using UnityEngine;

public interface ICombat
{
    void Shoot(Vector3 direction);
    bool CanShoot();
}