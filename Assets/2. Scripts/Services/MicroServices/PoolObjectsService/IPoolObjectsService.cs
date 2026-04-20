using UnityEngine;

namespace Services.MicroServices.PoolObjectsService
{
    public interface IPoolObjectsService : IGameService
    {
        public T GetOrCreateObject<T>(T p_prefab, Transform p_content = null) where T : Object;
        public void ReturnObject<T>(T p_object) where T : Object;
    }
}