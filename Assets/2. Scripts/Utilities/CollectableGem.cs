using Services;
using UnityEngine;

/// <summary>
/// Valuable gem collectable item
/// </summary>
public class CollectableGem : MonoBehaviour, ICollectable
{
    [Header("Item Settings")]
    [SerializeField] private int pointValue = 50;
    [SerializeField] private string itemName = "Gem";
    [SerializeField] private int walletReward = 1;

    /// <summary>
    /// Points awarded when collected
    /// </summary>
    public int Points => pointValue;

    /// <summary>
    /// Name of the item
    /// </summary>
    public string ItemName => itemName;

    /// <summary>
    /// Called when this item is collected
    /// </summary>
    /// <param name="collector">The collector that collected this item</param>
    public void Collect(ICollector collector)
    {
        MyLogger.LogInfo($"Collected {ItemName} worth {Points} points!");

        collector.AddPoints(Points);

        if (collector is PlayerCollector playerCollector)
        {
            playerCollector.RegisterGemPickup(walletReward);
        }

        gameObject.SetActive(false);
    }
}
