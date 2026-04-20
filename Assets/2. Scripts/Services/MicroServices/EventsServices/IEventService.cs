using System;

namespace Services.MicroServices.EventsServices
{
    public interface IEventService : IGameService
    {
        void AddListener(string p_key, Action p_callback);
        void RemoveListener(string p_key, Action p_callback);
        void DispatchEvent(string p_key);
        
        void AddListener<T>(Action<T> p_callback) where T : ICustomEventData;
        void RemoveListener<T>(Action<T> p_callback) where T : ICustomEventData;
        void DispatchEvent<T>(T p_data) where T : ICustomEventData;
    }

    public interface ICustomEventData{}
    
    public interface ICustomEventWrapper{}
    public interface ICustomEventWrapper<T> : ICustomEventWrapper where T : ICustomEventData{}
}