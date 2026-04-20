using System.Collections.Generic;
using FlockingSystem;
using UnityEngine;

namespace Game.AI.Flocking
{
    // ==================== NEIGHBOR DATA ====================

    /// <summary>
    /// Cached data for a neighboring entity.
    /// Stored once per frame to avoid redundant distance calculations.
    /// </summary>
    public struct NeighborData
    {
        public FlockingEntity entity;
        public Vector3 position;
        public Vector3 velocity;
        public Vector3 directionToNeighbor;
        public float distance;
        public float sqrDistance;

        public NeighborData(FlockingEntity source, FlockingEntity neighbor)
        {
            entity = neighbor;
            position = neighbor.transform.position;
            velocity = neighbor.Velocity;
            directionToNeighbor = position - source.transform.position;
            sqrDistance = directionToNeighbor.sqrMagnitude;
            distance = Mathf.Sqrt(sqrDistance);
        }
    }

    // ==================== SPATIAL HASH ====================

    /// <summary>
    /// Spatial hash grid for efficient neighbor queries O(n).
    /// Divides space into cells and only checks entities in nearby cells.
    /// </summary>
    public class SpatialHash
    {
        private Dictionary<int, List<FlockingEntity>> grid;
        private float cellSize;

        public SpatialHash(float cellSize)
        {
            this.cellSize = Mathf.Max(0.1f, cellSize);
            grid = new Dictionary<int, List<FlockingEntity>>();
        }

        public void Clear()
        {
            foreach (var cell in grid.Values)
            {
                cell.Clear();
            }
        }

        public void Insert(FlockingEntity entity)
        {
            int hash = GetHash(entity.transform.position);

            if (!grid.ContainsKey(hash))
            {
                grid[hash] = new List<FlockingEntity>();
            }

            grid[hash].Add(entity);
        }

        public List<FlockingEntity> Query(Vector3 position, float radius, FlockingEntity exclude = null)
        {
            List<FlockingEntity> results = new List<FlockingEntity>();
            float sqrRadius = radius * radius;

            int cellRadius = Mathf.CeilToInt(radius / cellSize);
            Vector3Int centerCell = GetCell(position);

            for (int x = -cellRadius; x <= cellRadius; x++)
            {
                for (int z = -cellRadius; z <= cellRadius; z++)
                {
                    Vector3Int cell = centerCell + new Vector3Int(x, 0, z);
                    int hash = GetHash(cell);

                    if (!grid.ContainsKey(hash))
                        continue;

                    foreach (var entity in grid[hash])
                    {
                        if (entity == exclude)
                            continue;

                        float sqrDistance = (entity.transform.position - position).sqrMagnitude;

                        if (sqrDistance <= sqrRadius)
                        {
                            results.Add(entity);
                        }
                    }
                }
            }

            return results;
        }

        public void SetCellSize(float newCellSize)
        {
            cellSize = Mathf.Max(0.1f, newCellSize);
        }

        private Vector3Int GetCell(Vector3 position)
        {
            return new Vector3Int(
                Mathf.FloorToInt(position.x / cellSize),
                0,
                Mathf.FloorToInt(position.z / cellSize)
            );
        }

        private int GetHash(Vector3Int cell)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + cell.x;
                hash = hash * 31 + cell.z;
                return hash;
            }
        }

        private int GetHash(Vector3 position)
        {
            return GetHash(GetCell(position));
        }
    }
}
