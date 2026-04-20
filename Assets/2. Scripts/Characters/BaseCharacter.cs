using UnityEngine;
using UnityEngine.Assertions;

//todo delete private 
public abstract class BaseCharacter : MonoBehaviour, ICharacter, IDamageable
{
    [SerializeField] protected CharacterDataSO characterData;
    
    protected float currentHealth;
    protected float lastShootTime;
    protected bool isAlive = true;
    
    public GameObject GameObject => gameObject;
    public Transform Transform => transform;
    public bool IsAlive => isAlive;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => characterData != null ? characterData.maxHealth : 100f;
    
    protected virtual void Awake()
    {
        Assert.IsNotNull(characterData);
        Initialize();
    }
    
    public virtual void Initialize()
    {
        if (characterData != null)
        {
            currentHealth = characterData.maxHealth;
        }
        else
        {
            currentHealth = 100f;
            MyLogger.LogWarning($"{gameObject.name}: No CharacterDataSO assigned, using default values");
        }
        
        isAlive = true;
        lastShootTime = 0f;
    }
    
    public abstract void Move(Vector3 direction);
    public abstract void Shoot(Vector3 direction);
    
    public virtual void TakeDamage(float damage)
    {
        if (!isAlive) return;

        currentHealth -= damage;
        if (currentHealth <= 0f)
        {
            isAlive = false;
            OnDeath();
        }
    }
    
    protected virtual void OnDeath()
    {
        gameObject.SetActive(false);
    }
    
    public virtual bool CanShoot()
    {
        return Time.time >= lastShootTime +  characterData.shootCooldown;
    }
}
