using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "AI/Pathfinding/GraphAsset")]
public class GraphAsset : ScriptableObject
{
    public List<Vector3> nodePositions = new List<Vector3>();
    public List<IntArray> neighbors = new List<IntArray>();

    [Serializable] public struct IntArray { public int[] data; }

    public int NodeCount => nodePositions?.Count ?? 0;
}