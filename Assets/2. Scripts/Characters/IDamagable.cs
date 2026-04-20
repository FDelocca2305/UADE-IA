using UnityEngine;

public interface IDamageable
{
    
    GameObject GameObject { get; }
    void TakeDamage(float damage);
    
    bool IsAlive { get; }
    
    
}
