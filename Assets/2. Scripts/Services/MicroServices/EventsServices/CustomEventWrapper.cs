using System;

namespace Services.MicroServices.EventsServices
{
    public class CustomEventWrapper<T> : ICustomEventWrapper<T> where T : ICustomEventData
    {
        public event Action<T> EventAction;

        public void Dispatch(T p_eventData) => EventAction?.Invoke(p_eventData);
    }
}