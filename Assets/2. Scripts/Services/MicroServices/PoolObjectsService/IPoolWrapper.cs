using UnityEngine;

namespace Services.MicroServices.PoolObjectsService
{
    public interface IPoolWrapper
    {
        
    }
    public interface IPoolWrapper<T> : IPoolWrapper where T : Object
    {
        
    }
}