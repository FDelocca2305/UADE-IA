using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Services.MicroServices.PersistanceService;
using Services.MicroServices.UserDataService;

namespace Services.MicroServices.UserDataService
{
    [Serializable]
    public class ObjectLocator<TBase> : IPersistentElement
    {
        [JsonProperty] private Dictionary<Type, TBase> m_stateInstances = new Dictionary<Type, TBase>();

        public ObjectLocator(string p_id) => PersistenceID = p_id;

        public TConcrete GetObject<TConcrete>() where TConcrete : TBase, new()
        {
            Type l_stateType = typeof(TConcrete);
            if (m_stateInstances.TryGetValue(l_stateType, out var l_instance))
            {
                return (TConcrete)l_instance;
            }

            TConcrete l_stateInstance = new TConcrete();
            m_stateInstances.Add(l_stateType, l_stateInstance);

            return l_stateInstance;
        }

        public string PersistenceID { get; }

        public void OnAfterDeserialize()
        {
        }

        public void OnBeforeSerialize()
        {
        }
    }
}