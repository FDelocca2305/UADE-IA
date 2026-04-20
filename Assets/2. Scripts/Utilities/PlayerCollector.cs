using System;
using System.Collections;
using Services;
using Services.MicroServices.AudioService;
using Services.MicroServices.EventsServices;
using Services.MicroServices.EventsServices.CustomEvents;
using Services.MicroServices.GameStateService;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Player collector that automatically picks up collectable items and updates UI
/// </summary>
public class PlayerCollector : MonoBehaviour, ICollector
{
    [Header("UI Settings")]
    [SerializeField] private TMP_Text pointsText;
    [SerializeField] private string pointsFormat = "Points: {0}";

    [Header("Escape Settings")]
    [SerializeField] private TMP_Text escapeText;
    [SerializeField] private string escapeMessage = "Ya puedes escapar!";
    [SerializeField] private int escapeThreshold = 100;

    [Header("Health Settings")]
    [SerializeField] private Slider healthBar;
    [SerializeField] private int maxHealth = 100;

    [SerializeField] private GameObject ragdoll;
    [SerializeField] private GameObject[] objectsToDeactivateOnDeath;

    [Header("Audio")]
    [SerializeField] private AudioSource heartbeatAudioSource;

    private int _totalPoints = 0;
    private int _sessionCoinsCollected = 0;
    private int _sessionDiamondsCollected = 0;
    private bool _canEscape;
    private int _currentHealth;
    private bool _isDead;
    private MainCharacter _mainCharacter;
    private PlayerTouchMovement _playerMovement;
    private static AudioService AudioService => AudioService.Instance;
    private bool _isHeartbeatPlaying = false;
    
    /// <summary>
    /// Total points collected (read-only)
    /// </summary>
    public int TotalPoints => _totalPoints;
    public int SessionCoins => _sessionCoinsCollected;
    public int SessionDiamonds => _sessionDiamondsCollected;
    
    public event Action<bool> OnChangeCanEscape;
    
    void Start()
    {
        _mainCharacter = GetComponent<MainCharacter>();
        _playerMovement = GetComponent<PlayerTouchMovement>();
        _isDead = false;
        _sessionCoinsCollected = 0;
        _sessionDiamondsCollected = 0;

        if (_mainCharacter != null)
        {
            maxHealth = Mathf.Max(1, Mathf.RoundToInt(_mainCharacter.MaxHealth));
            _currentHealth = Mathf.Clamp(Mathf.RoundToInt(_mainCharacter.CurrentHealth), 0, maxHealth);
        }
        else
        {
            _currentHealth = maxHealth;
        }

        UpdateHealthBar();
        UpdatePointsDisplay();
        
        AudioService.PlayMusic(AudioService.GetConfig().gameplayBackground);
    }

    /// <summary>
    /// Add points to the total and update UI
    /// </summary>
    /// <param name="points">Points to add</param>
    public void AddPoints(int points)
    {
        _totalPoints += points;

        if (UGS_Analytics.Instance != null)
        {
            UGS_Analytics.Instance.LogItemCollected(points, _totalPoints);
        }

        if (!_canEscape && _totalPoints >= escapeThreshold)
        {
            _canEscape = true;
            OnChangeCanEscape?.Invoke(_canEscape);
            AudioService.PlaySFX(AudioService.GetConfig().canEscapeSFX);

            if (UGS_Analytics.Instance != null)
            {
                UGS_Analytics.Instance.LogEscapeUnlocked(_totalPoints, Time.timeSinceLevelLoad);
            }
        }

        UpdatePointsDisplay();
    }

    public void RegisterCoinPickup(int coinsAmount)
    {
        _sessionCoinsCollected += Mathf.Max(0, coinsAmount);
    }

    public void RegisterGemPickup(int diamondsAmount)
    {
        _sessionDiamondsCollected += Mathf.Max(0, diamondsAmount);
    }

    /// <summary>
    /// Hide HUD elements when player reaches the escape point.
    /// </summary>
    public void HideUIForEscape()
    {
        if (pointsText)
        {
            pointsText.gameObject.SetActive(false);
        }

        if (escapeText)
        {
            escapeText.gameObject.SetActive(false);
        }

        if (healthBar)
        {
            healthBar.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Update the points display text
    /// </summary>
    private void UpdatePointsDisplay()
    {
        if (pointsText != null)
        {
            pointsText.text = string.Format(pointsFormat, _totalPoints);
        }

        if (escapeText != null)
        {
            if (_canEscape)
            {
                escapeText.text = escapeMessage;
                escapeText.gameObject.SetActive(true);
            }
            else
            {
                escapeText.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Detect collectable items and bullets on trigger enter
    /// </summary>
    /// <param name="other">The collider that entered the trigger</param>
    void OnTriggerEnter(Collider other)
    {
        var collectable = other.GetComponent<ICollectable>();
        if (collectable != null)
        {
            collectable.Collect(this);
            AudioService.PlaySFX(AudioService.GetConfig().collectItemSFX);
        }
    }

    public void SyncHealth(float currentHealth, float maxHealthValue)
    {
        if (_isDead) return;

        int previousHealth = _currentHealth;

        maxHealth = Mathf.Max(1, Mathf.RoundToInt(maxHealthValue));
        _currentHealth = Mathf.Clamp(Mathf.RoundToInt(currentHealth), 0, maxHealth);

        if (_currentHealth < previousHealth)
        {
            AudioService.PlaySFX(AudioService.GetConfig().maleHurtSFX);
        }

        // Control heartbeat sound when health is low (below 30%)
        float healthPercentage = (float)_currentHealth / maxHealth;
        if (healthPercentage < 0.3f && healthPercentage > 0f)
        {
            if (!_isHeartbeatPlaying && heartbeatAudioSource != null)
            {
                heartbeatAudioSource.clip = AudioService.GetConfig().heartBeatingSFX;
                heartbeatAudioSource.loop = true;
                heartbeatAudioSource.Play();
                _isHeartbeatPlaying = true;
            }
        }
        else
        {
            if (_isHeartbeatPlaying && heartbeatAudioSource != null)
            {
                heartbeatAudioSource.Stop();
                _isHeartbeatPlaying = false;
            }
        }

        UpdateHealthBar();
        if (_currentHealth <= 0)
        {
            HandleDeath();
        }
    }

    /// <summary>
    /// Update the health bar UI
    /// </summary>
    private void UpdateHealthBar()
    {
        if (healthBar != null)
        {
            healthBar.value = (float)_currentHealth / maxHealth;
        }
    }

    /// <summary>
    /// Handle player death and restart game
    /// </summary>
    public void HandleDeath()
    {
        if (_isDead)
        {
            return;
        }

        _isDead = true;
        _currentHealth = 0;

        UpdateHealthBar();

        // Stop heartbeat if playing
        if (_isHeartbeatPlaying && heartbeatAudioSource != null)
        {
            heartbeatAudioSource.Stop();
            _isHeartbeatPlaying = false;
        }

        AudioService.PlaySFX(AudioService.GetConfig().maleDeathSFX);

        if (UGS_Analytics.Instance != null)
        {
            UGS_Analytics.Instance.LogPlayerDeath(_totalPoints, _currentHealth);
        }

        // On defeat we do not award run earnings; send zeros to results.
        ServiceLocator.Get<IEventService>().DispatchEvent(new GameResultEvent(false, 0, 0, 0));
        ServiceLocator.Get<IGameStateService>().ChangeState(GameState.GameOver);

        if (_playerMovement)
        {
            _playerMovement.enabled = false;
        }

        GetComponent<MeshCollider>().enabled = false;

        foreach (GameObject objects in objectsToDeactivateOnDeath)
        {
            objects.SetActive(false);
        }
        
        ragdoll.SetActive(true);
    }
}
