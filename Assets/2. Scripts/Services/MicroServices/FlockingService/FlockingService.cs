using System.Collections.Generic;
using FlockingSystem;
using Game.AI.Flocking;
using Services.MicroServices.UpdateService;

namespace Services.MicroServices.FlockingService
{
    /// <summary>
    /// Central service for all flocking entities.
    /// Uses spatial hashing for efficient neighbor queries O(n) instead of O(n²).
    /// Integrates with IUpdateService for automatic spatial hash rebuilding.
    /// </summary>
    public class FlockingService : IFlockingService, ILateUpdateListener
    {
        private readonly IUpdateService m_updateService;

        private List<FlockingEntity> m_entities;
        private SpatialHash m_spatialHash;
        private float m_spatialHashCellSize;
        private int m_hashRebuildInterval;
        private int m_frameCounter;

        /// <summary>
        /// Constructor with dependency injection.
        /// </summary>
        /// <param name="p_updateService">Update service for LateUpdate integration</param>
        public FlockingService(IUpdateService p_updateService)
        {
            m_updateService = p_updateService;
        }

        public void Initialize()
        {
            // Default configuration (can be changed via SetSpatialHashCellSize)
            m_spatialHashCellSize = 10f;
            m_hashRebuildInterval = 2;
            m_frameCounter = 0;

            m_entities = new List<FlockingEntity>();
            m_spatialHash = new SpatialHash(m_spatialHashCellSize);

            SubscribeService();
        }

        public void RegisterEntity(FlockingEntity p_entity)
        {
            if (p_entity == null)
                return;

            if (!m_entities.Contains(p_entity))
            {
                m_entities.Add(p_entity);
            }
        }

        public void UnregisterEntity(FlockingEntity p_entity)
        {
            if (p_entity == null)
                return;

            m_entities.Remove(p_entity);
        }

        public List<FlockingEntity> GetNeighbors(FlockingEntity p_entity, float p_radius)
        {
            if (p_entity == null)
                return new List<FlockingEntity>();

            return m_spatialHash.Query(p_entity.transform.position, p_radius, p_entity);
        }

        public int GetEntityCount()
        {
            return m_entities.Count;
        }

        public void SetSpatialHashCellSize(float p_cellSize)
        {
            m_spatialHashCellSize = UnityEngine.Mathf.Max(0.1f, p_cellSize);
            m_spatialHash.SetCellSize(m_spatialHashCellSize);
            RebuildSpatialHash();
        }

        /// <summary>
        /// Set the rebuild interval for spatial hash (in frames).
        /// Higher values = better performance but less accuracy.
        /// </summary>
        /// <param name="p_interval">Frames between rebuilds (1 = every frame)</param>
        public void SetHashRebuildInterval(int p_interval)
        {
            m_hashRebuildInterval = UnityEngine.Mathf.Max(1, p_interval);
        }

        #region ILateUpdateListener Implementation

        public void MyLateUpdate()
        {
            m_frameCounter++;

            if (m_frameCounter >= m_hashRebuildInterval)
            {
                RebuildSpatialHash();
                m_frameCounter = 0;
            }
        }

        public void SubscribeService()
        {
            m_updateService?.AddLateUpdateListener(this);
        }

        public void UnsubscribeService()
        {
            m_updateService?.RemoveLateUpdateListener(this);
        }

        #endregion

        /// <summary>
        /// Rebuild the spatial hash grid with current entity positions.
        /// Called automatically based on hashRebuildInterval.
        /// </summary>
        private void RebuildSpatialHash()
        {
            m_spatialHash.Clear();

            for (int l_i = 0; l_i < m_entities.Count; l_i++)
            {
                if (m_entities[l_i] != null)
                {
                    m_spatialHash.Insert(m_entities[l_i]);
                }
            }
        }
    }
}
