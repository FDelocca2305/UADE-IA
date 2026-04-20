using UnityEngine;

/// <summary>
/// Interface for items that can be collected by a collector
/// </summary>
public interface ICollectable
{
    /// <summary>
    /// Points awarded when collected
    /// </summary>
    int Points { get; }

    /// <summary>
    /// Name of the item for display/logging
    /// </summary>
    string ItemName { get; }

    /// <summary>
    /// Called when this item is collected
    /// </summary>
    /// <param name="collector">The collector that collected this item</param>
    void Collect(ICollector collector);
}