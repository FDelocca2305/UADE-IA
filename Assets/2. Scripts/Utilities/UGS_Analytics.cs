using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Analytics;
using UnityEngine.SceneManagement;

public class UGS_Analytics : MonoBehaviour
{
    public static UGS_Analytics Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    async void Start()
    {
        try
        {
            await UnityServices.InitializeAsync();
            GiveConsent(); // StartDataCollection in the new flow
        }
        catch (ServicesInitializationException e) // from Unity.Services.Core
        {
            MyLogger.LogError(e);
        }
        catch (RequestFailedException e) // from Unity.Services.Core
        {
            MyLogger.LogError(e);
        }
        catch (System.Exception e) // fallback
        {
            MyLogger.LogError(e);
        }
    }

    public void GiveConsent()
    {
        AnalyticsService.Instance.StartDataCollection();
        MyLogger.LogInfo("Consent has been provided. The SDK is now collecting data!");
    }

    // Event 1: Item collected
    public void LogItemCollected(int pointsAdded, int totalPoints)
    {
        var ev = new CustomEvent("itemCollected")
        {
            ["points_added"] = pointsAdded,
            ["total_points"]  = totalPoints
        };
        AnalyticsService.Instance.RecordEvent(ev);
        MyLogger.LogInfo($"Analytics: Item collected - Points added: {pointsAdded}, Total: {totalPoints}");
    }

    // Event 2: Escape unlocked
    public void LogEscapeUnlocked(int totalPoints, float timeToUnlockSeconds)
    {
        var ev = new CustomEvent("escapeUnlocked")
        {
            ["total_points"] = totalPoints,
            ["time_to_unlock"] = timeToUnlockSeconds
        };
        AnalyticsService.Instance.RecordEvent(ev);
        MyLogger.LogInfo($"Analytics: Escape unlocked at {totalPoints} points after {timeToUnlockSeconds:F1}s");
    }

    // Event 3: Player death
    public void LogPlayerDeath(int finalPoints, int finalHealth)
    {
        var ev = new CustomEvent("playerDeath")
        {
            ["final_points"] = finalPoints,
            ["final_health"] = finalHealth
        };
        AnalyticsService.Instance.RecordEvent(ev);
        MyLogger.LogInfo($"Analytics: Player died - Points: {finalPoints}, Health: {finalHealth}");
    }

    // Event 4: Retry button pressed
    public void LogRetryPressed(int lastScore, bool lastResultWasVictory)
    {
        var ev = new CustomEvent("retryPressed")
        {
            ["last_score"] = lastScore,
            ["was_victory"] = lastResultWasVictory,
            ["scene"] = SceneManager.GetActiveScene().name
        };
        AnalyticsService.Instance.RecordEvent(ev);
        MyLogger.LogInfo($"Analytics: Retry pressed - Score {lastScore}, Victory: {lastResultWasVictory}");
    }

    // Event 5: Guard killed
    public void LogGuardKilled(string guardName, Vector3 position)
    {
        var ev = new CustomEvent("guardKilled")
        {
            ["guard_name"] = guardName,
            ["scene"] = SceneManager.GetActiveScene().name,
            ["pos_x"] = position.x,
            ["pos_y"] = position.y,
            ["pos_z"] = position.z
        };
        AnalyticsService.Instance.RecordEvent(ev);
        MyLogger.LogInfo($"Analytics: Guard killed - {guardName} at {position}");
    }

    // Event 5b: Player hit by guard projectile
    public void LogPlayerHitByGuard(string guardName, float damage, float resultingHealth)
    {
        var ev = new CustomEvent("playerHitByGuard")
        {
            ["guard_name"] = guardName,
            ["damage"] = damage,
            ["resulting_health"] = resultingHealth,
            ["scene"] = SceneManager.GetActiveScene().name
        };
        AnalyticsService.Instance.RecordEvent(ev);
        MyLogger.LogInfo($"Analytics: Player hit by guard {guardName} for {damage} dmg (health now {resultingHealth})");
    }

    // Event 6: Civilian killed
    public void LogCivilianKilled(string civilianName, Vector3 position)
    {
        var ev = new CustomEvent("civilianKilled")
        {
            ["civilian_name"] = civilianName,
            ["scene"] = SceneManager.GetActiveScene().name,
            ["pos_x"] = position.x,
            ["pos_y"] = position.y,
            ["pos_z"] = position.z
        };
        AnalyticsService.Instance.RecordEvent(ev);
        MyLogger.LogInfo($"Analytics: Civilian killed - {civilianName} at {position}");
    }

    // Event 7: Shop panel opened
    public void LogShopOpened(string sourcePanel)
    {
        var ev = new CustomEvent("shopOpened")
        {
            ["source_panel"] = sourcePanel,
            ["scene"] = SceneManager.GetActiveScene().name
        };
        AnalyticsService.Instance.RecordEvent(ev);
        MyLogger.LogInfo($"Analytics: Shop opened from {sourcePanel}");
    }

    // Event 7a: Shop category viewed
    public void LogShopCategoryViewed(string categoryName, float scrollPosition)
    {
        var ev = new CustomEvent("shopCategoryViewed")
        {
            ["category"] = categoryName,
            ["scroll_position"] = scrollPosition,
            ["scene"] = SceneManager.GetActiveScene().name
        };
        AnalyticsService.Instance.RecordEvent(ev);
        MyLogger.LogInfo($"Analytics: Shop category viewed - {categoryName} (scroll {scrollPosition:F2})");
    }

    // Event 7b: Escape zone reached (level completion trigger)
    public void LogEscapeZoneReached(int totalPoints, float timeSinceStartSeconds)
    {
        var ev = new CustomEvent("escapeZoneReached")
        {
            ["total_points"] = totalPoints,
            ["time_since_start"] = timeSinceStartSeconds,
            ["scene"] = SceneManager.GetActiveScene().name
        };
        AnalyticsService.Instance.RecordEvent(ev);
        MyLogger.LogInfo($"Analytics: Escape zone reached after {timeSinceStartSeconds:F1}s with {totalPoints} points");
    }

    // Event 7c: Shop purchase attempt
    public void LogShopPurchaseAttempt(string itemId, string currency, int price)
    {
        var ev = new CustomEvent("shopPurchaseAttempt")
        {
            ["item_id"] = itemId,
            ["currency"] = currency,
            ["price"] = price,
            ["scene"] = SceneManager.GetActiveScene().name
        };
        AnalyticsService.Instance.RecordEvent(ev);
        MyLogger.LogInfo($"Analytics: Shop purchase attempt - {itemId} for {price} {currency}");
    }

    // Event 7d: Shop purchase success
    public void LogShopPurchaseSuccess(string itemId, string currency, int price)
    {
        var ev = new CustomEvent("shopPurchaseSuccess")
        {
            ["item_id"] = itemId,
            ["currency"] = currency,
            ["price"] = price,
            ["scene"] = SceneManager.GetActiveScene().name
        };
        AnalyticsService.Instance.RecordEvent(ev);
        MyLogger.LogInfo($"Analytics: Shop purchase success - {itemId} for {price} {currency}");
    }

    // Event 7e: Shop closed without purchase
    public void LogShopClosedWithoutPurchase(float timeInShopSeconds)
    {
        var ev = new CustomEvent("shopClosedWithoutPurchase")
        {
            ["time_in_shop"] = timeInShopSeconds,
            ["scene"] = SceneManager.GetActiveScene().name
        };
        AnalyticsService.Instance.RecordEvent(ev);
        MyLogger.LogInfo($"Analytics: Shop closed without purchase after {timeInShopSeconds:F1}s");
    }

    // Event 9: Rewarded ad started (e.g., duplicate reward button)
    public void LogRewardAdStarted(string placementId, string sourcePanel)
    {
        var ev = new CustomEvent("rewardAdStarted")
        {
            ["placement_id"] = placementId,
            ["source_panel"] = sourcePanel,
            ["scene"] = SceneManager.GetActiveScene().name
        };
        AnalyticsService.Instance.RecordEvent(ev);
        MyLogger.LogInfo($"Analytics: Rewarded ad started - {placementId} from {sourcePanel}");
    }

    // Event 10: Rewarded ad completed (reward granted)
    public void LogRewardAdCompleted(string placementId, string sourcePanel)
    {
        var ev = new CustomEvent("rewardAdCompleted")
        {
            ["placement_id"] = placementId,
            ["source_panel"] = sourcePanel,
            ["scene"] = SceneManager.GetActiveScene().name
        };
        AnalyticsService.Instance.RecordEvent(ev);
        MyLogger.LogInfo($"Analytics: Rewarded ad completed - {placementId} from {sourcePanel}");
    }

    // Event 11: Rewarded ad aborted (closed/failed/not ready)
    public void LogRewardAdAborted(string placementId, string sourcePanel, string reason)
    {
        var ev = new CustomEvent("rewardAdAborted")
        {
            ["placement_id"] = placementId,
            ["source_panel"] = sourcePanel,
            ["reason"] = reason,
            ["scene"] = SceneManager.GetActiveScene().name
        };
        AnalyticsService.Instance.RecordEvent(ev);
        MyLogger.LogInfo($"Analytics: Rewarded ad aborted - {placementId} from {sourcePanel} (reason: {reason})");
    }

    // Event 8: Session completed (victory or defeat)
    public void LogSessionCompleted(bool isVictory, int score, float durationSeconds)
    {
        var ev = new CustomEvent("sessionCompleted")
        {
            ["is_victory"] = isVictory,
            ["score"] = score,
            ["duration_seconds"] = durationSeconds,
            ["scene"] = SceneManager.GetActiveScene().name
        };
        AnalyticsService.Instance.RecordEvent(ev);
        MyLogger.LogInfo($"Analytics: Session completed - Victory: {isVictory}, Score: {score}, Duration: {durationSeconds:F1}s");
    }
    
    void OnApplicationQuit()
    {
        // Fuerza el envío antes de que se cierre el proceso (útil en Editor/PC)
        AnalyticsService.Instance.Flush();
    }
}
