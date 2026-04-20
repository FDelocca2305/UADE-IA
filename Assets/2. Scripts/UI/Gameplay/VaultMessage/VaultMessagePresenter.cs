using System;
using UnityEngine;

namespace _2._Scripts.UI.Gameplay.VaultMessage
{
    [RequireComponent(typeof(VaultMessageModel))]
    [RequireComponent(typeof(VaultMessageView))]
    public class VaultMessagePresenter : UIPresenter
    {
        private VaultMessageModel m_model;
        private VaultMessageView m_view;
        private void Awake()
        {
            Initialize();
        }

        private void OnDestroy()
        {
            Shutdown();
        }

        public override void Initialize()
        {
            base.Initialize();
            m_view = uiView as VaultMessageView;
            m_model = uiModel as VaultMessageModel;

            if (m_model == null) 
                return;
            
            m_model.OnStart += Show;
            m_model.OnEnd += Hide;
            
            if (m_view != null) 
                m_model.OnUpdateSeconds += m_view.SetSeconds;
        }
    }
}