using System;
using System.Collections.Generic;

namespace Services.MicroServices.EventsServices
{
    public class EventService : IEventService
    {
        private readonly Dictionary<string, Action> m_simpleEventsDictionary = new();
        private readonly Dictionary<Type, ICustomEventWrapper> m_complexEventsDictionary = new();

        public void Initialize() {}

        public void AddListener(string p_key, Action p_callback)
        {
            if (!m_simpleEventsDictionary.ContainsKey(p_key))
            {
                m_simpleEventsDictionary.Add(p_key, p_callback);
                return;
            }

            m_simpleEventsDictionary[p_key] += p_callback;
        }

        public void RemoveListener(string p_key, Action p_callback)
        {
            if (!m_simpleEventsDictionary.ContainsKey(p_key))
                return;

            m_simpleEventsDictionary[p_key] -= p_callback;
        }

        public void DispatchEvent(string p_key)
        {
            if (!m_simpleEventsDictionary.ContainsKey(p_key))
                return;
            
            m_simpleEventsDictionary[p_key]?.Invoke();
        }

        public void AddListener<T>(Action<T> p_callback) where T : ICustomEventData
        {
            var l_eventType = typeof(T);
            if (!m_complexEventsDictionary.ContainsKey(l_eventType))
            {
                var l_newWrapper = new CustomEventWrapper<T>();
                l_newWrapper.EventAction += p_callback;
                m_complexEventsDictionary.Add(l_eventType, l_newWrapper);
                return;
            }

            if (m_complexEventsDictionary[l_eventType] is CustomEventWrapper<T> l_wrapper)
                l_wrapper.EventAction += p_callback;
        }

        public void RemoveListener<T>(Action<T> p_callback) where T : ICustomEventData
        {
            var l_eventType = typeof(T);
            if (!m_complexEventsDictionary.ContainsKey(l_eventType))
                return;
            
            if (m_complexEventsDictionary[l_eventType] is CustomEventWrapper<T> l_wrapper)
                l_wrapper.EventAction -= p_callback;
        }

        public void DispatchEvent<T>(T p_data) where T : ICustomEventData
        {
            var l_eventType = typeof(T);
            if (!m_complexEventsDictionary.ContainsKey(l_eventType))
                return;
            
            if (m_complexEventsDictionary[l_eventType] is CustomEventWrapper<T> l_wrapper)
                l_wrapper.Dispatch(p_data);
        }
    }
}