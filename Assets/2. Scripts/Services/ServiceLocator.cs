using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Services.MicroServices.AdsService;
using Services.MicroServices.AudioService;
using Services.MicroServices.BlackboardService;
using Services.MicroServices.EventsServices;
using Services.MicroServices.FlockingService;
using Services.MicroServices.GameStateService;
using Services.MicroServices.PersistanceService;
using Services.MicroServices.PoolObjectsService;
using Services.MicroServices.UpdateService;
using Services.MicroServices.UserDataService;
using Services.MicroServices.UserDataService.PlayerUpgrades;
using Services.MicroServices.UserDataService.Wallet;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

namespace Services
{
    public struct ServiceDefinition
    {
        public Type Service { get; private set; }
        public bool IsSceneUnloaded { get; private set; }

        public ServiceDefinition(Type p_service, bool p_isSceneUnloaded)
        {
            Service = p_service;
            IsSceneUnloaded = p_isSceneUnloaded;
        }
    }
    
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, ServiceDefinition> m_serviceDefinitions = new();
        private static readonly Dictionary<Type, IGameService> m_serviceInstances = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        public static void DefineServices()
        {
            SceneManager.sceneUnloaded += SceneManagerOnSceneUnloaded;

            Register<IEventService, EventService>();
            Register<IPersistenceService, LocalPersistenceService>();
            Register<IUserDataService, UserDataService>();
            Register<IUpdateService, UpdateService>();
            Register<IFlockingService, FlockingService>();
            Register<IPoolObjectsService, PoolObjectsService>();
            Register<IGameStateService, GameStateService>();
            Register<IBlackboardService, BlackboardService>(true);
            Register<IWalletService, WalletService>(true, true);
            Register<IPlayerUpgradeService, PlayerUpgradeService>(true, true);
            Register<IAdsService, AdsService>(false, true);
        }

        public static void Register<TInterface, TInstance>(bool p_isSceneUnloaded = false, bool p_immediateInit = false)
            where TInterface : IGameService where TInstance : class, TInterface
        {
            var l_interfaceType = typeof(TInterface);
            var l_instanceClassType = typeof(TInstance);

            Assert.IsFalse(m_serviceDefinitions.ContainsKey(l_interfaceType),
                $"Service {l_interfaceType} is already registered");
            m_serviceDefinitions.Add(l_interfaceType, new ServiceDefinition(l_instanceClassType, p_isSceneUnloaded));

            if (p_immediateInit)
            {
                InitializeService<TInterface>();
            }
        }

        public static void Unregister<T>() where T : IGameService
        {
            var l_type = typeof(T);
            Assert.IsFalse(!m_serviceDefinitions.ContainsKey(l_type), $"Service {l_type} is not registered");
            m_serviceDefinitions.Remove(l_type);
        }

        public static T Get<T>() where T : IGameService
        {
            var l_type = typeof(T);
            return (T)Get(l_type);
        }

        private static IGameService Get(Type p_type)
        {
            return m_serviceInstances.TryGetValue(p_type, out var l_instance) ? l_instance : InitializeService(p_type);
        }

        private static T InitializeService<T>() where T : IGameService
        {
            var l_type = typeof(T);
            return (T)InitializeService(l_type);
        }

        private static IGameService InitializeService(Type p_serviceType)
        {
            if (!m_serviceDefinitions.ContainsKey(p_serviceType))
                throw new Exception($"Service {p_serviceType} not found");

            var l_concreteType = m_serviceDefinitions[p_serviceType];

            var l_newInstance = CreateInstance(l_concreteType.Service);
            m_serviceInstances.Add(p_serviceType, l_newInstance);

            l_newInstance.Initialize();

            return l_newInstance;
        }

        private static bool AreParametersValid(ConstructorInfo p_constructorInfo)
        {
            return p_constructorInfo.GetParameters().All(p_p => typeof(IGameService).IsAssignableFrom(p_p.ParameterType));
        }

        private static IGameService CreateInstance(Type p_concreteType)
        {
            var l_constructors = p_concreteType.GetConstructors();

            if (l_constructors.Length == 0 || l_constructors[0].GetParameters().Length == 0)
            {
                return (IGameService)Activator.CreateInstance(p_concreteType);
            }

            if (l_constructors.Length > 1 || !AreParametersValid(l_constructors[0]))
            {
                throw new Exception(
                    $"The Service {p_concreteType} can't be created. It should define only one constructor with IGameService parameters or an empty constructor.");
            }

            var l_constructorParameters = l_constructors[0].GetParameters();
            var l_instanceParameters = new object[l_constructorParameters.Length];

            for (var l_i = 0; l_i < l_constructorParameters.Length; l_i++)
            {
                l_instanceParameters[l_i] = Get(l_constructorParameters[l_i].ParameterType);
            }

            return (IGameService)Activator.CreateInstance(p_concreteType, l_instanceParameters);
        }

        public static void TearDown()
        {
            foreach (var l_type in m_serviceDefinitions.Keys)
            {
                if (!m_serviceInstances.ContainsKey(l_type))
                    continue;

                if (m_serviceInstances[l_type] is IDisposable l_target)
                {
                    l_target.Dispose();
                }

                m_serviceInstances.Remove(l_type);
            }
        }
        private static void SceneManagerOnSceneUnloaded(Scene p_arg0)
        {
            foreach (var l_type in m_serviceDefinitions.Keys)
            {
                if (!m_serviceInstances.ContainsKey(l_type))
                    continue;
                
                if (!m_serviceDefinitions[l_type].IsSceneUnloaded)
                    continue;
                
                if (m_serviceInstances[l_type] is IDisposable l_target)
                {
                    l_target.Dispose();
                }

                m_serviceInstances.Remove(l_type);
            }
        }
        
        public static void RegisterInstance<TInterface>(TInterface instance, bool p_isSceneUnloaded = false)
            where TInterface : class, IGameService
        {
            var iface = typeof(TInterface);
            Assert.IsFalse(m_serviceDefinitions.ContainsKey(iface) || m_serviceInstances.ContainsKey(iface),
                $"Service {iface} is already registered");
            
            m_serviceDefinitions.Add(iface, new ServiceDefinition(instance.GetType(), p_isSceneUnloaded));

            m_serviceInstances.Add(iface, instance);
            instance.Initialize();
        }
    }

    public interface IGameService
    {
        void Initialize();
    }
}
