using System;
using ScriptableObjects.Bullets;
using Services;
using Services.MicroServices.UpdateService;
using Unity.VisualScripting;
using UnityEngine;

public enum BulletOwner
{
    Unknown,
    Player,
    Guard,
    Ally
}

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Renderer))]
public class BulletObject : MonoBehaviour, IUpdateListener
{
    private BulletData m_bulletData;
    private Vector3 m_direction;
    private float m_timer;
    private bool m_isActive;
    private BulletOwner m_owner = BulletOwner.Unknown;
    private string m_ownerName = string.Empty;

    private Rigidbody m_rb;
    private Renderer m_bulletRenderer;

    public event Action<BulletObject> OnDeactivate;
    
    private static IUpdateService UpdateService => ServiceLocator.Get<IUpdateService>();

    private void Awake()
    {
        m_rb = GetComponent<Rigidbody>();
        m_bulletRenderer = GetComponent<Renderer>();
    }

    public void InitializeBullet(
        BulletData p_bulletData,
        Vector3 p_spawnPoint,
        Vector3 p_shootDirection,
        BulletOwner owner = BulletOwner.Unknown,
        string ownerName = "")
    {
        transform.position = p_spawnPoint;
        
        m_bulletData = p_bulletData;
        m_direction = p_shootDirection.normalized;
        m_timer = 0f;
        m_isActive = true;
        
        m_owner = owner;
        m_ownerName = ownerName ?? string.Empty;
        
        if (m_direction.sqrMagnitude > 0.0001f)
        {
            var lookRot = Quaternion.LookRotation(m_direction, Vector3.up);
            transform.rotation = lookRot * Quaternion.Euler(90f, 0f, 0f);
        }

        m_rb.useGravity = m_bulletData.UseGravity;
        m_rb.linearVelocity = m_direction * m_bulletData.Speed;
        m_bulletRenderer.material.color = m_bulletData.Color;

        SubscribeUpdateService();
        
        gameObject.SetActive(true);
    }

    private void OnTriggerEnter(Collider p_other)
    {
        HandleHit(p_other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        HandleHit(collision.collider);
    }

    private void Deactivate()
    {
        m_rb.linearVelocity = Vector3.zero;
        m_rb.angularVelocity = Vector3.zero;
        
        UnsubscribeUpdateService();
        
        m_owner = BulletOwner.Unknown;
        m_ownerName = string.Empty;
        gameObject.SetActive(false);
        OnDeactivate?.Invoke(this);
    }

    private void OnDestroy()
    {
        UnsubscribeUpdateService();
    }

    public void MyUpdate()
    {
        if (!m_isActive) return;

        m_timer += Time.deltaTime;
        if (m_timer >= m_bulletData.Lifetime)
        {
            Deactivate();
        }
    }

    public void SubscribeUpdateService()
    {
        UpdateService.AddUpdateListener(this);
    }

    public void UnsubscribeUpdateService()
    {
        UpdateService.RemoveUpdateListener(this);
    }

    private void HandleHit(Collider p_other)
    {
        if (!m_isActive)
            return;

        if (!p_other.TryGetComponent<IDamageable>(out var l_character))
        {
            l_character = p_other.GetComponentInParent<IDamageable>();
        }

        if (l_character == null)
        {
            Deactivate();
            return;
        }

        if (l_character.GameObject == gameObject)
            return;

        l_character.TakeDamage(m_bulletData.Damage);

        if (m_owner == BulletOwner.Guard && l_character is MainCharacter mainCharacter)
        {
            var analytics = UGS_Analytics.Instance;
            if (analytics != null)
            {
                string guardName = string.IsNullOrEmpty(m_ownerName) ? "Guard" : m_ownerName;
                analytics.LogPlayerHitByGuard(guardName, m_bulletData.Damage, mainCharacter.CurrentHealth);
            }
        }

        Deactivate();
    }
}
