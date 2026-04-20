using UnityEngine;
using Services;
using Services.MicroServices.AudioService;
using Services.MicroServices.EventsServices;
using Services.MicroServices.EventsServices.CustomEvents;
using Services.MicroServices.GameStateService;
using Services.MicroServices.UserDataService.Wallet;

/// <summary>
/// Escape zone that activates when player can escape and handles scene restart
/// </summary>
public class EscapeZone : MonoBehaviour
{
    [Header("Zone Settings")]
    [SerializeField] private Renderer escapePlaneRender;
    [SerializeField] private Material greenMaterial;
    [SerializeField] private PlayerCollector playerCollector;

    private Material m_originalMaterial;
    private static AudioService AudioService => AudioService.Instance;
    private AudioConfig m_audioConfig;

    private bool m_canEscape;

    private void Start()
    {
        m_audioConfig = AudioService.GetConfig();
        
        m_originalMaterial = escapePlaneRender.material;

        playerCollector.OnChangeCanEscape += OnChangeCanEventHandler;
    }

    private void OnChangeCanEventHandler(bool p_canEscape)
    {
        m_canEscape = p_canEscape;
        if (m_canEscape)
        {
            if (greenMaterial != null)
            {
                escapePlaneRender.material = greenMaterial;
            }
        }
        else
        {
            escapePlaneRender.material = m_originalMaterial;
        }
    }

    private void OnTriggerEnter(Collider p_other)
    {
        if (!p_other.CompareTag("Player")) 
            return;
        
        if (m_canEscape)
        {
            ShowEscapeUI(playerCollector);
        }
    }

    private void ShowEscapeUI(PlayerCollector p_playerCollector)
    {
        var l_playerMovement = p_playerCollector.GetComponent<PlayerTouchMovement>();
        if (l_playerMovement)
        {
            l_playerMovement.enabled = false;
        }

        // Hide collector HUD once player reaches the escape point.
        p_playerCollector.HideUIForEscape();

        if (UGS_Analytics.Instance != null)
        {
            UGS_Analytics.Instance.LogEscapeZoneReached(p_playerCollector.TotalPoints, Time.timeSinceLevelLoad);
        }

        p_playerCollector.gameObject.SetActive(false);

        if (AudioService != null && m_audioConfig != null)
        {
            AudioService.PlaySFX(m_audioConfig.escapeSFX);
        }

        // Credit the run only on victory.
        ServiceLocator.Get<IWalletService>()?.AddCoins(p_playerCollector.SessionCoins);
        ServiceLocator.Get<IWalletService>()?.AddDiamonds(p_playerCollector.SessionDiamonds);

        ServiceLocator.Get<IEventService>().DispatchEvent(new GameResultEvent(true, p_playerCollector.TotalPoints, p_playerCollector.SessionCoins, p_playerCollector.SessionDiamonds));
        ServiceLocator.Get<IGameStateService>().ChangeState(GameState.Victory);
    }
}
