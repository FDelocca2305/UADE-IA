using UnityEngine;

/// <summary>
/// Interface for entities that can collect items
/// </summary>
public interface ICollector
{
    /// <summary>
    /// Transform of the collector for positioning/distance checks
    /// </summary>
    Transform transform { get; }

    /// <summary>
    /// Add points to the collector's total
    /// </summary>
    /// <param name="points">Points to add</param>
    void AddPoints(int points);
}