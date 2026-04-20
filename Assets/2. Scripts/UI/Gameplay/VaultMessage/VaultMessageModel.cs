using System;
using _2._Scripts.Gameplay;
using Services;
using Services.MicroServices.EventsServices;
using StaticClass;
using UnityEngine;

namespace _2._Scripts.UI.Gameplay.VaultMessage
{
    public class VaultMessageModel : UIModel
    {
        private static IEventService EventService => ServiceLocator.Get<IEventService>();

        public event Action OnStart;
        public event Action OnEnd;
        public event Action<float> OnUpdateSeconds;

        public override void Initialize(UIPresenter p_presenter)
        {
            base.Initialize(p_presenter);
            EventService.AddListener(EventsDefinition.START_OPEN_VAULT, StartOpenVaultHandler);
            EventService.AddListener(EventsDefinition.END_OPEN_VAULT, EndOpenVaultHandler);
            EventService.AddListener<OnUpdateVaultEvent>(OnUpdateVaultEventHandler);
        }

        public override void Shutdown()
        {
            base.Shutdown();
            EventService.RemoveListener(EventsDefinition.START_OPEN_VAULT, StartOpenVaultHandler);
            EventService.RemoveListener(EventsDefinition.END_OPEN_VAULT, EndOpenVaultHandler);      
            EventService.RemoveListener<OnUpdateVaultEvent>(OnUpdateVaultEventHandler);
        }

        private void StartOpenVaultHandler()
        {
            OnStart?.Invoke();
        }
        
        private void EndOpenVaultHandler()
        {
            OnEnd?.Invoke();
        }
        
        private void OnUpdateVaultEventHandler(OnUpdateVaultEvent p_data)
        {
            OnUpdateSeconds?.Invoke(p_data.Seconds);
        }
    }
}