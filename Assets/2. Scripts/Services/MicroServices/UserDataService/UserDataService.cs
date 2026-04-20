using System;
using System.IO;
using Services.MicroServices.PersistanceService;
using UnityEngine;

namespace Services.MicroServices.UserDataService
{
    public interface IUserDataService : IGameService
    {
        T GetState<T>() where T : IUserState, new();
        void Save();
    }


    [Serializable]
    public class UserDataService : IUserDataService, IDisposable
    {
        private const string PERSISTENCE_KEY = nameof(UserDataService);
        private ObjectLocator<IUserState> m_stateLocator;

        private IPersistenceService PersistenceService { get; }

        public UserDataService(IPersistenceService p_persistenceService)
        {
            PersistenceService = p_persistenceService;
        }

        public void Initialize()
        {
            try
            {
                m_stateLocator = PersistenceService.Get<ObjectLocator<IUserState>>(PERSISTENCE_KEY);
            }
            catch
            {
                MyLogger.LogError("The data had an error when obtaining it. The measure was taken to eliminate said data to be created again.");
                var l_fullPath = Path.Combine(Application.persistentDataPath, PERSISTENCE_KEY + ".json");

                if (File.Exists(l_fullPath))
                {
                    File.Delete(l_fullPath);
                }

                m_stateLocator = default;
            }

            if (m_stateLocator != default) 
                return;
            
            m_stateLocator = new ObjectLocator<IUserState>("UserStates");
            PersistenceService.Set(m_stateLocator, PERSISTENCE_KEY);
        }


        public T GetState<T>() where T : IUserState, new() => m_stateLocator.GetObject<T>();

        public void Save()
        {
            PersistenceService.Flush(PERSISTENCE_KEY);
        }

        public void Dispose() => Save();
    }
}